using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GenAIDBExplorer.Core.Models.SemanticModel;

namespace GenAIDBExplorer.Core.Tests.Models.SemanticModel
{
    [TestClass]
    public class SemanticModelStreamingTests
    {
        private SemanticModel CreateTestSemanticModel()
        {
            var semanticModel = new SemanticModel("TestDatabase", "TestSource", "Test database description");

            // Add a test table with columns and indexes
            var table = new SemanticModelTable("dbo", "TestTable", "Test table description");
            table.SemanticDescription = "This is a test table";
            table.Details = "Table details from data dictionary";
            table.AdditionalInformation = "Additional business rules information";
            
            var column1 = new SemanticModelColumn("dbo", "ID", "Primary key column")
            {
                Type = "int",
                IsPrimaryKey = true,
                IsNullable = false
            };
            var column2 = new SemanticModelColumn("dbo", "Name", "Name column")
            {
                Type = "nvarchar(50)",
                IsNullable = true
            };
            table.AddColumn(column1);
            table.AddColumn(column2);

            var index = new SemanticModelIndex("dbo", "PK_TestTable", "ID", "Primary key index")
            {
                IsPrimaryKey = true,
                IsUnique = true
            };
            table.AddIndex(index);

            semanticModel.AddTable(table);

            // Add a test view
            var view = new SemanticModelView("dbo", "TestView", "Test view description");
            view.Definition = "SELECT * FROM dbo.TestTable";
            view.SemanticDescription = "This is a test view";
            view.AdditionalInformation = "View business rules information";

            var viewColumn = new SemanticModelColumn("dbo", "ID", "ID from table")
            {
                Type = "int",
                IsNullable = false
            };
            view.AddColumn(viewColumn);

            semanticModel.AddView(view);

            // Add a test stored procedure
            var storedProcedure = new SemanticModelStoredProcedure(
                "dbo", 
                "TestProcedure", 
                "CREATE PROCEDURE dbo.TestProcedure AS SELECT 1",
                "@Param1 INT",
                "Test stored procedure description");
            storedProcedure.SemanticDescription = "This is a test stored procedure";

            semanticModel.AddStoredProcedure(storedProcedure);

            return semanticModel;
        }

        [TestMethod]
        public async Task SaveStreamAsync_WithValidModel_ShouldSerializeToStream()
        {
            // Arrange
            var semanticModel = CreateTestSemanticModel();
            using var stream = new MemoryStream();

            // Act
            await semanticModel.SaveStreamAsync(stream);

            // Assert
            stream.Position = 0;
            var jsonContent = Encoding.UTF8.GetString(stream.ToArray());
            
            jsonContent.Should().Contain($"\"Name\": \"{semanticModel.Name}\"");
            jsonContent.Should().Contain($"\"Source\": \"{semanticModel.Source}\"");
            jsonContent.Should().Contain($"\"Description\": \"{semanticModel.Description}\"");
            jsonContent.Should().Contain("\"Tables\":");
            jsonContent.Should().Contain("\"Views\":");
            jsonContent.Should().Contain("\"StoredProcedures\":");
            jsonContent.Should().Contain("\"TestTable\"");
            jsonContent.Should().Contain("\"TestView\"");
            jsonContent.Should().Contain("\"TestProcedure\"");
        }

        [TestMethod]
        public async Task LoadStreamAsync_WithValidStream_ShouldDeserializeModel()
        {
            // Arrange
            var originalModel = CreateTestSemanticModel();
            using var stream = new MemoryStream();
            await originalModel.SaveStreamAsync(stream);
            stream.Position = 0;

            // Act
            var loadedModel = await originalModel.LoadStreamAsync(stream);

            // Assert
            loadedModel.Should().NotBeNull();
            loadedModel.Name.Should().Be(originalModel.Name);
            loadedModel.Source.Should().Be(originalModel.Source);
            loadedModel.Description.Should().Be(originalModel.Description);
            
            loadedModel.Tables.Should().HaveCount(originalModel.Tables.Count);
            loadedModel.Views.Should().HaveCount(originalModel.Views.Count);
            loadedModel.StoredProcedures.Should().HaveCount(originalModel.StoredProcedures.Count);

            // Verify table details
            var loadedTable = loadedModel.Tables[0];
            var originalTable = originalModel.Tables[0];
            loadedTable.Schema.Should().Be(originalTable.Schema);
            loadedTable.Name.Should().Be(originalTable.Name);
            loadedTable.Description.Should().Be(originalTable.Description);
            loadedTable.SemanticDescription.Should().Be(originalTable.SemanticDescription);
            loadedTable.Details.Should().Be(originalTable.Details);
            loadedTable.AdditionalInformation.Should().Be(originalTable.AdditionalInformation);
            loadedTable.Columns.Should().HaveCount(originalTable.Columns.Count);
            loadedTable.Indexes.Should().HaveCount(originalTable.Indexes.Count);

            // Verify view details
            var loadedView = loadedModel.Views[0];
            var originalView = originalModel.Views[0];
            loadedView.Schema.Should().Be(originalView.Schema);
            loadedView.Name.Should().Be(originalView.Name);
            loadedView.Description.Should().Be(originalView.Description);
            loadedView.SemanticDescription.Should().Be(originalView.SemanticDescription);
            loadedView.Definition.Should().Be(originalView.Definition);
            loadedView.AdditionalInformation.Should().Be(originalView.AdditionalInformation);
            loadedView.Columns.Should().HaveCount(originalView.Columns.Count);

            // Verify stored procedure details
            var loadedSp = loadedModel.StoredProcedures[0];
            var originalSp = originalModel.StoredProcedures[0];
            loadedSp.Schema.Should().Be(originalSp.Schema);
            loadedSp.Name.Should().Be(originalSp.Name);
            loadedSp.Description.Should().Be(originalSp.Description);
            loadedSp.SemanticDescription.Should().Be(originalSp.SemanticDescription);
            loadedSp.Definition.Should().Be(originalSp.Definition);
            loadedSp.Parameters.Should().Be(originalSp.Parameters);
        }

