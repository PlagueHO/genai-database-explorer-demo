using GenAIDBExplorer.Core.Models.Progress;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Models.Database;
using GenAIDBExplorer.Core.SemanticModelProviders;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;

namespace GenAIDBExplorer.Core.Test.SemanticModelProviders;

[TestClass]
public class SemanticModelProviderProgressTests
{
    private Mock<IProject> _mockProject = null!;
    private Mock<ISchemaRepository> _mockSchemaRepository = null!;
    private Mock<ILogger<SemanticModelProvider>> _mockLogger = null!;
    private SemanticModelProvider _provider = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockProject = new Mock<IProject>();
        _mockSchemaRepository = new Mock<ISchemaRepository>();
        _mockLogger = new Mock<ILogger<SemanticModelProvider>>();

        var settings = new ProjectSettings
        {
            Database = new DatabaseSettings
            {
                Name = "TestDatabase",
                ConnectionString = "TestConnectionString",
                Description = "Test Description",
                Schema = "dbo",
                MaxDegreeOfParallelism = 1
            },
            DataDictionary = new DataDictionarySettings(),
            OpenAIService = new OpenAIServiceSettings(),
            SemanticModel = new SemanticModelSettings()
        };
        
        _mockProject.Setup(p => p.Settings).Returns(settings);

        _provider = new SemanticModelProvider(_mockProject.Object, _mockSchemaRepository.Object, _mockLogger.Object);
    }

    [TestMethod]
    public async Task ExtractSemanticModelAsync_WithProgress_ShouldReportInitialProgress()
    {
        // Arrange
        var progressReports = new List<SemanticModelExtractionProgress>();
        var progress = new Progress<SemanticModelExtractionProgress>(p => progressReports.Add(p));

        _mockSchemaRepository.Setup(r => r.GetTablesAsync(It.IsAny<string>())).ReturnsAsync(new Dictionary<string, TableInfo>());
        _mockSchemaRepository.Setup(r => r.GetViewsAsync(It.IsAny<string>())).ReturnsAsync(new Dictionary<string, ViewInfo>());
        _mockSchemaRepository.Setup(r => r.GetStoredProceduresAsync(It.IsAny<string>())).ReturnsAsync(new Dictionary<string, StoredProcedureInfo>());

        // Act
        await _provider.ExtractSemanticModelAsync(progress);

        // Assert
        progressReports.Should().NotBeEmpty();
        progressReports.First().CurrentPhase.Should().Be("Initialization");
        progressReports.First().Message.Should().Be("Starting semantic model extraction");
        progressReports.First().CurrentStep.Should().Be(0);
        progressReports.First().TotalSteps.Should().Be(3);
    }

    [TestMethod]
    public async Task ExtractSemanticModelAsync_WithProgress_ShouldReportAllPhases()
    {
        // Arrange
        var progressReports = new List<SemanticModelExtractionProgress>();
        var progress = new Progress<SemanticModelExtractionProgress>(p => progressReports.Add(p));

        _mockSchemaRepository.Setup(r => r.GetTablesAsync(It.IsAny<string>())).ReturnsAsync(new Dictionary<string, TableInfo>());
        _mockSchemaRepository.Setup(r => r.GetViewsAsync(It.IsAny<string>())).ReturnsAsync(new Dictionary<string, ViewInfo>());
        _mockSchemaRepository.Setup(r => r.GetStoredProceduresAsync(It.IsAny<string>())).ReturnsAsync(new Dictionary<string, StoredProcedureInfo>());

        // Act
        await _provider.ExtractSemanticModelAsync(progress);

        // Assert
        progressReports.Should().HaveCountGreaterThan(3);
        progressReports.Should().Contain(p => p.CurrentPhase == "Initialization");
        progressReports.Should().Contain(p => p.CurrentPhase == "Tables");
        progressReports.Should().Contain(p => p.CurrentPhase == "Views");
        progressReports.Should().Contain(p => p.CurrentPhase == "StoredProcedures");
        progressReports.Should().Contain(p => p.CurrentPhase == "Completed");
    }

    [TestMethod]
    public async Task ExtractSemanticModelAsync_WithCancellation_ShouldThrowOperationCancelledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        _mockSchemaRepository.Setup(r => r.GetTablesAsync(It.IsAny<string>())).ReturnsAsync(new Dictionary<string, TableInfo>());
        _mockSchemaRepository.Setup(r => r.GetViewsAsync(It.IsAny<string>())).ReturnsAsync(new Dictionary<string, ViewInfo>());
        _mockSchemaRepository.Setup(r => r.GetStoredProceduresAsync(It.IsAny<string>())).ReturnsAsync(new Dictionary<string, StoredProcedureInfo>());

        // Act & Assert
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(
            async () => await _provider.ExtractSemanticModelAsync(null, cts.Token));
    }

    [TestMethod]
    public async Task ExtractSemanticModelAsync_LegacyOverload_ShouldStillWork()
    {
        // Arrange
        _mockSchemaRepository.Setup(r => r.GetTablesAsync(It.IsAny<string>())).ReturnsAsync(new Dictionary<string, TableInfo>());
        _mockSchemaRepository.Setup(r => r.GetViewsAsync(It.IsAny<string>())).ReturnsAsync(new Dictionary<string, ViewInfo>());
        _mockSchemaRepository.Setup(r => r.GetStoredProceduresAsync(It.IsAny<string>())).ReturnsAsync(new Dictionary<string, StoredProcedureInfo>());

        // Act
        var result = await _provider.ExtractSemanticModelAsync();

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("TestDatabase");
    }
}