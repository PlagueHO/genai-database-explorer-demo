using FluentAssertions;
using GenAIDBExplorer.Core.Models.Project;

namespace GenAIDBExplorer.Core.Tests.Models.Project
{
    [TestClass]
    public class DatabaseSettingsTests
    {
        [TestMethod]
        public void DatabaseSettings_DefaultValues_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var settings = new DatabaseSettings();

            // Assert
            settings.MaxPoolSize.Should().Be(100);
            settings.MinPoolSize.Should().Be(5);
            settings.ConnectionTimeout.Should().Be(30);
            settings.CommandTimeout.Should().Be(30);
            settings.PoolingEnabled.Should().BeTrue();
            settings.MaxRetryAttempts.Should().Be(3);
            settings.RetryDelayMilliseconds.Should().Be(1000);
            settings.EnableHealthMonitoring.Should().BeTrue();
            settings.MaxDegreeOfParallelism.Should().Be(1);
            settings.NotUsedTables.Should().NotBeNull().And.BeEmpty();
            settings.NotUsedColumns.Should().NotBeNull().And.BeEmpty();
            settings.NotUsedViews.Should().NotBeNull().And.BeEmpty();
            settings.NotUsedStoredProcedures.Should().NotBeNull().And.BeEmpty();
        }

        [TestMethod]
        public void DatabaseSettings_PoolingConfiguration_ShouldAcceptValidValues()
        {
            // Arrange
            var settings = new DatabaseSettings();

            // Act
            settings.MaxPoolSize = 200;
            settings.MinPoolSize = 10;
            settings.ConnectionTimeout = 60;
            settings.CommandTimeout = 120;
            settings.PoolingEnabled = false;
            settings.MaxRetryAttempts = 5;
            settings.RetryDelayMilliseconds = 2000;
            settings.EnableHealthMonitoring = false;

            // Assert
            settings.MaxPoolSize.Should().Be(200);
            settings.MinPoolSize.Should().Be(10);
            settings.ConnectionTimeout.Should().Be(60);
            settings.CommandTimeout.Should().Be(120);
            settings.PoolingEnabled.Should().BeFalse();
            settings.MaxRetryAttempts.Should().Be(5);
            settings.RetryDelayMilliseconds.Should().Be(2000);
            settings.EnableHealthMonitoring.Should().BeFalse();
        }

        [TestMethod]
        public void DatabaseSettings_RequiredProperties_ShouldBeConfigurable()
        {
            // Arrange
            var settings = new DatabaseSettings();

            // Act
            settings.Name = "TestDatabase";
            settings.Description = "Test database description";
            settings.ConnectionString = "Server=localhost;Database=test;";
            settings.Schema = "dbo";

            // Assert
            settings.Name.Should().Be("TestDatabase");
            settings.Description.Should().Be("Test database description");
            settings.ConnectionString.Should().Be("Server=localhost;Database=test;");
            settings.Schema.Should().Be("dbo");
        }
    }
}