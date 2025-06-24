using FluentAssertions;
using GenAIDBExplorer.Core.Models.SemanticModel;

namespace GenAIDBExplorer.Core.Tests.Models.SemanticModel
{
    [TestClass]
    public class SemanticModelIndexConsistencyTests
    {
        private Core.Models.SemanticModel.SemanticModel _semanticModel;

        [TestInitialize]
        public void Setup()
        {
            _semanticModel = new Core.Models.SemanticModel.SemanticModel("TestModel", "TestSource", "Test Description");
        }

        [TestMethod]
        public void PropertySetters_ShouldRebuildIndexes()
        {
            // Arrange - Create entities to set via property
            var table1 = new SemanticModelTable("dbo", "Users", "Users table");
            var table2 = new SemanticModelTable("sales", "Orders", "Orders table");
            var view1 = new SemanticModelView("dbo", "UserView", "User view");
            var sp1 = new SemanticModelStoredProcedure("dbo", "GetUsers", "Get users procedure");

            var tables = new List<SemanticModelTable> { table1, table2 };
            var views = new List<SemanticModelView> { view1 };
            var storedProcedures = new List<SemanticModelStoredProcedure> { sp1 };

            // Act - Set properties directly (simulating JSON deserialization)
            _semanticModel.Tables = tables;
            _semanticModel.Views = views;
            _semanticModel.StoredProcedures = storedProcedures;

            // Assert - Indexes should be rebuilt and lookups should work
            _semanticModel.FindTable("dbo", "Users").Should().Be(table1);
            _semanticModel.FindTable("sales", "Orders").Should().Be(table2);
            _semanticModel.FindView("dbo", "UserView").Should().Be(view1);
            _semanticModel.FindStoredProcedure("dbo", "GetUsers").Should().Be(sp1);

            // Non-existent entities should return null
            _semanticModel.FindTable("dbo", "NonExistent").Should().BeNull();
            _semanticModel.FindView("sales", "NonExistent").Should().BeNull();
            _semanticModel.FindStoredProcedure("hr", "NonExistent").Should().BeNull();
        }

        [TestMethod]
        public void CompositeKeys_ShouldHandleSpecialCharacters()
        {
            // Arrange - Create entities with special characters in schema/name
            var table1 = new SemanticModelTable("my.schema", "table.with.dots", "Table with dots");
            var table2 = new SemanticModelTable("my-schema", "table-with-dashes", "Table with dashes");
            var table3 = new SemanticModelTable("my_schema", "table_with_underscores", "Table with underscores");

            // Act
            _semanticModel.AddTable(table1);
            _semanticModel.AddTable(table2);
            _semanticModel.AddTable(table3);

            // Assert - All should be findable despite special characters
            _semanticModel.FindTable("my.schema", "table.with.dots").Should().Be(table1);
            _semanticModel.FindTable("my-schema", "table-with-dashes").Should().Be(table2);
            _semanticModel.FindTable("my_schema", "table_with_underscores").Should().Be(table3);

            // Edge case: entities with similar but not identical names
            _semanticModel.FindTable("my.schema", "table-with-dots").Should().BeNull();
            _semanticModel.FindTable("my_schema", "table.with.underscores").Should().BeNull();
        }

        [TestMethod]
        public void CaseSensitivity_ShouldBeConsistent()
        {
            // Arrange
            var table1 = new SemanticModelTable("DBO", "Users", "Users table");
            var table2 = new SemanticModelTable("dbo", "USERS", "Users table uppercase");

            // Act
            _semanticModel.AddTable(table1);
            _semanticModel.AddTable(table2);

            // Assert - Should treat case-sensitive names as different entities
            _semanticModel.FindTable("DBO", "Users").Should().Be(table1);
            _semanticModel.FindTable("dbo", "USERS").Should().Be(table2);
            _semanticModel.FindTable("dbo", "Users").Should().BeNull();
            _semanticModel.FindTable("DBO", "USERS").Should().BeNull();

            _semanticModel.Tables.Should().HaveCount(2);
        }

