using FluentAssertions;
using GenAIDBExplorer.Core.Models.Project;
using System.ComponentModel.DataAnnotations;

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

        [TestMethod]
        public void Validate_WithValidSettings_ShouldReturnNoErrors()
        {
            // Arrange
            var settings = new DatabaseSettings
            {
                Name = "TestDB",
                ConnectionString = "Server=localhost;Database=test;",
                MaxPoolSize = 100,
                MinPoolSize = 5,
                ConnectionTimeout = 30,
                CommandTimeout = 30,
                MaxRetryAttempts = 3,
                RetryDelayMilliseconds = 1000,
                MaxDegreeOfParallelism = 1
            };

            // Act
            var results = settings.Validate(new ValidationContext(settings));

            // Assert
            results.Should().BeEmpty();
        }

        [TestMethod]
        public void Validate_WithInvalidPoolSizes_ShouldReturnErrors()
        {
            // Arrange
            var settings = new DatabaseSettings
            {
                Name = "TestDB",
                ConnectionString = "Server=localhost;Database=test;",
                MaxPoolSize = 5,
                MinPoolSize = 10, // Invalid: min > max
                ConnectionTimeout = 30,
                CommandTimeout = 30
            };

            // Act
            var results = settings.Validate(new ValidationContext(settings)).ToList();

            // Assert
            results.Should().HaveCount(1);
            results[0].ErrorMessage.Should().Contain("MinPoolSize cannot be greater than MaxPoolSize");
        }

        [TestMethod]
        public void Validate_WithNegativeValues_ShouldReturnErrors()
        {
            // Arrange
            var settings = new DatabaseSettings
            {
                Name = "TestDB",
                ConnectionString = "Server=localhost;Database=test;",
                MaxPoolSize = -1,
                MinPoolSize = -1,
                ConnectionTimeout = -1,
                CommandTimeout = -1,
                MaxRetryAttempts = -1,
                RetryDelayMilliseconds = -1,
                MaxDegreeOfParallelism = -1
            };

            // Act
            var results = settings.Validate(new ValidationContext(settings)).ToList();

            // Assert
            results.Should().HaveCountGreaterThan(5);
            results.Should().Contain(r => r.ErrorMessage.Contains("must be non-negative") || r.ErrorMessage.Contains("must be greater than zero"));
        }

        [TestMethod]
        public void Validate_WithExcessiveValues_ShouldReturnWarnings()
        {
            // Arrange
            var settings = new DatabaseSettings
            {
                Name = "TestDB",
                ConnectionString = "Server=localhost;Database=test;",
                MaxPoolSize = 2000, // Very high
                ConnectionTimeout = 400, // Very high
                CommandTimeout = 30,
                MinPoolSize = 5
            };

            // Act
            var results = settings.Validate(new ValidationContext(settings)).ToList();

            // Assert
            results.Should().HaveCount(2);
            results.Should().Contain(r => r.ErrorMessage.Contains("MaxPoolSize is very high"));
            results.Should().Contain(r => r.ErrorMessage.Contains("ConnectionTimeout is very high"));
        }

        [TestMethod]
        public void Validate_WithParallelismButNoMARS_ShouldReturnWarning()
        {
            // Arrange
            var settings = new DatabaseSettings
            {
                Name = "TestDB",
                ConnectionString = "Server=localhost;Database=test;Integrated Security=true;",
                MaxDegreeOfParallelism = 4, // > 1
                PoolingEnabled = true,
                MaxPoolSize = 100,
                MinPoolSize = 5
            };

            // Act
            var results = settings.Validate(new ValidationContext(settings)).ToList();

            // Assert
            results.Should().HaveCount(1);
            results[0].ErrorMessage.Should().Contain("MultipleActiveResultSets=True");
        }
    }
}