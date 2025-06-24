using FluentAssertions;
using GenAIDBExplorer.Core.Models.Database;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.SemanticModelProviders;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace GenAIDBExplorer.Core.Tests.SemanticModelProviders;

[TestClass]
public class InMemorySemanticModelCacheTests
{
    private IMemoryCache _memoryCache = null!;
    private Mock<ILogger<InMemorySemanticModelCache>> _mockLogger = null!;
    private InMemorySemanticModelCache _cache = null!;

    [TestInitialize]
    public void Setup()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _mockLogger = new Mock<ILogger<InMemorySemanticModelCache>>();
        _cache = new InMemorySemanticModelCache(_memoryCache, _mockLogger.Object);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _cache.Dispose();
        _memoryCache.Dispose();
    }

    [TestMethod]
    public async Task GetOrSetTablesAsync_ShouldExecuteFactory_WhenCacheMiss()
    {
        // Arrange
        var key = "test-tables";
        var ttl = TimeSpan.FromMinutes(30);
        var expectedData = new Dictionary<string, TableInfo>
        {
            { "dbo.TestTable", new TableInfo("dbo", "TestTable") }
        };
        var factoryCalled = false;

        // Act
        var result = await _cache.GetOrSetTablesAsync(key, () =>
        {
            factoryCalled = true;
            return Task.FromResult(expectedData);
        }, ttl);

        // Assert
        factoryCalled.Should().BeTrue();
        result.Should().BeEquivalentTo(expectedData);
        
        var stats = _cache.GetStatistics();
        stats.TotalRequests.Should().Be(1);
        stats.Misses.Should().Be(1);
        stats.Hits.Should().Be(0);
        stats.CacheSize.Should().Be(1);
    }

    [TestMethod]
    public async Task GetOrSetTablesAsync_ShouldReturnCachedData_WhenCacheHit()
    {
        // Arrange
        var key = "test-tables";
        var ttl = TimeSpan.FromMinutes(30);
        var expectedData = new Dictionary<string, TableInfo>
        {
            { "dbo.TestTable", new TableInfo("dbo", "TestTable") }
        };

        // Pre-populate cache
        await _cache.GetOrSetTablesAsync(key, () => Task.FromResult(expectedData), ttl);
        
        var factoryCalled = false;

        // Act
        var result = await _cache.GetOrSetTablesAsync(key, () =>
        {
            factoryCalled = true;
            return Task.FromResult(new Dictionary<string, TableInfo>());
        }, ttl);

        // Assert
        factoryCalled.Should().BeFalse();
        result.Should().BeEquivalentTo(expectedData);
        
        var stats = _cache.GetStatistics();
        stats.TotalRequests.Should().Be(2);
        stats.Misses.Should().Be(1);
        stats.Hits.Should().Be(1);
        stats.HitRatio.Should().Be(50.0);
    }

    [TestMethod]
    public async Task GetOrSetViewsAsync_ShouldWorkCorrectly()
    {
        // Arrange
        var key = "test-views";
        var ttl = TimeSpan.FromMinutes(30);
        var expectedData = new Dictionary<string, ViewInfo>
        {
            { "dbo.TestView", new ViewInfo("dbo", "TestView") }
        };

        // Act
        var result = await _cache.GetOrSetViewsAsync(key, () => Task.FromResult(expectedData), ttl);

        // Assert
        result.Should().BeEquivalentTo(expectedData);
    }

    [TestMethod]
    public async Task GetOrSetStoredProceduresAsync_ShouldWorkCorrectly()
    {
        // Arrange
        var key = "test-storedprocs";
        var ttl = TimeSpan.FromMinutes(30);
        var expectedData = new Dictionary<string, StoredProcedureInfo>
        {
            { "dbo.TestProc", new StoredProcedureInfo("dbo", "TestProc", "PROCEDURE", null, "CREATE PROCEDURE dbo.TestProc AS BEGIN SELECT 1 END") }
        };

        // Act
        var result = await _cache.GetOrSetStoredProceduresAsync(key, () => Task.FromResult(expectedData), ttl);

        // Assert
        result.Should().BeEquivalentTo(expectedData);
    }

    [TestMethod]
    public async Task GetOrSetColumnsAsync_ShouldWorkCorrectly()
    {
        // Arrange
        var key = "test-columns";
        var ttl = TimeSpan.FromMinutes(30);
        var expectedData = new List<SemanticModelColumn>
        {
            new("TestSchema", "TestColumn")
        };

        // Act
        var result = await _cache.GetOrSetColumnsAsync(key, () => Task.FromResult(expectedData), ttl);

        // Assert
        result.Should().BeEquivalentTo(expectedData);
    }

    [TestMethod]
    public async Task GetOrSetSampleDataAsync_ShouldWorkCorrectly()
    {
        // Arrange
        var key = "test-sampledata";
        var ttl = TimeSpan.FromMinutes(30);
        var expectedData = new List<Dictionary<string, object>>
        {
            new() { { "Column1", "Value1" }, { "Column2", 123 } }
        };

        // Act
        var result = await _cache.GetOrSetSampleDataAsync(key, () => Task.FromResult(expectedData), ttl);

        // Assert
        result.Should().BeEquivalentTo(expectedData);
    }

    [TestMethod]
    public async Task GetOrSetViewDefinitionAsync_ShouldWorkCorrectly()
    {
        // Arrange
        var key = "test-viewdef";
        var ttl = TimeSpan.FromMinutes(30);
        var expectedData = "SELECT * FROM TestTable";

        // Act
        var result = await _cache.GetOrSetViewDefinitionAsync(key, () => Task.FromResult(expectedData), ttl);

        // Assert
        result.Should().Be(expectedData);
    }

    [TestMethod]
    public async Task InvalidateKey_ShouldRemoveSpecificKey()
    {
        // Arrange
        var key1 = "test-key1";
        var key2 = "test-key2";
        var ttl = TimeSpan.FromMinutes(30);
        var data = new Dictionary<string, TableInfo>();

        await _cache.GetOrSetTablesAsync(key1, () => Task.FromResult(data), ttl);
        await _cache.GetOrSetTablesAsync(key2, () => Task.FromResult(data), ttl);

        _cache.GetStatistics().CacheSize.Should().Be(2);

        // Act
        _cache.InvalidateKey(key1);

        // Assert
        _cache.GetStatistics().CacheSize.Should().Be(1);
        
        // Verify key1 is gone and key2 still exists
        var factoryCalled = false;
        await _cache.GetOrSetTablesAsync(key1, () =>
        {
            factoryCalled = true;
            return Task.FromResult(data);
        }, ttl);
        factoryCalled.Should().BeTrue();

        factoryCalled = false;
        await _cache.GetOrSetTablesAsync(key2, () =>
        {
            factoryCalled = true;
            return Task.FromResult(data);
        }, ttl);
        factoryCalled.Should().BeFalse();
    }

    [TestMethod]
    public async Task InvalidatePattern_ShouldRemoveMatchingKeys()
    {
        // Arrange
        var ttl = TimeSpan.FromMinutes(30);
        var data = new Dictionary<string, TableInfo>();

        await _cache.GetOrSetTablesAsync("tables:dbo", () => Task.FromResult(data), ttl);
        await _cache.GetOrSetTablesAsync("tables:sales", () => Task.FromResult(data), ttl);
        await _cache.GetOrSetTablesAsync("views:dbo", () => Task.FromResult(data), ttl);

        _cache.GetStatistics().CacheSize.Should().Be(3);

        // Act
        _cache.InvalidatePattern("tables:.*");

        // Assert
        _cache.GetStatistics().CacheSize.Should().Be(1);
    }

    [TestMethod]
    public async Task Clear_ShouldRemoveAllKeys()
    {
        // Arrange
        var ttl = TimeSpan.FromMinutes(30);
        var data = new Dictionary<string, TableInfo>();

        await _cache.GetOrSetTablesAsync("key1", () => Task.FromResult(data), ttl);
        await _cache.GetOrSetTablesAsync("key2", () => Task.FromResult(data), ttl);
        await _cache.GetOrSetTablesAsync("key3", () => Task.FromResult(data), ttl);

        _cache.GetStatistics().CacheSize.Should().Be(3);

        // Act
        _cache.Clear();

        // Assert
        _cache.GetStatistics().CacheSize.Should().Be(0);
    }

    [TestMethod]
    public void GetStatistics_ShouldReturnCorrectValues()
    {
        // Arrange
        var stats = _cache.GetStatistics();

        // Assert
        stats.TotalRequests.Should().Be(0);
        stats.Hits.Should().Be(0);
        stats.Misses.Should().Be(0);
        stats.HitRatio.Should().Be(0);
        stats.CacheSize.Should().Be(0);
    }

    [TestMethod]
    public async Task GetOrSetTablesAsync_ShouldThrowArgumentNullException_WhenKeyIsNull()
    {
        // Act & Assert
        await FluentActions.Invoking(() => _cache.GetOrSetTablesAsync(null!, () => Task.FromResult(new Dictionary<string, TableInfo>()), TimeSpan.FromMinutes(1)))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [TestMethod]
    public async Task GetOrSetTablesAsync_ShouldThrowArgumentNullException_WhenFactoryIsNull()
    {
        // Act & Assert
        await FluentActions.Invoking(() => _cache.GetOrSetTablesAsync("key", null!, TimeSpan.FromMinutes(1)))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [TestMethod]
    public void InvalidateKey_ShouldThrowArgumentNullException_WhenKeyIsNull()
    {
        // Act & Assert
        FluentActions.Invoking(() => _cache.InvalidateKey(null!))
            .Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void InvalidatePattern_ShouldThrowArgumentNullException_WhenPatternIsNull()
    {
        // Act & Assert
        FluentActions.Invoking(() => _cache.InvalidatePattern(null!))
            .Should().Throw<ArgumentNullException>();
    }
}