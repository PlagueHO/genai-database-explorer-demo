using FluentAssertions;
using GenAIDBExplorer.Core.Models.SemanticModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GenAIDBExplorer.Core.Tests.Models.SemanticModel;

[TestClass]
public class LazySemanticModelTests
{
    private Mock<ISemanticModelMetadata> _mockMetadata = null!;
    private DirectoryInfo _mockModelPath = null!;
    private LazySemanticModel _lazyModel = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockMetadata = new Mock<ISemanticModelMetadata>();
        _mockModelPath = new DirectoryInfo("/tmp/test-model");
        
        // Setup mock metadata
        _mockMetadata.Setup(m => m.Name).Returns("TestModel");
        _mockMetadata.Setup(m => m.Source).Returns("TestSource");
        _mockMetadata.Setup(m => m.Description).Returns("TestDescription");
        _mockMetadata.Setup(m => m.TableIdentifiers).Returns(new[] { "dbo.Table1", "dbo.Table2" });
        _mockMetadata.Setup(m => m.ViewIdentifiers).Returns(new[] { "dbo.View1" });
        _mockMetadata.Setup(m => m.StoredProcedureIdentifiers).Returns(new[] { "dbo.SP1" });

        _lazyModel = new LazySemanticModel(_mockMetadata.Object, _mockModelPath);
    }

    [TestMethod]
    public void Constructor_ShouldInitializeProperties()
    {
        // Assert
        _lazyModel.Name.Should().Be("TestModel");
        _lazyModel.Source.Should().Be("TestSource");
        _lazyModel.Description.Should().Be("TestDescription");
    }

    [TestMethod]
    public void IsTableLoaded_WhenTableNotLoaded_ShouldReturnFalse()
    {
        // Act
        var isLoaded = _lazyModel.IsTableLoaded("dbo", "Table1");

        // Assert
        isLoaded.Should().BeFalse();
    }

    [TestMethod]
    public void IsViewLoaded_WhenViewNotLoaded_ShouldReturnFalse()
    {
        // Act
        var isLoaded = _lazyModel.IsViewLoaded("dbo", "View1");

        // Assert
        isLoaded.Should().BeFalse();
    }

    [TestMethod]
    public void IsStoredProcedureLoaded_WhenStoredProcedureNotLoaded_ShouldReturnFalse()
    {
        // Act
        var isLoaded = _lazyModel.IsStoredProcedureLoaded("dbo", "SP1");

        // Assert
        isLoaded.Should().BeFalse();
    }

    [TestMethod]
    public void AddTable_ShouldAddTableAndMarkAsLoaded()
    {
        // Arrange
        var table = new SemanticModelTable("dbo", "NewTable");

        // Act
        _lazyModel.AddTable(table);

        // Assert
        _lazyModel.IsTableLoaded("dbo", "NewTable").Should().BeTrue();
    }

    [TestMethod]
    public void AddView_ShouldAddViewAndMarkAsLoaded()
    {
        // Arrange
        var view = new SemanticModelView("dbo", "NewView");

        // Act
        _lazyModel.AddView(view);

        // Assert
        _lazyModel.IsViewLoaded("dbo", "NewView").Should().BeTrue();
    }

    [TestMethod]
    public void AddStoredProcedure_ShouldAddStoredProcedureAndMarkAsLoaded()
    {
        // Arrange
        var sp = new SemanticModelStoredProcedure("dbo", "NewSP", "SELECT 1");

        // Act
        _lazyModel.AddStoredProcedure(sp);

        // Assert
        _lazyModel.IsStoredProcedureLoaded("dbo", "NewSP").Should().BeTrue();
    }

    [TestMethod]
    public void RemoveTable_WhenTableExists_ShouldRemoveAndReturnTrue()
    {
        // Arrange
        var table = new SemanticModelTable("dbo", "TestTable");
        _lazyModel.AddTable(table);

        // Act
        var result = _lazyModel.RemoveTable(table);

        // Assert
        result.Should().BeTrue();
        _lazyModel.IsTableLoaded("dbo", "TestTable").Should().BeFalse();
    }

    [TestMethod]
    public void RemoveView_WhenViewExists_ShouldRemoveAndReturnTrue()
    {
        // Arrange
        var view = new SemanticModelView("dbo", "TestView");
        _lazyModel.AddView(view);

        // Act
        var result = _lazyModel.RemoveView(view);

        // Assert
        result.Should().BeTrue();
        _lazyModel.IsViewLoaded("dbo", "TestView").Should().BeFalse();
    }

    [TestMethod]
    public void RemoveStoredProcedure_WhenStoredProcedureExists_ShouldRemoveAndReturnTrue()
    {
        // Arrange
        var sp = new SemanticModelStoredProcedure("dbo", "TestSP", "SELECT 1");
        _lazyModel.AddStoredProcedure(sp);

        // Act
        var result = _lazyModel.RemoveStoredProcedure(sp);

        // Assert
        result.Should().BeTrue();
        _lazyModel.IsStoredProcedureLoaded("dbo", "TestSP").Should().BeFalse();
    }

    [TestMethod]
    public void FindTable_WhenTableNotInMetadata_ShouldReturnNull()
    {
        // Act
        var table = _lazyModel.FindTable("dbo", "NonExistentTable");

        // Assert
        table.Should().BeNull();
    }

    [TestMethod]
    public void FindView_WhenViewNotInMetadata_ShouldReturnNull()
    {
        // Act
        var view = _lazyModel.FindView("dbo", "NonExistentView");

        // Assert
        view.Should().BeNull();
    }

    [TestMethod]
    public void FindStoredProcedure_WhenStoredProcedureNotInMetadata_ShouldReturnNull()
    {
        // Act
        var sp = _lazyModel.FindStoredProcedure("dbo", "NonExistentSP");

        // Assert
        sp.Should().BeNull();
    }

    [TestMethod]
    public void LoadModelAsync_ShouldThrowNotSupportedException()
    {
        // Act
        Func<Task> act = async () => await _lazyModel.LoadModelAsync(_mockModelPath);

        // Assert
        act.Should().ThrowAsync<NotSupportedException>();
    }

    [TestMethod]
    public async Task LoadTablesAsync_WithValidIds_ShouldReturnCorrectCount()
    {
        // Arrange
        var tableIds = new[] { "dbo.Table1", "dbo.Table2" };

        // Act & Assert - This will fail because there are no actual files to load
        // The method should complete successfully but return tables with minimal data
        var result = await _lazyModel.LoadTablesAsync(tableIds);
        
        // Assert
        result.Should().HaveCount(2);
        result[0].Schema.Should().Be("dbo");
        result[0].Name.Should().Be("Table1");
        result[1].Schema.Should().Be("dbo");
        result[1].Name.Should().Be("Table2");
    }

    [TestMethod]
    public async Task LoadTablesAsync_WithInvalidId_ShouldThrowArgumentException()
    {
        // Arrange
        var invalidTableIds = new[] { "InvalidFormat" };

        // Act
        Func<Task> act = async () => await _lazyModel.LoadTablesAsync(invalidTableIds);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Invalid table identifier format: InvalidFormat. Expected 'schema.name'.");
    }

    [TestMethod]
    public async Task LoadViewsAsync_WithInvalidId_ShouldThrowArgumentException()
    {
        // Arrange
        var invalidViewIds = new[] { "InvalidFormat" };

        // Act
        Func<Task> act = async () => await _lazyModel.LoadViewsAsync(invalidViewIds);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Invalid view identifier format: InvalidFormat. Expected 'schema.name'.");
    }

    [TestMethod]
    public async Task LoadStoredProceduresAsync_WithInvalidId_ShouldThrowArgumentException()
    {
        // Arrange
        var invalidSpIds = new[] { "InvalidFormat" };

        // Act
        Func<Task> act = async () => await _lazyModel.LoadStoredProceduresAsync(invalidSpIds);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Invalid stored procedure identifier format: InvalidFormat. Expected 'schema.name'.");
    }
}