using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using System.Data.Common;
using GenAIDBExplorer.Core.SemanticModelProviders;
using GenAIDBExplorer.Core.Data.DatabaseProviders;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Models.Database;
using GenAIDBExplorer.Core.Models.SemanticModel;

namespace GenAIDBExplorer.Core.Tests.SemanticModelProviders
{
    [TestClass]
    public class SchemaRepositoryBatchOperationsTests
    {
        private Mock<ISqlQueryExecutor> _mockSqlQueryExecutor;
        private Mock<IProject> _mockProject;
        private Mock<ILogger<SchemaRepository>> _mockLogger;
        private Mock<IDatabaseSettings> _mockDatabaseSettings;
        private Mock<IProjectSettings> _mockProjectSettings;
        private SchemaRepository _schemaRepository;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockSqlQueryExecutor = new Mock<ISqlQueryExecutor>();
            _mockProject = new Mock<IProject>();
            _mockLogger = new Mock<ILogger<SchemaRepository>>();
            _mockDatabaseSettings = new Mock<IDatabaseSettings>();
            _mockProjectSettings = new Mock<IProjectSettings>();

            _mockProject.Setup(p => p.Settings).Returns(_mockProjectSettings.Object);
            _mockProjectSettings.Setup(s => s.Database).Returns(_mockDatabaseSettings.Object);
            _mockDatabaseSettings.Setup(d => d.NotUsedTables).Returns(new List<string>());
            _mockDatabaseSettings.Setup(d => d.NotUsedColumns).Returns(new List<string>());

            _schemaRepository = new SchemaRepository(_mockSqlQueryExecutor.Object, _mockProject.Object, _mockLogger.Object);
        }

