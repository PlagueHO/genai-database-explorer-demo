using System.Diagnostics;
using FluentAssertions;
using GenAIDBExplorer.Core.Models.Database;
using GenAIDBExplorer.Core.Models.SemanticModel;

namespace GenAIDBExplorer.Core.Tests.Models.SemanticModel
{
    [TestClass]
    public class SemanticModelPerformanceTests
    {
        private Core.Models.SemanticModel.SemanticModel _semanticModel;

        [TestInitialize]
        public void Setup()
        {
            _semanticModel = new Core.Models.SemanticModel.SemanticModel("TestModel", "TestSource", "Test Description");
        }

        [TestMethod]
        public void IndexedLookup_Performance_ShouldBeFasterThanLinearSearch()
        {
            // Arrange - Create a dataset with 10,000 entities
            const int numberOfTables = 10000;
            
            // Add tables with known patterns
            for (int i = 0; i < numberOfTables; i++)
            {
                var table = new SemanticModelTable($"schema_{i % 100}", $"table_{i}", $"Table {i} description");
                _semanticModel.AddTable(table);
            }

            // Add target table at the end to simulate worst-case for linear search
            var targetTable = new SemanticModelTable("target_schema", "target_table", "Target table");
            _semanticModel.AddTable(targetTable);

            // Act - Perform multiple lookups and measure average time
            const int numberOfLookups = 1000;
            var stopwatch = Stopwatch.StartNew();
            
            SemanticModelTable? foundTable = null;
            for (int i = 0; i < numberOfLookups; i++)
            {
                foundTable = _semanticModel.FindTable("target_schema", "target_table");
            }
            
            stopwatch.Stop();
            var averageTimePerLookup = stopwatch.ElapsedTicks / (double)numberOfLookups;

            // Assert
            foundTable.Should().NotBeNull();
            foundTable.Should().Be(targetTable);
            
            // With O(1) indexed lookup, even with 10K+ entities, each lookup should be very fast
            // Average time per lookup should be less than 1000 ticks (roughly < 0.1ms on most systems)
            averageTimePerLookup.Should().BeLessThan(1000, 
                "Indexed lookup should provide O(1) performance regardless of dataset size");

            // Total time for 1000 lookups should be well under 100ms
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(100, 
                "1000 indexed lookups should complete in under 100ms");
        }

        [TestMethod]
        public void SelectTables_Performance_ShouldScaleWellWithLargeDatasets()
        {
            // Arrange - Create a large dataset
            const int numberOfTables = 5000;
            var targetTables = new List<SemanticModelTable>();
            
            for (int i = 0; i < numberOfTables; i++)
            {
                var table = new SemanticModelTable($"schema_{i % 50}", $"table_{i}", $"Table {i} description");
                _semanticModel.AddTable(table);
                
                // Mark every 25th table as a target (200 target tables)
                if (i % 25 == 0)
                {
                    targetTables.Add(table);
                }
            }

            var tableList = new TableList
            {
                Tables = targetTables.Select(t => new TableInfo(t.Schema, t.Name)).ToList()
            };

            // Act - Measure SelectTables performance
            var stopwatch = Stopwatch.StartNew();
            var result = _semanticModel.SelectTables(tableList);
            stopwatch.Stop();

            // Assert
            result.Should().HaveCount(targetTables.Count);
            foreach (var targetTable in targetTables)
            {
                result.Should().Contain(targetTable);
            }
            
            // With indexed lookup, selecting 200 tables from 5000 should be very fast
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(50, 
                "SelectTables with indexed lookup should complete quickly even with large datasets");
        }

        [TestMethod]
        public void FindMethods_MultipleEntityTypes_ShouldMaintainPerformance()
        {
            // Arrange - Create mixed dataset with tables, views, and stored procedures
            const int entitiesPerType = 1000;
            
            // Add tables
            for (int i = 0; i < entitiesPerType; i++)
            {
                var table = new SemanticModelTable($"schema_{i % 10}", $"table_{i}", $"Table {i}");
                _semanticModel.AddTable(table);
            }
            
            // Add views
            for (int i = 0; i < entitiesPerType; i++)
            {
                var view = new SemanticModelView($"schema_{i % 10}", $"view_{i}", $"View {i}");
                _semanticModel.AddView(view);
            }
            
            // Add stored procedures
            for (int i = 0; i < entitiesPerType; i++)
            {
                var sp = new SemanticModelStoredProcedure($"schema_{i % 10}", $"sp_{i}", $"SP {i}");
                _semanticModel.AddStoredProcedure(sp);
            }

            // Add target entities
            var targetTable = new SemanticModelTable("target", "test_table", "Target table");
            var targetView = new SemanticModelView("target", "test_view", "Target view");
            var targetSP = new SemanticModelStoredProcedure("target", "test_sp", "Target SP");
            
            _semanticModel.AddTable(targetTable);
            _semanticModel.AddView(targetView);
            _semanticModel.AddStoredProcedure(targetSP);

            // Act - Perform lookups across all entity types
            var stopwatch = Stopwatch.StartNew();
            
            var foundTable = _semanticModel.FindTable("target", "test_table");
            var foundView = _semanticModel.FindView("target", "test_view");
            var foundSP = _semanticModel.FindStoredProcedure("target", "test_sp");
            
            stopwatch.Stop();

            // Assert
            foundTable.Should().Be(targetTable);
            foundView.Should().Be(targetView);
            foundSP.Should().Be(targetSP);
            
            // All three lookups should complete very quickly
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(10, 
                "Multiple indexed lookups should complete in under 10ms");
        }

        [TestMethod]
        public void AddRemove_Operations_ShouldMaintainIndexConsistency()
        {
            // Arrange
            const int numberOfOperations = 1000;
            var tables = new List<SemanticModelTable>();
            
            // Create tables to add/remove
            for (int i = 0; i < numberOfOperations; i++)
            {
                tables.Add(new SemanticModelTable($"schema_{i % 10}", $"table_{i}", $"Table {i}"));
            }

            // Act - Measure add operations
            var addStopwatch = Stopwatch.StartNew();
            foreach (var table in tables)
            {
                _semanticModel.AddTable(table);
            }
            addStopwatch.Stop();

            // Verify all tables can be found quickly
            var lookupStopwatch = Stopwatch.StartNew();
            foreach (var table in tables)
            {
                var found = _semanticModel.FindTable(table.Schema, table.Name);
                found.Should().Be(table);
            }
            lookupStopwatch.Stop();

            // Measure remove operations
            var removeStopwatch = Stopwatch.StartNew();
            foreach (var table in tables.Take(numberOfOperations / 2))
            {
                _semanticModel.RemoveTable(table);
            }
            removeStopwatch.Stop();

            // Assert
            _semanticModel.Tables.Should().HaveCount(numberOfOperations / 2);
            
            // All operations should be fast with indexed collections
            addStopwatch.ElapsedMilliseconds.Should().BeLessThan(200, 
                "Adding 1000 tables should complete in under 200ms");
            lookupStopwatch.ElapsedMilliseconds.Should().BeLessThan(100, 
                "1000 lookups should complete in under 100ms");
            removeStopwatch.ElapsedMilliseconds.Should().BeLessThan(100, 
                "Removing 500 tables should complete in under 100ms");
        }
    }
}