        [TestMethod]
        public async Task SaveStreamAsync_WithCancellationToken_ShouldRespectCancellation()
        {
            // Arrange
            var semanticModel = CreateTestSemanticModel();
            using var stream = new MemoryStream();
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            var act = async () => await semanticModel.SaveStreamAsync(stream, cancellationTokenSource.Token);
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [TestMethod]
        public async Task LoadStreamAsync_WithCancellationToken_ShouldRespectCancellation()
        {
            // Arrange
            var semanticModel = CreateTestSemanticModel();
            using var stream = new MemoryStream();
            await semanticModel.SaveStreamAsync(stream);
            stream.Position = 0;

            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            var act = async () => await semanticModel.LoadStreamAsync(stream, cancellationTokenSource.Token);
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [TestMethod]
        public async Task SaveStreamAsync_WithEmptyModel_ShouldSerializeEmptyArrays()
        {
            // Arrange
            var semanticModel = new SemanticModel("EmptyDatabase", "EmptySource");
            using var stream = new MemoryStream();

            // Act
            await semanticModel.SaveStreamAsync(stream);

            // Assert
            stream.Position = 0;
            var jsonContent = Encoding.UTF8.GetString(stream.ToArray());
            
            jsonContent.Should().Contain("\"Tables\": []");
            jsonContent.Should().Contain("\"Views\": []");
            jsonContent.Should().Contain("\"StoredProcedures\": []");
        }

        [TestMethod]
        public async Task LoadStreamAsync_WithEmptyArrays_ShouldCreateEmptyCollections()
        {
            // Arrange
            var originalModel = new SemanticModel("EmptyDatabase", "EmptySource");
            using var stream = new MemoryStream();
            await originalModel.SaveStreamAsync(stream);
            stream.Position = 0;

            // Act
            var loadedModel = await originalModel.LoadStreamAsync(stream);

            // Assert
            loadedModel.Should().NotBeNull();
            loadedModel.Tables.Should().BeEmpty();
            loadedModel.Views.Should().BeEmpty();
            loadedModel.StoredProcedures.Should().BeEmpty();
        }

        [TestMethod]
        public async Task LoadStreamAsync_WithInvalidStream_ShouldThrowException()
        {
            // Arrange
            var semanticModel = new SemanticModel("Test", "Test");
            var invalidJson = "{ invalid json }";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(invalidJson));

            // Act & Assert
            var act = async () => await semanticModel.LoadStreamAsync(stream);
            await act.Should().ThrowAsync<JsonException>();
        }

        [TestMethod]
        public async Task LoadStreamAsync_WithMissingRequiredProperties_ShouldThrowException()
        {
            // Arrange
            var semanticModel = new SemanticModel("Test", "Test");
            var invalidJson = "{ \"Description\": \"Missing required properties\" }";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(invalidJson));

            // Act & Assert
            var act = async () => await semanticModel.LoadStreamAsync(stream);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [TestMethod]
        public async Task RoundTrip_ComplexModel_ShouldPreserveAllData()
        {
            // Arrange
            var originalModel = CreateTestSemanticModel();
            
            // Add additional complexity
            var largeTable = new SemanticModelTable("dbo", "LargeTable", "Large table for testing");
            for (int i = 0; i < 100; i++)
            {
                var column = new SemanticModelColumn("dbo", $"Column{i}", $"Column {i} description")
                {
                    Type = i % 2 == 0 ? "int" : "nvarchar(50)",
                    IsNullable = i % 3 == 0,
                    IsPrimaryKey = i == 0,
                    IsForeignKey = i % 5 == 0,
                    ForeignKeyTable = i % 5 == 0 ? "RefTable" : null,
                    ForeignKeyColumn = i % 5 == 0 ? "RefColumn" : null
                };
                largeTable.AddColumn(column);
            }
            originalModel.AddTable(largeTable);

            using var stream = new MemoryStream();

            // Act
            await originalModel.SaveStreamAsync(stream);
            stream.Position = 0;
            var loadedModel = await originalModel.LoadStreamAsync(stream);

            // Assert
            loadedModel.Should().NotBeNull();
            loadedModel.Tables.Should().HaveCount(originalModel.Tables.Count);
            
            var loadedLargeTable = loadedModel.FindTable("dbo", "LargeTable");
            loadedLargeTable.Should().NotBeNull();
            loadedLargeTable!.Columns.Should().HaveCount(100);
            
            for (int i = 0; i < 100; i++)
            {
                var loadedColumn = loadedLargeTable.Columns[i];
                var originalColumn = largeTable.Columns[i];
                
                loadedColumn.Name.Should().Be(originalColumn.Name);
                loadedColumn.Type.Should().Be(originalColumn.Type);
                loadedColumn.IsNullable.Should().Be(originalColumn.IsNullable);
                loadedColumn.IsPrimaryKey.Should().Be(originalColumn.IsPrimaryKey);
                loadedColumn.IsForeignKey.Should().Be(originalColumn.IsForeignKey);
                loadedColumn.ForeignKeyTable.Should().Be(originalColumn.ForeignKeyTable);
                loadedColumn.ForeignKeyColumn.Should().Be(originalColumn.ForeignKeyColumn);
            }
        }

        [TestMethod]
        public async Task StreamingVsDirectoryMethods_ShouldProduceEquivalentResults()
        {
            // Arrange
            var originalModel = CreateTestSemanticModel();
            
            // Use streaming method
            using var stream = new MemoryStream();
            await originalModel.SaveStreamAsync(stream);
            stream.Position = 0;
            var streamLoadedModel = await originalModel.LoadStreamAsync(stream);

            // Use directory method
            var tempDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
            try
            {
                await originalModel.SaveModelAsync(tempDir);
                var directoryLoadedModel = await originalModel.LoadModelAsync(tempDir);

                // Assert - both methods should produce equivalent models
                streamLoadedModel.Name.Should().Be(directoryLoadedModel.Name);
                streamLoadedModel.Source.Should().Be(directoryLoadedModel.Source);
                streamLoadedModel.Description.Should().Be(directoryLoadedModel.Description);
                streamLoadedModel.Tables.Should().HaveCount(directoryLoadedModel.Tables.Count);
                streamLoadedModel.Views.Should().HaveCount(directoryLoadedModel.Views.Count);
                streamLoadedModel.StoredProcedures.Should().HaveCount(directoryLoadedModel.StoredProcedures.Count);
            }
            finally
            {
                if (tempDir.Exists)
                {
                    tempDir.Delete(true);
                }
            }
        }

        [TestMethod]
        public async Task SaveLoadStreamAsync_WithAdditionalProperties_ShouldPreserveAllProperties()
        {
            // Arrange
            var semanticModel = new SemanticModel("TestDB", "TestSource", "Test description");
            
            var table = new SemanticModelTable("dbo", "TestTable", "Table description");
            table.Details = "Detailed table information";
            table.AdditionalInformation = "Business rules and constraints";
            table.SemanticDescription = "Semantic description";
            table.NotUsed = true;
            table.NotUsedReason = "Deprecated table";
            semanticModel.AddTable(table);

            var view = new SemanticModelView("dbo", "TestView", "View description");
            view.AdditionalInformation = "View business rules";
            view.Definition = "SELECT * FROM TestTable";
            view.SemanticDescription = "View semantic description";
            view.NotUsed = false;
            semanticModel.AddView(view);

            using var stream = new MemoryStream();

            // Act
            await semanticModel.SaveStreamAsync(stream);
            stream.Position = 0;
            var loadedModel = await semanticModel.LoadStreamAsync(stream);

            // Assert
            var loadedTable = loadedModel.Tables[0];
            loadedTable.Details.Should().Be(table.Details);
            loadedTable.AdditionalInformation.Should().Be(table.AdditionalInformation);
            loadedTable.SemanticDescription.Should().Be(table.SemanticDescription);
            loadedTable.NotUsed.Should().Be(table.NotUsed);
            loadedTable.NotUsedReason.Should().Be(table.NotUsedReason);

            var loadedView = loadedModel.Views[0];
            loadedView.AdditionalInformation.Should().Be(view.AdditionalInformation);
            loadedView.Definition.Should().Be(view.Definition);
            loadedView.SemanticDescription.Should().Be(view.SemanticDescription);
            loadedView.NotUsed.Should().Be(view.NotUsed);
        }
    }
}