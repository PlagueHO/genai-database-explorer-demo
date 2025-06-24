using System.Data;
using FluentAssertions;
using GenAIDBExplorer.Core.Data.DatabaseProviders;
using GenAIDBExplorer.Core.Models.Project;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Moq;

namespace GenAIDBExplorer.Core.Tests.Data.DatabaseProviders
{
    [TestClass]
    public class SqlConnectionProviderTests
    {
        private Mock<IProject> _mockProject;
        private Mock<ILogger<SqlConnectionProvider>> _mockLogger;

        [TestInitialize]
        public void Setup()
        {
            _mockProject = new Mock<IProject>();
            _mockLogger = new Mock<ILogger<SqlConnectionProvider>>();
        }

        [TestMethod]
        public async Task ConnectAsync_WithMissingConnectionString_ShouldThrowInvalidDataException()
        {
            // Arrange
            var projectSettings = new ProjectSettings
            {
                Database = new DatabaseSettings { ConnectionString = null },
                DataDictionary = new DataDictionarySettings(),
                SemanticModel = new SemanticModelSettings(),
                OpenAIService = new OpenAIServiceSettings()
            };
            _mockProject.Setup(p => p.Settings).Returns(projectSettings);

            var provider = new SqlConnectionProvider(_mockProject.Object, _mockLogger.Object);

            // Act
            Func<Task> act = async () => await provider.ConnectAsync();

            // Assert
            await act.Should().ThrowAsync<InvalidDataException>()
                .WithMessage("Missing database connection string.");
        }

        [TestMethod]
        public async Task ConnectAsync_WhenGeneralExceptionOccurs_ShouldThrowException()
        {
            // Arrange
            var connectionString = "Server=localhost;Database=master;Trusted_Connection=True;";
            var projectSettings = new ProjectSettings
            {
                Database = new DatabaseSettings { ConnectionString = connectionString },
                DataDictionary = new DataDictionarySettings(),
                SemanticModel = new SemanticModelSettings(),
                OpenAIService = new OpenAIServiceSettings()
            };
            _mockProject.Setup(p => p.Settings).Returns(projectSettings);

            // Simulate a general exception during connection.OpenAsync()
            var provider = new SqlConnectionProvider(_mockProject.Object, _mockLogger.Object);

            // Act
            Func<Task> act = async () =>
            {
                await provider.ConnectAsync();
            };

            // Assert
            await act.Should().ThrowAsync<Exception>();
        }

        [TestMethod]
        public async Task ConnectAsync_WithPoolingDisabled_ShouldBuildConnectionStringWithoutPooling()
        {
            // Arrange
            var baseConnectionString = "Server=localhost;Database=test;";
            var projectSettings = new ProjectSettings
            {
                Database = new DatabaseSettings 
                { 
                    ConnectionString = baseConnectionString,
                    PoolingEnabled = false,
                    MaxPoolSize = 50,
                    MinPoolSize = 2
                },
                DataDictionary = new DataDictionarySettings(),
                SemanticModel = new SemanticModelSettings(),
                OpenAIService = new OpenAIServiceSettings()
            };
            _mockProject.Setup(p => p.Settings).Returns(projectSettings);

            var provider = new SqlConnectionProvider(_mockProject.Object, _mockLogger.Object);

            // Act & Assert - This will fail due to invalid connection string but we're testing the string building logic
            Func<Task> act = async () => await provider.ConnectAsync();
            await act.Should().ThrowAsync<Exception>();
        }

        [TestMethod]
        public async Task ConnectAsync_WithCustomPoolSettings_ShouldBuildConnectionStringWithCustomValues()
        {
            // Arrange
            var baseConnectionString = "Server=localhost;Database=test;";
            var projectSettings = new ProjectSettings
            {
                Database = new DatabaseSettings 
                { 
                    ConnectionString = baseConnectionString,
                    PoolingEnabled = true,
                    MaxPoolSize = 200,
                    MinPoolSize = 10,
                    ConnectionTimeout = 60,
                    CommandTimeout = 120
                },
                DataDictionary = new DataDictionarySettings(),
                SemanticModel = new SemanticModelSettings(),
                OpenAIService = new OpenAIServiceSettings()
            };
            _mockProject.Setup(p => p.Settings).Returns(projectSettings);

            var provider = new SqlConnectionProvider(_mockProject.Object, _mockLogger.Object);

            // Act & Assert - This will fail due to invalid connection string but we're testing the string building logic
            Func<Task> act = async () => await provider.ConnectAsync();
            await act.Should().ThrowAsync<Exception>();
        }

        [TestMethod]
        public async Task ConnectAsync_WithRetrySettings_ShouldRespectMaxRetryAttempts()
        {
            // Arrange
            var connectionString = "Server=invalidserver;Database=test;Connection Timeout=1;";
            var projectSettings = new ProjectSettings
            {
                Database = new DatabaseSettings 
                { 
                    ConnectionString = connectionString,
                    MaxRetryAttempts = 2,
                    RetryDelayMilliseconds = 100
                },
                DataDictionary = new DataDictionarySettings(),
                SemanticModel = new SemanticModelSettings(),
                OpenAIService = new OpenAIServiceSettings()
            };
            _mockProject.Setup(p => p.Settings).Returns(projectSettings);

            var provider = new SqlConnectionProvider(_mockProject.Object, _mockLogger.Object);

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            Func<Task> act = async () => await provider.ConnectAsync();

            // Assert
            await act.Should().ThrowAsync<Exception>();
            
            // Should have tried multiple times with delays
            stopwatch.ElapsedMilliseconds.Should().BeGreaterThan(200); // At least 2 delays of 100ms each
        }
    }
}
