using FluentAssertions;
using GenAIDBExplorer.Core.Models.Database;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.SemanticModelProviders;
using Microsoft.Extensions.Logging;
using Moq;

namespace GenAIDBExplorer.Core.Tests.SemanticModelProviders;

[TestClass]
public class CachedSchemaRepositoryTests
{
    private Mock<ISchemaRepository> _mockInnerRepository = null!;
    private Mock<ISemanticModelCache> _mockCache = null!;
    private Mock<IProject> _mockProject = null!;
    private Mock<ILogger<CachedSchemaRepository>> _mockLogger = null!;
    private CachedSchemaRepository _cachedRepository = null!;
    private SemanticModelCacheSettings _cacheSettings = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockInnerRepository = new Mock<ISchemaRepository>();
        _mockCache = new Mock<ISemanticModelCache>();
        _mockProject = new Mock<IProject>();
        _mockLogger = new Mock<ILogger<CachedSchemaRepository>>();

        _cacheSettings = new SemanticModelCacheSettings
        {
            Enabled = true,
            TablesTtlMinutes = 30,
            ViewsTtlMinutes = 25,
            StoredProceduresTtlMinutes = 20,
            ColumnsTtlMinutes = 60,
            SampleDataTtlMinutes = 15,
            ViewDefinitionsTtlMinutes = 45
        };

        var semanticModelSettings = new SemanticModelSettings
        {
            Cache = _cacheSettings
        };

        var projectSettings = new ProjectSettings
        {
            SettingsVersion = new Version(1, 0),
            Database = new DatabaseSettings { Name = "TestDB", ConnectionString = "test" },
            DataDictionary = new DataDictionarySettings(),
            OpenAIService = new OpenAIServiceSettings(),
            SemanticModel = semanticModelSettings
        };

        _mockProject.Setup(p => p.Settings).Returns(projectSettings);

