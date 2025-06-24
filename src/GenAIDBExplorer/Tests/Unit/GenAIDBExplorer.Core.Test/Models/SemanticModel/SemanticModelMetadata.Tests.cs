using FluentAssertions;
using GenAIDBExplorer.Core.Models.SemanticModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GenAIDBExplorer.Core.Tests.Models.SemanticModel;

[TestClass]
public class SemanticModelMetadataTests
{
    [TestMethod]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange
        var name = "TestModel";
        var source = "TestSource";
        var description = "Test description";
        var tableIds = new[] { "dbo.Table1", "dbo.Table2" };
        var viewIds = new[] { "dbo.View1" };
        var spIds = new[] { "dbo.SP1", "dbo.SP2", "dbo.SP3" };

        // Act
        var metadata = new SemanticModelMetadata(name, source, description, tableIds, viewIds, spIds);

        // Assert
        metadata.Name.Should().Be(name);
        metadata.Source.Should().Be(source);
        metadata.Description.Should().Be(description);
        metadata.TableCount.Should().Be(2);
        metadata.ViewCount.Should().Be(1);
        metadata.StoredProcedureCount.Should().Be(3);
        metadata.TableIdentifiers.Should().BeEquivalentTo(tableIds);
        metadata.ViewIdentifiers.Should().BeEquivalentTo(viewIds);
        metadata.StoredProcedureIdentifiers.Should().BeEquivalentTo(spIds);
    }

    [TestMethod]
    public void Constructor_WithEmptyCollections_ShouldReturnZeroCounts()
    {
        // Arrange
        var name = "TestModel";
        var source = "TestSource";

        // Act
        var metadata = new SemanticModelMetadata(name, source, null, [], [], []);

        // Assert
        metadata.TableCount.Should().Be(0);
        metadata.ViewCount.Should().Be(0);
        metadata.StoredProcedureCount.Should().Be(0);
        metadata.TableIdentifiers.Should().BeEmpty();
        metadata.ViewIdentifiers.Should().BeEmpty();
        metadata.StoredProcedureIdentifiers.Should().BeEmpty();
    }

    [TestMethod]
    public async Task LoadFromPathAsync_WhenFileDoesNotExist_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = new DirectoryInfo("NonExistentDirectory");

        // Act
        Func<Task> act = async () => await SemanticModelMetadata.LoadFromPathAsync(nonExistentPath);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }
}