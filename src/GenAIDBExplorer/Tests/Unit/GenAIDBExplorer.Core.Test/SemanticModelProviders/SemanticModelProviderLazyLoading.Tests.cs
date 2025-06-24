using FluentAssertions;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.SemanticModelProviders;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GenAIDBExplorer.Core.Tests.SemanticModelProviders;

[TestClass]
public class SemanticModelProviderLazyLoadingTests
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

        _provider = new SemanticModelProvider(_mockProject.Object, _mockSchemaRepository.Object, _mockLogger.Object);
    }

    [TestMethod]
    public async Task LoadModelMetadataAsync_WithNonExistentPath_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = new DirectoryInfo("NonExistentDirectory");

        // Act
        Func<Task> act = async () => await _provider.LoadModelMetadataAsync(nonExistentPath);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [TestMethod]
    public async Task LoadModelLazyAsync_WithNonExistentPath_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = new DirectoryInfo("NonExistentDirectory");

        // Act
        Func<Task> act = async () => await _provider.LoadModelLazyAsync(nonExistentPath);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }
}