        _cachedRepository = new CachedSchemaRepository(
            _mockInnerRepository.Object,
            _mockCache.Object,
            _mockProject.Object,
            _mockLogger.Object);
    }

    [TestMethod]
    public async Task GetTablesAsync_ShouldUseCache_WhenCacheEnabled()
    {
        // Arrange
        var schema = "dbo";
        var expectedData = new Dictionary<string, TableInfo>
        {
            { "dbo.TestTable", new TableInfo("dbo", "TestTable") }
        };

        _mockCache.Setup(c => c.GetOrSetTablesAsync(
                "tables:dbo",
                It.IsAny<Func<Task<Dictionary<string, TableInfo>>>>(),
                TimeSpan.FromMinutes(30)))
            .ReturnsAsync(expectedData);

        // Act
        var result = await _cachedRepository.GetTablesAsync(schema);

        // Assert
        result.Should().BeEquivalentTo(expectedData);
        _mockCache.Verify(c => c.GetOrSetTablesAsync(
            "tables:dbo",
            It.IsAny<Func<Task<Dictionary<string, TableInfo>>>>(),
            TimeSpan.FromMinutes(30)), Times.Once);
    }

    [TestMethod]
    public async Task GetTablesAsync_ShouldBypassCache_WhenCacheDisabled()
    {
        // Arrange
        _cacheSettings.Enabled = false;
        var schema = "dbo";
        var expectedData = new Dictionary<string, TableInfo>
        {
            { "dbo.TestTable", new TableInfo("dbo", "TestTable") }
        };

        _mockInnerRepository.Setup(r => r.GetTablesAsync(schema))
            .ReturnsAsync(expectedData);

        // Act
        var result = await _cachedRepository.GetTablesAsync(schema);

        // Assert
        result.Should().BeEquivalentTo(expectedData);
        _mockInnerRepository.Verify(r => r.GetTablesAsync(schema), Times.Once);
        _mockCache.Verify(c => c.GetOrSetTablesAsync(
            It.IsAny<string>(),
            It.IsAny<Func<Task<Dictionary<string, TableInfo>>>>(),
            It.IsAny<TimeSpan>()), Times.Never);
    }

    [TestMethod]
    public async Task GetViewsAsync_ShouldUseCache_WhenCacheEnabled()
    {
        // Arrange
        var schema = "dbo";
        var expectedData = new Dictionary<string, ViewInfo>
        {
            { "dbo.TestView", new ViewInfo("dbo", "TestView") }
        };

        _mockCache.Setup(c => c.GetOrSetViewsAsync(
                "views:dbo",
                It.IsAny<Func<Task<Dictionary<string, ViewInfo>>>>(),
                TimeSpan.FromMinutes(25)))
            .ReturnsAsync(expectedData);

        // Act
        var result = await _cachedRepository.GetViewsAsync(schema);

        // Assert
        result.Should().BeEquivalentTo(expectedData);
        _mockCache.Verify(c => c.GetOrSetViewsAsync(
            "views:dbo",
            It.IsAny<Func<Task<Dictionary<string, ViewInfo>>>>(),
            TimeSpan.FromMinutes(25)), Times.Once);
    }

    [TestMethod]
    public async Task GetStoredProceduresAsync_ShouldUseCache_WhenCacheEnabled()
    {
        // Arrange
        var schema = "dbo";
        var expectedData = new Dictionary<string, StoredProcedureInfo>
        {
            { "dbo.TestProc", new StoredProcedureInfo("dbo", "TestProc", "PROCEDURE", null, "CREATE PROCEDURE dbo.TestProc AS BEGIN SELECT 1 END") }
        };

        _mockCache.Setup(c => c.GetOrSetStoredProceduresAsync(
                "storedprocedures:dbo",
                It.IsAny<Func<Task<Dictionary<string, StoredProcedureInfo>>>>(),
                TimeSpan.FromMinutes(20)))
            .ReturnsAsync(expectedData);

        // Act
        var result = await _cachedRepository.GetStoredProceduresAsync(schema);

        // Assert
        result.Should().BeEquivalentTo(expectedData);
        _mockCache.Verify(c => c.GetOrSetStoredProceduresAsync(
            "storedprocedures:dbo",
            It.IsAny<Func<Task<Dictionary<string, StoredProcedureInfo>>>>(),
            TimeSpan.FromMinutes(20)), Times.Once);
    }

    [TestMethod]
    public async Task GetViewDefinitionAsync_ShouldUseCache_WhenCacheEnabled()
    {
        // Arrange
        var view = new ViewInfo("dbo", "TestView");
        var expectedDefinition = "SELECT * FROM TestTable";

        _mockCache.Setup(c => c.GetOrSetViewDefinitionAsync(
                "viewdefinition:dbo:TestView",
                It.IsAny<Func<Task<string>>>(),
                TimeSpan.FromMinutes(45)))
            .ReturnsAsync(expectedDefinition);

        // Act
        var result = await _cachedRepository.GetViewDefinitionAsync(view);

        // Assert
        result.Should().Be(expectedDefinition);
        _mockCache.Verify(c => c.GetOrSetViewDefinitionAsync(
            "viewdefinition:dbo:TestView",
            It.IsAny<Func<Task<string>>>(),
            TimeSpan.FromMinutes(45)), Times.Once);
    }

    [TestMethod]
    public async Task GetColumnsForTableAsync_ShouldUseCache_WhenCacheEnabled()
    {
        // Arrange
        var table = new TableInfo("dbo", "TestTable");
        var expectedColumns = new List<SemanticModelColumn>
        {
            new("TestSchema", "TestColumn")
        };

        _mockCache.Setup(c => c.GetOrSetColumnsAsync(
                "tablecolumns:dbo:TestTable",
                It.IsAny<Func<Task<List<SemanticModelColumn>>>>(),
                TimeSpan.FromMinutes(60)))
            .ReturnsAsync(expectedColumns);

        // Act
        var result = await _cachedRepository.GetColumnsForTableAsync(table);

        // Assert
        result.Should().BeEquivalentTo(expectedColumns);
        _mockCache.Verify(c => c.GetOrSetColumnsAsync(
            "tablecolumns:dbo:TestTable",
            It.IsAny<Func<Task<List<SemanticModelColumn>>>>(),
            TimeSpan.FromMinutes(60)), Times.Once);
    }

    [TestMethod]
    public async Task GetColumnsForViewAsync_ShouldUseCache_WhenCacheEnabled()
    {
        // Arrange
        var view = new ViewInfo("dbo", "TestView");
        var expectedColumns = new List<SemanticModelColumn>
        {
            new("TestSchema", "TestColumn")
        };

        _mockCache.Setup(c => c.GetOrSetColumnsAsync(
                "viewcolumns:dbo:TestView",
                It.IsAny<Func<Task<List<SemanticModelColumn>>>>(),
                TimeSpan.FromMinutes(60)))
            .ReturnsAsync(expectedColumns);

        // Act
        var result = await _cachedRepository.GetColumnsForViewAsync(view);

        // Assert
        result.Should().BeEquivalentTo(expectedColumns);
        _mockCache.Verify(c => c.GetOrSetColumnsAsync(
            "viewcolumns:dbo:TestView",
            It.IsAny<Func<Task<List<SemanticModelColumn>>>>(),
            TimeSpan.FromMinutes(60)), Times.Once);
    }

    [TestMethod]
    public async Task GetSampleTableDataAsync_ShouldUseCache_WhenCacheEnabled()
    {
        // Arrange
        var table = new TableInfo("dbo", "TestTable");
        var numberOfRecords = 10;
        var selectRandom = true;
        var expectedData = new List<Dictionary<string, object>>
        {
            new() { { "Column1", "Value1" } }
        };

        _mockCache.Setup(c => c.GetOrSetSampleDataAsync(
                "tablesampledata:dbo:TestTable:10:True",
                It.IsAny<Func<Task<List<Dictionary<string, object>>>>>(),
                TimeSpan.FromMinutes(15)))
            .ReturnsAsync(expectedData);

        // Act
        var result = await _cachedRepository.GetSampleTableDataAsync(table, numberOfRecords, selectRandom);

        // Assert
        result.Should().BeEquivalentTo(expectedData);
        _mockCache.Verify(c => c.GetOrSetSampleDataAsync(
            "tablesampledata:dbo:TestTable:10:True",
            It.IsAny<Func<Task<List<Dictionary<string, object>>>>>(),
            TimeSpan.FromMinutes(15)), Times.Once);
    }

    [TestMethod]
    public async Task GetSampleViewDataAsync_ShouldUseCache_WhenCacheEnabled()
    {
        // Arrange
        var view = new ViewInfo("dbo", "TestView");
        var numberOfRecords = 5;
        var selectRandom = false;
        var expectedData = new List<Dictionary<string, object>>
        {
            new() { { "Column1", "Value1" } }
        };

        _mockCache.Setup(c => c.GetOrSetSampleDataAsync(
                "viewsampledata:dbo:TestView:5:False",
                It.IsAny<Func<Task<List<Dictionary<string, object>>>>>(),
                TimeSpan.FromMinutes(15)))
            .ReturnsAsync(expectedData);

        // Act
        var result = await _cachedRepository.GetSampleViewDataAsync(view, numberOfRecords, selectRandom);

        // Assert
        result.Should().BeEquivalentTo(expectedData);
        _mockCache.Verify(c => c.GetOrSetSampleDataAsync(
            "viewsampledata:dbo:TestView:5:False",
            It.IsAny<Func<Task<List<Dictionary<string, object>>>>>(),
            TimeSpan.FromMinutes(15)), Times.Once);
    }

    [TestMethod]
    public async Task CreateSemanticModelTableAsync_ShouldNotUseCache()
    {
        // Arrange
        var table = new TableInfo("dbo", "TestTable");
        var expectedSemanticTable = new SemanticModelTable("dbo", "TestTable");

        _mockInnerRepository.Setup(r => r.CreateSemanticModelTableAsync(table))
            .ReturnsAsync(expectedSemanticTable);

        // Act
        var result = await _cachedRepository.CreateSemanticModelTableAsync(table);

        // Assert
        result.Should().BeEquivalentTo(expectedSemanticTable);
        _mockInnerRepository.Verify(r => r.CreateSemanticModelTableAsync(table), Times.Once);
        _mockCache.Verify(c => c.GetOrSetTablesAsync(
            It.IsAny<string>(),
            It.IsAny<Func<Task<Dictionary<string, TableInfo>>>>(),
            It.IsAny<TimeSpan>()), Times.Never);
    }

    [TestMethod]
    public async Task CreateSemanticModelViewAsync_ShouldNotUseCache()
    {
        // Arrange
        var view = new ViewInfo("dbo", "TestView");
        var expectedSemanticView = new SemanticModelView("dbo", "TestView");

        _mockInnerRepository.Setup(r => r.CreateSemanticModelViewAsync(view))
            .ReturnsAsync(expectedSemanticView);

        // Act
        var result = await _cachedRepository.CreateSemanticModelViewAsync(view);

        // Assert
        result.Should().BeEquivalentTo(expectedSemanticView);
        _mockInnerRepository.Verify(r => r.CreateSemanticModelViewAsync(view), Times.Once);
        _mockCache.Verify(c => c.GetOrSetViewsAsync(
            It.IsAny<string>(),
            It.IsAny<Func<Task<Dictionary<string, ViewInfo>>>>(),
            It.IsAny<TimeSpan>()), Times.Never);
    }

    [TestMethod]
    public async Task CreateSemanticModelStoredProcedureAsync_ShouldNotUseCache()
    {
        // Arrange
        var storedProc = new StoredProcedureInfo("dbo", "TestProc", "PROCEDURE", null, "CREATE PROCEDURE dbo.TestProc AS BEGIN SELECT 1 END");
        var expectedSemanticProc = new SemanticModelStoredProcedure("dbo", "TestProc", "CREATE PROCEDURE dbo.TestProc AS BEGIN SELECT 1 END", null);

        _mockInnerRepository.Setup(r => r.CreateSemanticModelStoredProcedureAsync(storedProc))
            .ReturnsAsync(expectedSemanticProc);

        // Act
        var result = await _cachedRepository.CreateSemanticModelStoredProcedureAsync(storedProc);

        // Assert
        result.Should().BeEquivalentTo(expectedSemanticProc);
        _mockInnerRepository.Verify(r => r.CreateSemanticModelStoredProcedureAsync(storedProc), Times.Once);
        _mockCache.Verify(c => c.GetOrSetStoredProceduresAsync(
            It.IsAny<string>(),
            It.IsAny<Func<Task<Dictionary<string, StoredProcedureInfo>>>>(),
            It.IsAny<TimeSpan>()), Times.Never);
    }

    [TestMethod]
    public void Constructor_ShouldThrowArgumentNullException_WhenInnerRepositoryIsNull()
    {
        // Act & Assert
        FluentActions.Invoking(() => new CachedSchemaRepository(
                null!,
                _mockCache.Object,
                _mockProject.Object,
                _mockLogger.Object))
            .Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void Constructor_ShouldThrowArgumentNullException_WhenCacheIsNull()
    {
        // Act & Assert
        FluentActions.Invoking(() => new CachedSchemaRepository(
                _mockInnerRepository.Object,
                null!,
                _mockProject.Object,
                _mockLogger.Object))
            .Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void Constructor_ShouldThrowArgumentNullException_WhenProjectIsNull()
    {
        // Act & Assert
        FluentActions.Invoking(() => new CachedSchemaRepository(
                _mockInnerRepository.Object,
                _mockCache.Object,
                null!,
                _mockLogger.Object))
            .Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        FluentActions.Invoking(() => new CachedSchemaRepository(
                _mockInnerRepository.Object,
                _mockCache.Object,
                _mockProject.Object,
                null!))
            .Should().Throw<ArgumentNullException>();
    }
}