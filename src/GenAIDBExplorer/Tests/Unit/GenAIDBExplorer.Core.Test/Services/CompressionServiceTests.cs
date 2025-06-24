using System.IO.Compression;
using FluentAssertions;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GenAIDBExplorer.Core.Test.Services;

[TestClass]
public class CompressionServiceTests
{
    private ICompressionService _compressionService = null!;
    private string _tempDirectory = null!;

    [TestInitialize]
    public void Setup()
    {
        var mockLogger = new Mock<ILogger<CompressionService>>();
        _compressionService = new CompressionService(mockLogger.Object);
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
    public async Task WriteFileAsync_WithCompressionDisabled_CreatesUncompressedFile()
    {
        // Arrange
        var compressionSettings = new CompressionSettings { Enabled = false };
        var filePath = Path.Combine(_tempDirectory, "test");
        var content = "{ \"test\": \"data\" }";

        // Act
        var result = await _compressionService.WriteFileAsync(filePath, content, compressionSettings);

        // Assert
        result.IsCompressed.Should().BeFalse();
        result.FilePath.Should().EndWith(".json");
        File.Exists(result.FilePath).Should().BeTrue();
        
        var fileContent = await File.ReadAllTextAsync(result.FilePath);
        fileContent.Should().Be(content);
        
        result.OriginalSize.Should().Be(content.Length);
        result.CompressedSize.Should().Be(new FileInfo(result.FilePath).Length);
        result.CompressionRatio.Should().Be(0);
    }

    [TestMethod]
    public async Task WriteFileAsync_WithCompressionEnabled_CreatesCompressedFile()
    {
        // Arrange
        var compressionSettings = new CompressionSettings 
        { 
            Enabled = true, 
            Level = CompressionLevel.Optimal, 
            FileExtension = ".json.gz" 
        };
        var filePath = Path.Combine(_tempDirectory, "test");
        var content = new string('A', 1000); // Large content to ensure compression

        // Act
        var result = await _compressionService.WriteFileAsync(filePath, content, compressionSettings);

        // Assert
        result.IsCompressed.Should().BeTrue();
        result.FilePath.Should().EndWith(".json.gz");
        File.Exists(result.FilePath).Should().BeTrue();
        
        result.OriginalSize.Should().Be(content.Length);
        result.CompressedSize.Should().BeLessThan(result.OriginalSize);
        result.CompressionRatio.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public async Task ReadFileAsync_WithUncompressedFile_ReturnsContent()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "test");
        var content = "{ \"test\": \"data\" }";
        
        // Create uncompressed file
        var compressionSettings = new CompressionSettings { Enabled = false };
        await _compressionService.WriteFileAsync(filePath, content, compressionSettings);

        // Act
        var readContent = await _compressionService.ReadFileAsync(filePath);

        // Assert
        readContent.Should().Be(content);
    }

    [TestMethod]
    public async Task ReadFileAsync_WithCompressedFile_ReturnsDecompressedContent()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "test");
        var content = "{ \"test\": \"data\" }";
        
        // Create compressed file
        var compressionSettings = new CompressionSettings { Enabled = true };
        await _compressionService.WriteFileAsync(filePath, content, compressionSettings);

        // Act
        var readContent = await _compressionService.ReadFileAsync(filePath);

        // Assert
        readContent.Should().Be(content);
    }

    [TestMethod]
    public async Task ReadFileAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "nonexistent");

        // Act & Assert
        await Assert.ThrowsExceptionAsync<FileNotFoundException>(
            () => _compressionService.ReadFileAsync(filePath));
    }

    [TestMethod]
    public void FileExists_WithExistingUncompressedFile_ReturnsTrue()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "test");
        File.WriteAllText(filePath + ".json", "test content");

        // Act
        var exists = _compressionService.FileExists(filePath);

        // Assert
        exists.Should().BeTrue();
    }

    [TestMethod]
    public void FileExists_WithExistingCompressedFile_ReturnsTrue()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "test");
        File.WriteAllText(filePath + ".json.gz", "test content");

        // Act
        var exists = _compressionService.FileExists(filePath);

        // Assert
        exists.Should().BeTrue();
    }

    [TestMethod]
    public void FileExists_WithNonExistentFile_ReturnsFalse()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "nonexistent");

        // Act
        var exists = _compressionService.FileExists(filePath);

        // Assert
        exists.Should().BeFalse();
    }

    [TestMethod]
    public void GetActualFilePath_PrioritizesCompressedFile()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "test");
        File.WriteAllText(filePath + ".json", "uncompressed");
        File.WriteAllText(filePath + ".json.gz", "compressed");

        // Act
        var actualPath = _compressionService.GetActualFilePath(filePath);

        // Assert
        actualPath.Should().EndWith(".json.gz");
    }

    [TestMethod]
    public void GetActualFilePath_WithOnlyUncompressedFile_ReturnsUncompressedPath()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "test");
        File.WriteAllText(filePath + ".json", "uncompressed");

        // Act
        var actualPath = _compressionService.GetActualFilePath(filePath);

        // Assert
        actualPath.Should().EndWith(".json");
    }

    [TestMethod]
    public void GetActualFilePath_WithNonExistentFile_ReturnsNull()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "nonexistent");

        // Act
        var actualPath = _compressionService.GetActualFilePath(filePath);

        // Assert
        actualPath.Should().BeNull();
    }
}