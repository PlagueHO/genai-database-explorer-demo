using System.IO.Compression;
using FluentAssertions;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GenAIDBExplorer.Core.Test.Integration;

[TestClass]
public class CompressionIntegrationTests
{
    private string _tempDirectory = null!;

    [TestInitialize]
    public void Setup()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDirectory);
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
    public async Task FullCompressionWorkflow_WithLargeSemanticModel_AchievesGoodCompressionRatio()
    {
        // Arrange - Create a large semantic model
        var semanticModel = new SemanticModel("LargeDatabase", "Integration test database", "Test compression with large model");

        // Add multiple tables with columns to create substantial JSON content
        for (int tableIndex = 0; tableIndex < 10; tableIndex++)
        {
            var table = new SemanticModelTable($"schema{tableIndex % 3}", $"Table{tableIndex:D3}", $"Description for table {tableIndex} with detailed information about its purpose and usage.");
            
            // Add many columns to each table
            for (int colIndex = 0; colIndex < 20; colIndex++)
            {
                var column = new SemanticModelColumn($"schema{tableIndex % 3}", $"Column{colIndex:D3}", $"Detailed description for column {colIndex} in table {tableIndex} including data type information and usage notes.");
                table.AddColumn(column);
            }
            
            semanticModel.AddTable(table);
        }

        // Add views
        for (int viewIndex = 0; viewIndex < 5; viewIndex++)
        {
            var view = new SemanticModelView($"schema{viewIndex % 2}", $"View{viewIndex:D3}", $"Comprehensive description for view {viewIndex}");
            view.Definition = $"SELECT {string.Join(", ", Enumerable.Range(1, 10).Select(i => $"Column{i:D3}"))} FROM Table{viewIndex:D3} WHERE ComplexCondition = 'Value{viewIndex}'";
            semanticModel.AddView(view);
        }

        // Add stored procedures
        for (int spIndex = 0; spIndex < 3; spIndex++)
        {
            var storedProc = new SemanticModelStoredProcedure(
                $"schema{spIndex}", 
                $"StoredProcedure{spIndex:D3}", 
                $"CREATE PROCEDURE StoredProcedure{spIndex:D3} AS BEGIN SELECT * FROM Table{spIndex:D3} WHERE Column001 = @Parameter{spIndex} END",
                null,
                $"Detailed description for stored procedure {spIndex} including parameter information and return value details.");
            semanticModel.AddStoredProcedure(storedProc);
        }

        var compressionSettings = new CompressionSettings 
        { 
            Enabled = true,
            Level = CompressionLevel.Optimal,
            FileExtension = ".json.gz"
        };
        var mockLogger = new Mock<ILogger>();
        var modelPath = new DirectoryInfo(_tempDirectory);

        // Act - Save with compression
        await semanticModel.SaveModelAsync(modelPath, compressionSettings, mockLogger.Object);

        // Verify compression results
        var compressionService = new CompressionService(Mock.Of<ILogger<CompressionService>>());
        
        // Check that compressed files exist
        File.Exists(Path.Combine(_tempDirectory, "semanticmodel.json.gz")).Should().BeTrue();
        Directory.GetFiles(Path.Combine(_tempDirectory, "tables"), "*.json.gz").Should().HaveCount(10);
        Directory.GetFiles(Path.Combine(_tempDirectory, "views"), "*.json.gz").Should().HaveCount(5);
        Directory.GetFiles(Path.Combine(_tempDirectory, "storedprocedures"), "*.json.gz").Should().HaveCount(3);

        // Verify no uncompressed files exist
        File.Exists(Path.Combine(_tempDirectory, "semanticmodel.json")).Should().BeFalse();
        Directory.GetFiles(Path.Combine(_tempDirectory, "tables"), "*.json").Should().BeEmpty();

        // Calculate total compression savings
        long totalOriginalSize = 0;
        long totalCompressedSize = 0;

        // Check main semantic model file
        var mainModelPath = Path.Combine(_tempDirectory, "semanticmodel");
        var mainResult = await GetCompressionStats(compressionService, mainModelPath, compressionSettings);
        totalOriginalSize += mainResult.OriginalSize;
        totalCompressedSize += mainResult.CompressedSize;

        // Check table files
        foreach (var table in semanticModel.Tables)
        {
            var tablePath = Path.Combine(_tempDirectory, "tables", $"{table.Schema}.{table.Name}");
            var tableResult = await GetCompressionStats(compressionService, tablePath, compressionSettings);
            totalOriginalSize += tableResult.OriginalSize;
            totalCompressedSize += tableResult.CompressedSize;
        }

        // Act - Load compressed model
        var emptyModel = new SemanticModel("Empty", "Empty");
        var loadedModel = await emptyModel.LoadModelAsync(modelPath, mockLogger.Object);

        // Assert - Verify data integrity
        loadedModel.Name.Should().Be("LargeDatabase");
        loadedModel.Source.Should().Be("Integration test database");
        loadedModel.Description.Should().Be("Test compression with large model");
        loadedModel.Tables.Should().HaveCount(10);
        loadedModel.Views.Should().HaveCount(5);
        loadedModel.StoredProcedures.Should().HaveCount(3);

        // Verify table details are preserved
        var firstTable = loadedModel.Tables.First();
        firstTable.Columns.Should().HaveCount(20);
        firstTable.Schema.Should().StartWith("schema");
        firstTable.Name.Should().StartWith("Table");

        // Assert - Verify compression effectiveness
        var overallCompressionRatio = totalOriginalSize > 0 ? 
            (1.0 - (double)totalCompressedSize / totalOriginalSize) * 100 : 0;
        
        // With substantial JSON content, we should achieve good compression
        overallCompressionRatio.Should().BeGreaterThan(30, "Large JSON files with repeated structure should compress well");
        totalCompressedSize.Should().BeLessThan(totalOriginalSize, "Compressed files should be smaller than originals");

        // Log compression results
        Console.WriteLine($"Compression Results:");
        Console.WriteLine($"  Original Size: {totalOriginalSize:N0} bytes");
        Console.WriteLine($"  Compressed Size: {totalCompressedSize:N0} bytes");
        Console.WriteLine($"  Compression Ratio: {overallCompressionRatio:F1}%");
        Console.WriteLine($"  Space Saved: {totalOriginalSize - totalCompressedSize:N0} bytes");
    }

    private async Task<CompressionResult> GetCompressionStats(ICompressionService compressionService, string basePath, CompressionSettings settings)
    {
        // Read the JSON content to get original size
        var jsonContent = await compressionService.ReadFileAsync(basePath);
        var originalSize = System.Text.Encoding.UTF8.GetByteCount(jsonContent);
        
        // Get the compressed file size
        var compressedFilePath = basePath + settings.FileExtension;
        var compressedSize = new FileInfo(compressedFilePath).Length;
        
        return new CompressionResult(compressedFilePath, originalSize, compressedSize, true, TimeSpan.Zero);
    }
}