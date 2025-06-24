using System.Diagnostics;
using FluentAssertions;
using GenAIDBExplorer.Core.Models.Database;
using GenAIDBExplorer.Core.Models.SemanticModel;

namespace GenAIDBExplorer.Core.Tests.Models.SemanticModel
{
    [TestClass]
    public class SemanticModelTests
    {
        private Core.Models.SemanticModel.SemanticModel _semanticModel;

        [TestInitialize]
        public void Setup()
        {
            _semanticModel = new Core.Models.SemanticModel.SemanticModel("TestModel", "TestSource", "Test Description");
        }

        [TestClass]
        public class FindTableTests : SemanticModelTests
        {
            [TestMethod]
            public void FindTable_WithExistingTable_ShouldReturnTable()
            {
                // Arrange
                var table = new SemanticModelTable("dbo", "Users", "Users table");
                _semanticModel.AddTable(table);

                // Act
                var result = _semanticModel.FindTable("dbo", "Users");

                // Assert
                result.Should().NotBeNull();
                result.Should().Be(table);
                result!.Schema.Should().Be("dbo");
                result.Name.Should().Be("Users");
            }

            [TestMethod]
            public void FindTable_WithNonExistingTable_ShouldReturnNull()
            {
                // Arrange
                var table = new SemanticModelTable("dbo", "Users", "Users table");
                _semanticModel.AddTable(table);

                // Act
                var result = _semanticModel.FindTable("dbo", "NonExisting");

                // Assert
                result.Should().BeNull();
            }

            [TestMethod]
            public void FindTable_WithDifferentSchema_ShouldReturnNull()
            {
                // Arrange
                var table = new SemanticModelTable("dbo", "Users", "Users table");
                _semanticModel.AddTable(table);

                // Act
                var result = _semanticModel.FindTable("sales", "Users");

                // Assert
                result.Should().BeNull();
            }

            [TestMethod]
            public void FindTable_WithMultipleTables_ShouldReturnCorrectTable()
            {
                // Arrange
                var table1 = new SemanticModelTable("dbo", "Users", "Users table");
                var table2 = new SemanticModelTable("dbo", "Orders", "Orders table");
                var table3 = new SemanticModelTable("sales", "Users", "Sales Users table");
                
                _semanticModel.AddTable(table1);
                _semanticModel.AddTable(table2);
                _semanticModel.AddTable(table3);

                // Act
                var result1 = _semanticModel.FindTable("dbo", "Users");
                var result2 = _semanticModel.FindTable("dbo", "Orders");
                var result3 = _semanticModel.FindTable("sales", "Users");

                // Assert
                result1.Should().Be(table1);
                result2.Should().Be(table2);
                result3.Should().Be(table3);
            }
        }

        [TestClass]
        public class FindViewTests : SemanticModelTests
        {
            [TestMethod]
            public void FindView_WithExistingView_ShouldReturnView()
            {
                // Arrange
                var view = new SemanticModelView("dbo", "UserView", "User view");
                _semanticModel.AddView(view);

                // Act
                var result = _semanticModel.FindView("dbo", "UserView");

                // Assert
                result.Should().NotBeNull();
                result.Should().Be(view);
                result!.Schema.Should().Be("dbo");
                result.Name.Should().Be("UserView");
            }

            [TestMethod]
            public void FindView_WithNonExistingView_ShouldReturnNull()
            {
                // Arrange
                var view = new SemanticModelView("dbo", "UserView", "User view");
                _semanticModel.AddView(view);

                // Act
                var result = _semanticModel.FindView("dbo", "NonExisting");

                // Assert
                result.Should().BeNull();
            }
        }

        [TestClass]
        public class FindStoredProcedureTests : SemanticModelTests
        {
            [TestMethod]
            public void FindStoredProcedure_WithExistingProcedure_ShouldReturnProcedure()
            {
                // Arrange
                var storedProcedure = new SemanticModelStoredProcedure("dbo", "GetUsers", "Get users procedure");
                _semanticModel.AddStoredProcedure(storedProcedure);

                // Act
                var result = _semanticModel.FindStoredProcedure("dbo", "GetUsers");

                // Assert
                result.Should().NotBeNull();
                result.Should().Be(storedProcedure);
                result!.Schema.Should().Be("dbo");
                result.Name.Should().Be("GetUsers");
            }

            [TestMethod]
            public void FindStoredProcedure_WithNonExistingProcedure_ShouldReturnNull()
            {
                // Arrange
                var storedProcedure = new SemanticModelStoredProcedure("dbo", "GetUsers", "Get users procedure");
                _semanticModel.AddStoredProcedure(storedProcedure);

                // Act
                var result = _semanticModel.FindStoredProcedure("dbo", "NonExisting");

                // Assert
                result.Should().BeNull();
            }
        }

        [TestClass]
        public class SelectTablesTests : SemanticModelTests
        {
            [TestMethod]
            public void SelectTables_WithMatchingTables_ShouldReturnCorrectTables()
            {
                // Arrange
                var table1 = new SemanticModelTable("dbo", "Users", "Users table");
                var table2 = new SemanticModelTable("dbo", "Orders", "Orders table");
                var table3 = new SemanticModelTable("sales", "Products", "Products table");
                
                _semanticModel.AddTable(table1);
                _semanticModel.AddTable(table2);
                _semanticModel.AddTable(table3);

                var tableList = new TableList
                {
                    Tables = new List<TableInfo>
                    {
                        new("dbo", "Users"),
                        new("sales", "Products")
                    }
                };

                // Act
                var result = _semanticModel.SelectTables(tableList);

                // Assert
                result.Should().HaveCount(2);
                result.Should().Contain(table1);
                result.Should().Contain(table3);
                result.Should().NotContain(table2);
            }