        [TestMethod]
        public void DuplicateKeys_ShouldOverwriteInIndex()
        {
            // Arrange
            var table1 = new SemanticModelTable("dbo", "Users", "First users table");
            var table2 = new SemanticModelTable("dbo", "Users", "Second users table"); // Same schema.name

            // Act
            _semanticModel.AddTable(table1);
            _semanticModel.AddTable(table2);

            // Assert - List should have both entries, but index should have the latest
            _semanticModel.Tables.Should().HaveCount(2);
            _semanticModel.Tables.Should().Contain(table1);
            _semanticModel.Tables.Should().Contain(table2);

            // Index should return the last added table with the same key
            var found = _semanticModel.FindTable("dbo", "Users");
            found.Should().Be(table2);
        }

        [TestMethod]
        public void RemoveOperation_ShouldMaintainIndexConsistency()
        {
            // Arrange
            var table1 = new SemanticModelTable("dbo", "Users", "Users table");
            var table2 = new SemanticModelTable("dbo", "Orders", "Orders table");
            var table3 = new SemanticModelTable("sales", "Products", "Products table");

            _semanticModel.AddTable(table1);
            _semanticModel.AddTable(table2);
            _semanticModel.AddTable(table3);

            // Verify all are present
            _semanticModel.Tables.Should().HaveCount(3);
            _semanticModel.FindTable("dbo", "Users").Should().Be(table1);
            _semanticModel.FindTable("dbo", "Orders").Should().Be(table2);
            _semanticModel.FindTable("sales", "Products").Should().Be(table3);

            // Act - Remove middle table
            var removed = _semanticModel.RemoveTable(table2);

            // Assert
            removed.Should().BeTrue();
            _semanticModel.Tables.Should().HaveCount(2);
            _semanticModel.Tables.Should().Contain(table1);
            _semanticModel.Tables.Should().NotContain(table2);
            _semanticModel.Tables.Should().Contain(table3);

            // Index should be updated
            _semanticModel.FindTable("dbo", "Users").Should().Be(table1);
            _semanticModel.FindTable("dbo", "Orders").Should().BeNull();
            _semanticModel.FindTable("sales", "Products").Should().Be(table3);
        }

        [TestMethod]
        public void EmptyStrings_ShouldBeHandledCorrectly()
        {
            // Arrange - Test edge case with empty strings
            var table1 = new SemanticModelTable("", "EmptySchema", "Table with empty schema");
            var table2 = new SemanticModelTable("dbo", "", "Table with empty name");

            // Act
            _semanticModel.AddTable(table1);
            _semanticModel.AddTable(table2);

            // Assert
            _semanticModel.FindTable("", "EmptySchema").Should().Be(table1);
            _semanticModel.FindTable("dbo", "").Should().Be(table2);

            // Should not find with different empty string combinations
            _semanticModel.FindTable("EmptySchema", "").Should().BeNull();
            _semanticModel.FindTable("", "").Should().BeNull();
        }

        [TestMethod]
        public void LoadModelAsync_ShouldRebuildIndexes()
        {
            // This test would require actual file I/O, so we'll test the RebuildIndexes functionality
            // by directly setting the backing collections and then calling the rebuild

            // Arrange - Add some entities normally
            var table1 = new SemanticModelTable("dbo", "Users", "Users table");
            _semanticModel.AddTable(table1);

            // Verify it's found
            _semanticModel.FindTable("dbo", "Users").Should().Be(table1);

            // Act - Simulate what happens during LoadModelAsync by setting the property
            var newTables = new List<SemanticModelTable>
            {
                new("sales", "Orders", "Orders table"),
                new("hr", "Employees", "Employees table")
            };
            
            _semanticModel.Tables = newTables;

            // Assert - Old table should not be found, new tables should be found
            _semanticModel.FindTable("dbo", "Users").Should().BeNull();
            _semanticModel.FindTable("sales", "Orders").Should().NotBeNull();
            _semanticModel.FindTable("hr", "Employees").Should().NotBeNull();
        }
    }
}