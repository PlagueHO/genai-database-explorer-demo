using FluentAssertions;
using GenAIDBExplorer.Core.Models.Database;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.SemanticModelProviders;
using Microsoft.Extensions.Logging;
using Moq;

namespace GenAIDBExplorer.Core.Integration.Test.SemanticModelProviders;

/// <summary>
/// Basic integration tests for SemanticModelProvider.
/// Tests fundamental semantic model creation and provider functionality.
/// </summary>
[TestClass]
[TestCategory("Integration")]
public class SemanticModelProviderBasicIntegrationTests
{
    private Mock<IProject> _mockProject = null!;
    private Mock<ISchemaRepository> _mockSchemaRepository = null!;
    private Mock<ILogger<SemanticModelProvider>> _mockLogger = null!;
    private SemanticModelProvider _semanticModelProvider = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockProject = new Mock<IProject>();
        _mockSchemaRepository = new Mock<ISchemaRepository>();
        _mockLogger = new Mock<ILogger<SemanticModelProvider>>();

        // Setup project settings
        var mockProjectSettings = new Mock<ProjectSettings>();
        var mockDatabaseSettings = new Mock<DatabaseSettings>();
        var mockSemanticModelSettings = new Mock<SemanticModelSettings>();
        
        mockDatabaseSettings.Setup(x => x.Name).Returns("TestDatabase");
        mockDatabaseSettings.Setup(x => x.ConnectionString).Returns("TestSource");
        mockDatabaseSettings.Setup(x => x.Description).Returns("Test semantic model description");
        mockDatabaseSettings.Setup(x => x.MaxDegreeOfParallelism).Returns(4);
        
        mockSemanticModelSettings.Setup(x => x.MaxDegreeOfParallelism).Returns(4);
        
        mockProjectSettings.Setup(x => x.Database).Returns(mockDatabaseSettings.Object);
        mockProjectSettings.Setup(x => x.SemanticModel).Returns(mockSemanticModelSettings.Object);
        _mockProject.Setup(x => x.Settings).Returns(mockProjectSettings.Object);

        _semanticModelProvider = new SemanticModelProvider(_mockProject.Object, _mockSchemaRepository.Object, _mockLogger.Object);
    }

    [TestMethod]
    public void CreateSemanticModel_ShouldReturnConfiguredSemanticModel()
    {
        // Act
        var result = _semanticModelProvider.CreateSemanticModel();

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("TestDatabase");
        result.Source.Should().Be("TestSource");
        result.Description.Should().Be("Test semantic model description");
        result.Tables.Should().NotBeNull();
        result.Views.Should().NotBeNull();
        result.StoredProcedures.Should().NotBeNull();
    }

    [TestMethod]
    public async Task ExtractSemanticModelAsync_ShouldCallRepositoryMethods()
    {
        // Arrange
        var mockTables = new Dictionary<string, TableInfo>
        {
            ["dbo.Users"] = new TableInfo("dbo", "Users")
        };

        var mockViews = new Dictionary<string, ViewInfo>
        {
            ["dbo.UserView"] = new ViewInfo("dbo", "UserView")
        };

        var mockStoredProcedures = new Dictionary<string, StoredProcedureInfo>
        {
            ["dbo.GetUser"] = new StoredProcedureInfo("dbo", "GetUser", "PROCEDURE", null, "SELECT * FROM Users")
        };

        _mockSchemaRepository.Setup(x => x.GetTablesAsync(It.IsAny<string?>()))
            .ReturnsAsync(mockTables);

        _mockSchemaRepository.Setup(x => x.GetViewsAsync(It.IsAny<string?>()))
            .ReturnsAsync(mockViews);

        _mockSchemaRepository.Setup(x => x.GetStoredProceduresAsync(It.IsAny<string?>()))
            .ReturnsAsync(mockStoredProcedures);

        _mockSchemaRepository.Setup(x => x.CreateSemanticModelTableAsync(It.IsAny<TableInfo>()))
            .ReturnsAsync(new SemanticModelTable("dbo", "Users"));

        _mockSchemaRepository.Setup(x => x.CreateSemanticModelViewAsync(It.IsAny<ViewInfo>()))
            .ReturnsAsync(new SemanticModelView("dbo", "UserView"));

        _mockSchemaRepository.Setup(x => x.CreateSemanticModelStoredProcedureAsync(It.IsAny<StoredProcedureInfo>()))
            .ReturnsAsync(new SemanticModelStoredProcedure("dbo", "GetUser", "SELECT * FROM Users", null, null));

        // Act
        var result = await _semanticModelProvider.ExtractSemanticModelAsync();

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("TestDatabase");
        result.Source.Should().Be("TestSource");
        result.Description.Should().Be("Test semantic model description");

        // Verify all repository methods were called
        _mockSchemaRepository.Verify(x => x.GetTablesAsync(It.IsAny<string?>()), Times.Once);
        _mockSchemaRepository.Verify(x => x.GetViewsAsync(It.IsAny<string?>()), Times.Once);
        _mockSchemaRepository.Verify(x => x.GetStoredProceduresAsync(It.IsAny<string?>()), Times.Once);
    }

    [TestMethod]
    public async Task LoadSemanticModelAsync_WithNonExistentPath_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var testPath = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "NonExistentSemanticModelTest"));

        // Act & Assert
        var act = async () => await _semanticModelProvider.LoadSemanticModelAsync(testPath);
        
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [TestMethod]
    public async Task ExtractSemanticModelAsync_WhenRepositoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        _mockSchemaRepository.Setup(x => x.GetTablesAsync(It.IsAny<string?>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => _semanticModelProvider.ExtractSemanticModelAsync());

        exception.Message.Should().Be("Database connection failed");
    }
}