            [TestMethod]
            public void SelectTables_WithNonMatchingTables_ShouldReturnEmptyList()
            {
                // Arrange
                var table1 = new SemanticModelTable("dbo", "Users", "Users table");
                _semanticModel.AddTable(table1);

                var tableList = new TableList
                {
                    Tables = new List<TableInfo>
                    {
                        new("dbo", "NonExisting")
                    }
                };

                // Act
                var result = _semanticModel.SelectTables(tableList);

                // Assert
                result.Should().BeEmpty();
            }
        }

        [TestClass]
        public class PerformanceTests : SemanticModelTests
        {
            [TestMethod]
            public void FindTable_WithLargeDataset_ShouldPerformWell()
            {
                // Arrange - Create a large dataset
                const int numberOfTables = 1000;
                for (int i = 0; i < numberOfTables; i++)
                {
                    var table = new SemanticModelTable($"schema{i}", $"Table{i}", $"Table {i} description");
                    _semanticModel.AddTable(table);
                }

                // Add target table at the end to simulate worst-case for linear search
                var targetTable = new SemanticModelTable("target_schema", "target_table", "Target table");
                _semanticModel.AddTable(targetTable);

                // Act & Assert - Measure performance
                var stopwatch = Stopwatch.StartNew();
                var result = _semanticModel.FindTable("target_schema", "target_table");
                stopwatch.Stop();

                // Assert
                result.Should().NotBeNull();
                result.Should().Be(targetTable);
                
                // Performance should be reasonable (this will improve significantly with indexed implementation)
                stopwatch.ElapsedMilliseconds.Should().BeLessThan(100, "Find operation should complete quickly");
            }

            [TestMethod]
            public void SelectTables_WithLargeDataset_ShouldPerformWell()
            {
                // Arrange - Create a large dataset
                const int numberOfTables = 500;
                var targetTables = new List<SemanticModelTable>();
                
                for (int i = 0; i < numberOfTables; i++)
                {
                    var table = new SemanticModelTable($"schema{i}", $"Table{i}", $"Table {i} description");
                    _semanticModel.AddTable(table);
                    
                    // Mark every 10th table as a target
                    if (i % 10 == 0)
                    {
                        targetTables.Add(table);
                    }
                }

                var tableList = new TableList
                {
                    Tables = targetTables.Select(t => new TableInfo(t.Schema, t.Name)).ToList()
                };

                // Act & Assert - Measure performance
                var stopwatch = Stopwatch.StartNew();
                var result = _semanticModel.SelectTables(tableList);
                stopwatch.Stop();

                // Assert
                result.Should().HaveCount(targetTables.Count);
                foreach (var targetTable in targetTables)
                {
                    result.Should().Contain(targetTable);
                }
                
                // Performance should be reasonable
                stopwatch.ElapsedMilliseconds.Should().BeLessThan(200, "SelectTables operation should complete quickly");
            }
        }

        [TestClass]
        public class AddRemoveTests : SemanticModelTests
        {
            [TestMethod]
            public void AddTable_ShouldAddToCollection()
            {
                // Arrange
                var table = new SemanticModelTable("dbo", "Users", "Users table");

                // Act
                _semanticModel.AddTable(table);

                // Assert
                _semanticModel.Tables.Should().ContainSingle().Which.Should().Be(table);
                _semanticModel.FindTable("dbo", "Users").Should().Be(table);
            }

            [TestMethod]
            public void RemoveTable_ShouldRemoveFromCollection()
            {
                // Arrange
                var table = new SemanticModelTable("dbo", "Users", "Users table");
                _semanticModel.AddTable(table);

                // Act
                var result = _semanticModel.RemoveTable(table);

                // Assert
                result.Should().BeTrue();
                _semanticModel.Tables.Should().BeEmpty();
                _semanticModel.FindTable("dbo", "Users").Should().BeNull();
            }

            [TestMethod]
            public void AddRemove_MultipleOperations_ShouldMaintainConsistency()
            {
                // Arrange
                var table1 = new SemanticModelTable("dbo", "Users", "Users table");
                var table2 = new SemanticModelTable("dbo", "Orders", "Orders table");
                var table3 = new SemanticModelTable("sales", "Users", "Sales Users table");

                // Act & Assert
                _semanticModel.AddTable(table1);
                _semanticModel.AddTable(table2);
                _semanticModel.AddTable(table3);

                _semanticModel.Tables.Should().HaveCount(3);
                _semanticModel.FindTable("dbo", "Users").Should().Be(table1);
                _semanticModel.FindTable("dbo", "Orders").Should().Be(table2);
                _semanticModel.FindTable("sales", "Users").Should().Be(table3);

                _semanticModel.RemoveTable(table2);
                
                _semanticModel.Tables.Should().HaveCount(2);
                _semanticModel.FindTable("dbo", "Users").Should().Be(table1);
                _semanticModel.FindTable("dbo", "Orders").Should().BeNull();
                _semanticModel.FindTable("sales", "Users").Should().Be(table3);
            }
        }
    }
}