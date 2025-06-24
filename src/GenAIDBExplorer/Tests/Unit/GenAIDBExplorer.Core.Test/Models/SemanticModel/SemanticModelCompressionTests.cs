using System.IO.Compression;
using FluentAssertions;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Models.SemanticModel;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GenAIDBExplorer.Core.Test.Models.SemanticModel;

[TestClass]
public class SemanticModelCompressionTests
{
    private string _tempDirectory = null!;
    private Mock<ILogger> _mockLogger = null!;

    [TestInitialize]
    public void Setup()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDirectory);
        _mockLogger = new Mock<ILogger>();
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [TestMethod]
    public async Task SaveModelAsync_WithCompressionDisabled_SavesUncompressedFiles()
    {
        // Arrange
        var semanticModel = new GenAIDBExplorer.Core.Models.SemanticModel.SemanticModel("TestModel", "TestSource");
        var table = new SemanticModelTable("dbo", "TestTable");
        semanticModel.AddTable(table);

        var compressionSettings = new CompressionSettings { Enabled = false };
        var modelPath = new DirectoryInfo(_tempDirectory);

        // Act
        await semanticModel.SaveModelAsync(modelPath, compressionSettings, _mockLogger.Object);

        // Assert
        File.Exists(Path.Combine(_tempDirectory, "semanticmodel.json")).Should().BeTrue();
        File.Exists(Path.Combine(_tempDirectory, "tables", "dbo.TestTable.json")).Should().BeTrue();
        
        // Verify files are not compressed
        File.Exists(Path.Combine(_tempDirectory, "semanticmodel.json.gz")).Should().BeFalse();
        File.Exists(Path.Combine(_tempDirectory, "tables", "dbo.TestTable.json.gz")).Should().BeFalse();
    }

    [TestMethod]
    public async Task SaveModelAsync_WithCompressionEnabled_SavesCompressedFiles()
    {
        // Arrange
        var semanticModel = new GenAIDBExplorer.Core.Models.SemanticModel.SemanticModel("TestModel", "TestSource");
        var table = new SemanticModelTable("dbo", "TestTable");
        semanticModel.AddTable(table);

        var compressionSettings = new CompressionSettings 
        { 
            Enabled = true,
            Level = CompressionLevel.Optimal,
            FileExtension = ".json.gz"
        };
        var modelPath = new DirectoryInfo(_tempDirectory);

        // Act
        await semanticModel.SaveModelAsync(modelPath, compressionSettings, _mockLogger.Object);

        // Assert
        File.Exists(Path.Combine(_tempDirectory, "semanticmodel.json.gz")).Should().BeTrue();
        File.Exists(Path.Combine(_tempDirectory, "tables", "dbo.TestTable.json.gz")).Should().BeTrue();
        
        // Verify uncompressed files don't exist
        File.Exists(Path.Combine(_tempDirectory, "semanticmodel.json")).Should().BeFalse();
        File.Exists(Path.Combine(_tempDirectory, "tables", "dbo.TestTable.json")).Should().BeFalse();
    }

    [TestMethod]
    public async Task LoadModelAsync_WithUncompressedFiles_LoadsSuccessfully()
    {
        // Arrange
        var originalModel = new GenAIDBExplorer.Core.Models.SemanticModel.SemanticModel("TestModel", "TestSource");
        var table = new SemanticModelTable("dbo", "TestTable");
        originalModel.AddTable(table);

        var compressionSettings = new CompressionSettings { Enabled = false };
        var modelPath = new DirectoryInfo(_tempDirectory);

        // Save original model without compression
        await originalModel.SaveModelAsync(modelPath, compressionSettings);

        var emptyModel = new GenAIDBExplorer.Core.Models.SemanticModel.SemanticModel("Empty", "Empty");

        // Act
        var loadedModel = await emptyModel.LoadModelAsync(modelPath, _mockLogger.Object);

        // Assert
        loadedModel.Name.Should().Be("TestModel");
        loadedModel.Source.Should().Be("TestSource");
        loadedModel.Tables.Should().HaveCount(1);
        loadedModel.Tables[0].Name.Should().Be("TestTable");
        loadedModel.Tables[0].Schema.Should().Be("dbo");
    }

    [TestMethod]
    public async Task LoadModelAsync_WithCompressedFiles_LoadsSuccessfully()
    {
        // Arrange
        var originalModel = new GenAIDBExplorer.Core.Models.SemanticModel.SemanticModel("TestModel", "TestSource");
        var table = new SemanticModelTable("dbo", "TestTable");
        originalModel.AddTable(table);

        var compressionSettings = new CompressionSettings { Enabled = true };
        var modelPath = new DirectoryInfo(_tempDirectory);

        // Save original model with compression
        await originalModel.SaveModelAsync(modelPath, compressionSettings);

        var emptyModel = new GenAIDBExplorer.Core.Models.SemanticModel.SemanticModel("Empty", "Empty");

        // Act
        var loadedModel = await emptyModel.LoadModelAsync(modelPath, _mockLogger.Object);

        // Assert
        loadedModel.Name.Should().Be("TestModel");
        loadedModel.Source.Should().Be("TestSource");
        loadedModel.Tables.Should().HaveCount(1);
        loadedModel.Tables[0].Name.Should().Be("TestTable");
        loadedModel.Tables[0].Schema.Should().Be("dbo");
    }

    [TestMethod]
    public async Task LoadModelAsync_WithMixedCompressionFormats_LoadsSuccessfully()
    {
        // Arrange
        var originalModel = new GenAIDBExplorer.Core.Models.SemanticModel.SemanticModel("TestModel", "TestSource");
        var table1 = new SemanticModelTable("dbo", "Table1");
        var table2 = new SemanticModelTable("dbo", "Table2");
        originalModel.AddTable(table1);
        originalModel.AddTable(table2);

        var modelPath = new DirectoryInfo(_tempDirectory);

        // Save main model with compression
        var compressedSettings = new CompressionSettings { Enabled = true };
        await originalModel.SaveModelAsync(modelPath, compressedSettings);

        // Also create an uncompressed version of one table to test mixed formats
        Directory.CreateDirectory(Path.Combine(_tempDirectory, "tables"));
        await File.WriteAllTextAsync(
            Path.Combine(_tempDirectory, "tables", "dbo.Table2.json"),
            System.Text.Json.JsonSerializer.Serialize(table2, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

        var emptyModel = new GenAIDBExplorer.Core.Models.SemanticModel.SemanticModel("Empty", "Empty");

        // Act
        var loadedModel = await emptyModel.LoadModelAsync(modelPath, _mockLogger.Object);

        // Assert - Should prioritize compressed files but fall back to uncompressed
        loadedModel.Name.Should().Be("TestModel");
        loadedModel.Tables.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task SaveAndLoadRoundTrip_WithAllEntityTypes_PreservesData()
    {
        // Arrange
        var originalModel = new GenAIDBExplorer.Core.Models.SemanticModel.SemanticModel("TestModel", "TestSource", "Test Description");
        
        var table = new SemanticModelTable("dbo", "TestTable", "Table description");
        var column = new SemanticModelColumn("dbo", "ID", "ID column");
        table.AddColumn(column);
        originalModel.AddTable(table);

        var view = new SemanticModelView("dbo", "TestView", "View description");
        view.Definition = "SELECT * FROM TestTable";
        originalModel.AddView(view);

        var storedProc = new SemanticModelStoredProcedure("dbo", "TestProc", "CREATE PROCEDURE...", null, "Proc description");
        originalModel.AddStoredProcedure(storedProc);

        var compressionSettings = new CompressionSettings { Enabled = true };
        var modelPath = new DirectoryInfo(_tempDirectory);

        // Act
        await originalModel.SaveModelAsync(modelPath, compressionSettings);
        
        var emptyModel = new GenAIDBExplorer.Core.Models.SemanticModel.SemanticModel("Empty", "Empty");
        var loadedModel = await emptyModel.LoadModelAsync(modelPath);

        // Assert
        loadedModel.Name.Should().Be(originalModel.Name);
        loadedModel.Source.Should().Be(originalModel.Source);
        loadedModel.Description.Should().Be(originalModel.Description);
        
        loadedModel.Tables.Should().HaveCount(1);
        loadedModel.Tables[0].Name.Should().Be("TestTable");
        loadedModel.Tables[0].Columns.Should().HaveCount(1);
        
        loadedModel.Views.Should().HaveCount(1);
        loadedModel.Views[0].Name.Should().Be("TestView");
        
        loadedModel.StoredProcedures.Should().HaveCount(1);
        loadedModel.StoredProcedures[0].Name.Should().Be("TestProc");
    }

    [TestMethod]
    public async Task LoadModelAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var emptyModel = new GenAIDBExplorer.Core.Models.SemanticModel.SemanticModel("Empty", "Empty");
        var nonExistentPath = new DirectoryInfo(Path.Combine(_tempDirectory, "nonexistent"));

        // Act & Assert
        await Assert.ThrowsExceptionAsync<FileNotFoundException>(
            () => emptyModel.LoadModelAsync(nonExistentPath));
    }
}