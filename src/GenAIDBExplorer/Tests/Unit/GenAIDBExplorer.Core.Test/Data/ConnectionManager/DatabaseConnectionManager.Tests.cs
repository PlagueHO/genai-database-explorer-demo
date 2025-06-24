using System.Data;
using FluentAssertions;
using GenAIDBExplorer.Core.Data.ConnectionManager;
using GenAIDBExplorer.Core.Data.DatabaseProviders;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Moq;

namespace GenAIDBExplorer.Core.Tests.Data.ConnectionManager
{
    [TestClass]
    public class DatabaseConnectionManagerTests
    {
        private Mock<IDatabaseConnectionProvider> _mockConnectionProvider;
        private Mock<ILogger<DatabaseConnectionManager>> _mockLogger;

        [TestInitialize]
        public void Setup()
        {
            _mockConnectionProvider = new Mock<IDatabaseConnectionProvider>();
            _mockLogger = new Mock<ILogger<DatabaseConnectionManager>>();
        }

        [TestMethod]
        public async Task GetOpenConnectionAsync_ShouldReturnOpenConnection()
        {
            // Arrange
            var mockConnection = new Mock<SqlConnection>();
            mockConnection.Setup(c => c.State).Returns(ConnectionState.Open);
            
            _mockConnectionProvider.Setup(p => p.ConnectAsync())
                .ReturnsAsync(mockConnection.Object);

            var manager = new DatabaseConnectionManager(_mockConnectionProvider.Object, _mockLogger.Object);

            // Act
            var result = await manager.GetOpenConnectionAsync();

            // Assert
            result.Should().NotBeNull();
            result.State.Should().Be(ConnectionState.Open);
            _mockConnectionProvider.Verify(p => p.ConnectAsync(), Times.Once);
        }

        [TestMethod]
        public async Task GetOpenConnectionAsync_WhenConnectionNotOpen_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var mockConnection = new Mock<SqlConnection>();
            mockConnection.Setup(c => c.State).Returns(ConnectionState.Closed);
            
            _mockConnectionProvider.Setup(p => p.ConnectAsync())
                .ReturnsAsync(mockConnection.Object);

            var manager = new DatabaseConnectionManager(_mockConnectionProvider.Object, _mockLogger.Object);

            // Act
            Func<Task> act = async () => await manager.GetOpenConnectionAsync();

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>();
            mockConnection.Verify(c => c.Dispose(), Times.Once);
        }

        [TestMethod]
        public async Task GetOpenConnectionAsync_WhenProviderThrows_ShouldPropagateException()
        {
            // Arrange
            _mockConnectionProvider.Setup(p => p.ConnectAsync())
                .ThrowsAsync(new SqlException());

            var manager = new DatabaseConnectionManager(_mockConnectionProvider.Object, _mockLogger.Object);

            // Act
            Func<Task> act = async () => await manager.GetOpenConnectionAsync();

            // Assert
            await act.Should().ThrowAsync<SqlException>();
        }

        [TestMethod]
        public async Task GetOpenConnectionAsync_MultipleCalls_ShouldGetNewConnectionEachTime()
        {
            // Arrange
            var mockConnection1 = new Mock<SqlConnection>();
            var mockConnection2 = new Mock<SqlConnection>();
            mockConnection1.Setup(c => c.State).Returns(ConnectionState.Open);
            mockConnection2.Setup(c => c.State).Returns(ConnectionState.Open);
            
            _mockConnectionProvider.SetupSequence(p => p.ConnectAsync())
                .ReturnsAsync(mockConnection1.Object)
                .ReturnsAsync(mockConnection2.Object);

            var manager = new DatabaseConnectionManager(_mockConnectionProvider.Object, _mockLogger.Object);

            // Act
            var result1 = await manager.GetOpenConnectionAsync();
            var result2 = await manager.GetOpenConnectionAsync();

            // Assert
            result1.Should().NotBeNull();
            result2.Should().NotBeNull();
            _mockConnectionProvider.Verify(p => p.ConnectAsync(), Times.Exactly(2));
        }

        [TestMethod]
        public async Task GetOpenConnectionAsync_AfterDisposed_ShouldThrowObjectDisposedException()
        {
            // Arrange
            var manager = new DatabaseConnectionManager(_mockConnectionProvider.Object, _mockLogger.Object);
            manager.Dispose();

            // Act
            Func<Task> act = async () => await manager.GetOpenConnectionAsync();

            // Assert
            await act.Should().ThrowAsync<ObjectDisposedException>();
        }

        [TestMethod]
        public void Dispose_ShouldLogFinalMetrics()
        {
            // Arrange
            var manager = new DatabaseConnectionManager(_mockConnectionProvider.Object, _mockLogger.Object);

            // Act
            manager.Dispose();

            // Assert
            // Verify that logging occurred - this tests that metrics are tracked
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("DatabaseConnectionManager disposed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [TestMethod]
        public void Dispose_CalledMultipleTimes_ShouldNotThrow()
        {
            // Arrange
            var manager = new DatabaseConnectionManager(_mockConnectionProvider.Object, _mockLogger.Object);

            // Act & Assert
            Action act = () =>
            {
                manager.Dispose();
                manager.Dispose();
                manager.Dispose();
            };

            act.Should().NotThrow();
        }
    }
}