        [TestMethod]
        public async Task GetColumnsForTablesAsync_WithEmptyCollection_ShouldReturnEmptyDictionary()
        {
            // Arrange
            var tables = new List<TableInfo>();

            // Act
            var result = await _schemaRepository.GetColumnsForTablesAsync(tables);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
            _mockSqlQueryExecutor.Verify(x => x.ExecuteReaderAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()), Times.Never);
        }

        [TestMethod]
        public async Task GetColumnsForTablesAsync_WithSingleTable_ShouldReturnColumnsForTable()
        {
            // Arrange
            var tables = new List<TableInfo>
            {
                new TableInfo("dbo", "TestTable")
            };

            var mockReader = CreateMockDataReader(new[]
            {
                new { SchemaName = "dbo", TableName = "TestTable", ColumnName = "Id", ColumnDesc = (string?)null, 
                      ColumnType = "int", IsPK = true, MaxLength = (short)4, Precision = (byte)10, Scale = (byte)0, 
                      IsNullable = false, IsIdentity = true, IsComputed = false, IsXmlDocument = false }
            });

            _mockSqlQueryExecutor.Setup(x => x.ExecuteReaderAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(mockReader);

            // Act
            var result = await _schemaRepository.GetColumnsForTablesAsync(tables);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.Should().ContainKey(tables[0]);
            
            var columns = result[tables[0]];
            columns.Should().HaveCount(1);
            columns[0].Name.Should().Be("Id");
            columns[0].Type.Should().Be("int");
            columns[0].IsPrimaryKey.Should().BeTrue();
            columns[0].IsIdentity.Should().BeTrue();
        }

        [TestMethod]
        public async Task GetColumnsForTablesAsync_WithMultipleTables_ShouldReturnColumnsForAllTables()
        {
            // Arrange
            var tables = new List<TableInfo>
            {
                new TableInfo("dbo", "Table1"),
                new TableInfo("dbo", "Table2")
            };

            var mockReader = CreateMockDataReader(new[]
            {
                new { SchemaName = "dbo", TableName = "Table1", ColumnName = "Id", ColumnDesc = (string?)null, 
                      ColumnType = "int", IsPK = true, MaxLength = (short)4, Precision = (byte)10, Scale = (byte)0, 
                      IsNullable = false, IsIdentity = true, IsComputed = false, IsXmlDocument = false },
                new { SchemaName = "dbo", TableName = "Table1", ColumnName = "Name", ColumnDesc = (string?)null, 
                      ColumnType = "varchar", IsPK = false, MaxLength = (short)50, Precision = (byte)0, Scale = (byte)0, 
                      IsNullable = true, IsIdentity = false, IsComputed = false, IsXmlDocument = false },
                new { SchemaName = "dbo", TableName = "Table2", ColumnName = "Id", ColumnDesc = (string?)null, 
                      ColumnType = "int", IsPK = true, MaxLength = (short)4, Precision = (byte)10, Scale = (byte)0, 
                      IsNullable = false, IsIdentity = true, IsComputed = false, IsXmlDocument = false }
            });

            _mockSqlQueryExecutor.Setup(x => x.ExecuteReaderAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(mockReader);

            // Act
            var result = await _schemaRepository.GetColumnsForTablesAsync(tables);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().ContainKey(tables[0]);
            result.Should().ContainKey(tables[1]);
            
            result[tables[0]].Should().HaveCount(2);
            result[tables[1]].Should().HaveCount(1);
        }

        [TestMethod]
        public async Task CreateSemanticModelTablesAsync_WithEmptyCollection_ShouldReturnEmptyList()
        {
            // Arrange
            var tables = new List<TableInfo>();

            // Act
            var result = await _schemaRepository.CreateSemanticModelTablesAsync(tables);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [TestMethod]
        public async Task CreateSemanticModelTablesAsync_WithTables_ShouldReturnSemanticModelTables()
        {  
            // Arrange
            var tables = new List<TableInfo>
            {
                new TableInfo("dbo", "TestTable")
            };

            // Mock columns data
            var mockColumnsReader = CreateMockDataReader(new[]
            {
                new { SchemaName = "dbo", TableName = "TestTable", ColumnName = "Id", ColumnDesc = (string?)null, 
                      ColumnType = "int", IsPK = true, MaxLength = (short)4, Precision = (byte)10, Scale = (byte)0, 
                      IsNullable = false, IsIdentity = true, IsComputed = false, IsXmlDocument = false }
            });

            // Mock references data (empty)
            var mockReferencesReader = CreateMockDataReader(Array.Empty<object>());

            // Mock indexes data (empty)
            var mockIndexesReader = CreateMockDataReader(Array.Empty<object>());

            _mockSqlQueryExecutor.SetupSequence(x => x.ExecuteReaderAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(mockColumnsReader)
                .ReturnsAsync(mockReferencesReader)
                .ReturnsAsync(mockIndexesReader);

            // Act
            var result = await _schemaRepository.CreateSemanticModelTablesAsync(tables);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].Schema.Should().Be("dbo");
            result[0].Name.Should().Be("TestTable");
            result[0].Columns.Should().HaveCount(1);
            result[0].Columns[0].Name.Should().Be("Id");
        }

        [TestMethod]
        public async Task GetSampleDataForTablesAsync_WithEmptyCollection_ShouldReturnEmptyDictionary()
        {
            // Arrange
            var tables = new List<TableInfo>();

            // Act
            var result = await _schemaRepository.GetSampleDataForTablesAsync(tables);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        private SqlDataReader CreateMockDataReader(IEnumerable<object> data)
        {
            var mockReader = new Mock<SqlDataReader>();
            var dataArray = data.ToArray();
            var currentIndex = -1;

            mockReader.Setup(r => r.ReadAsync()).Returns(() =>
            {
                currentIndex++;
                return Task.FromResult(currentIndex < dataArray.Length);
            });

            // Setup field access methods based on the test data structure
            mockReader.Setup(r => r.GetString(0)).Returns(() => GetProperty<string>(dataArray[currentIndex], "SchemaName"));
            mockReader.Setup(r => r.GetString(1)).Returns(() => GetProperty<string>(dataArray[currentIndex], "TableName"));
            mockReader.Setup(r => r.GetString(2)).Returns(() => GetProperty<string>(dataArray[currentIndex], "ColumnName"));
            mockReader.Setup(r => r.IsDBNull(3)).Returns(() => GetProperty<string?>(dataArray[currentIndex], "ColumnDesc") == null);
            mockReader.Setup(r => r.GetString(3)).Returns(() => GetProperty<string?>(dataArray[currentIndex], "ColumnDesc") ?? "");
            mockReader.Setup(r => r.GetString(4)).Returns(() => GetProperty<string>(dataArray[currentIndex], "ColumnType"));
            mockReader.Setup(r => r.GetBoolean(5)).Returns(() => GetProperty<bool>(dataArray[currentIndex], "IsPK"));
            mockReader.Setup(r => r.GetInt16(6)).Returns(() => GetProperty<short>(dataArray[currentIndex], "MaxLength"));
            mockReader.Setup(r => r.GetByte(7)).Returns(() => GetProperty<byte>(dataArray[currentIndex], "Precision"));
            mockReader.Setup(r => r.GetByte(8)).Returns(() => GetProperty<byte>(dataArray[currentIndex], "Scale"));
            mockReader.Setup(r => r.GetBoolean(9)).Returns(() => GetProperty<bool>(dataArray[currentIndex], "IsNullable"));
            mockReader.Setup(r => r.GetBoolean(10)).Returns(() => GetProperty<bool>(dataArray[currentIndex], "IsIdentity"));
            mockReader.Setup(r => r.GetBoolean(11)).Returns(() => GetProperty<bool>(dataArray[currentIndex], "IsComputed"));
            mockReader.Setup(r => r.GetBoolean(12)).Returns(() => GetProperty<bool>(dataArray[currentIndex], "IsXmlDocument"));

            return mockReader.Object;
        }

        private T GetProperty<T>(object obj, string propertyName)
        {
            var property = obj.GetType().GetProperty(propertyName);
            return (T)property!.GetValue(obj)!;
        }
    }
}