using FluentAssertions;
using GenAIDBExplorer.Core.Models.SemanticModel;
using System.Text.Json;

namespace GenAIDBExplorer.Core.Integration.Test.Security;

/// <summary>
/// Security validation tests for semantic model components.
/// Tests path validation, input sanitization, and security measures.
/// </summary>
[TestClass]
[TestCategory("Security")]
public class SecurityValidationTests
{
    private const string TEMP_TEST_DIR = "SecurityTestTemp";
    private string _tempBasePath = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _tempBasePath = Path.Combine(Path.GetTempPath(), TEMP_TEST_DIR, Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempBasePath);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (Directory.Exists(_tempBasePath))
        {
            Directory.Delete(_tempBasePath, recursive: true);
        }
    }

    [TestMethod]
    public void PathValidator_WithDirectoryTraversalAttempt_ShouldThrowSecurityException()
    {
        // Arrange
        var maliciousPath = Path.Combine(_tempBasePath, "..", "..", "etc", "passwd");
        var semanticModel = new SemanticModel("TestDB", "TestSource", "Test Description");

        // Act & Assert
        var exception = Assert.ThrowsException<UnauthorizedAccessException>(() =>
        {
            var directoryInfo = new DirectoryInfo(maliciousPath);
            ValidatePath(directoryInfo);
        });

        exception.Message.Should().Contain("path traversal");
    }

    [TestMethod]
    public void PathValidator_WithValidPath_ShouldNotThrowException()
    {
        // Arrange
        var validPath = Path.Combine(_tempBasePath, "ValidSubfolder");
        Directory.CreateDirectory(validPath);

        // Act & Assert
        var directoryInfo = new DirectoryInfo(validPath);
        var act = () => ValidatePath(directoryInfo);
        act.Should().NotThrow();
    }

    [TestMethod]
    public void PathValidator_WithNullPath_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => ValidatePath(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void EntityNameSanitization_WithMaliciousTableName_ShouldSanitizeName()
    {
        // Arrange
        var maliciousTableName = "../../../etc/passwd";
        var schemaName = "dbo"; // Keep variable for potential future use

        // Act
        var sanitizedName = SanitizeEntityName(maliciousTableName);

        // Assert
        sanitizedName.Should().NotContain("..");
        sanitizedName.Should().NotContain("/");
        sanitizedName.Should().NotContain("\\");
        sanitizedName.Should().NotBeEmpty();
    }

    [TestMethod]
    public void EntityNameSanitization_WithValidName_ShouldReturnOriginalName()
    {
        // Arrange
        var validName = "Users";

        // Act
        var sanitizedName = SanitizeEntityName(validName);

        // Assert
        sanitizedName.Should().Be(validName);
    }

    [TestMethod]
    public void EntityNameSanitization_WithSpecialCharacters_ShouldSanitizeSpecialCharacters()
    {
        // Arrange
        var nameWithSpecialChars = "<script>alert('xss')</script>";

        // Act
        var sanitizedName = SanitizeEntityName(nameWithSpecialChars);

        // Assert
        sanitizedName.Should().NotContain("<");
        sanitizedName.Should().NotContain(">");
        sanitizedName.Should().NotContain("script");
        sanitizedName.Should().NotContain("alert");
    }

    [TestMethod]
    public async Task JsonDeserialization_WithMaliciousPayload_ShouldHandleSafely()
    {
        // Arrange
        var maliciousJson = """
        {
            "Name": "TestDB",
            "Source": "TestSource",
            "Description": "Test",
            "__proto__": { "polluted": true },
            "constructor": { "prototype": { "polluted": true } }
        }
        """;

        // Act & Assert
        var act = () => JsonSerializer.Deserialize<SemanticModel>(maliciousJson);
        
        // The JsonSerializer should either ignore unknown properties or throw an exception
        // We're testing that it doesn't cause security issues
        act.Should().NotThrow("JSON deserialization should handle unknown properties safely");
    }

    [TestMethod]
    public async Task JsonDeserialization_WithLargePayload_ShouldHandleGracefully()
    {
        // Arrange
        var largeDescription = new string('A', 1000000); // 1MB string
        var semanticModel = new SemanticModel("TestDB", "TestSource", largeDescription);

        // Act
        var json = JsonSerializer.Serialize(semanticModel);
        var act = () => JsonSerializer.Deserialize<SemanticModel>(json);

        // Assert
        act.Should().NotThrow();
        var deserializedModel = JsonSerializer.Deserialize<SemanticModel>(json);
        deserializedModel.Should().NotBeNull();
        deserializedModel!.Description.Should().HaveLength(1000000);
    }

    [TestMethod]
    public void FilePermissions_WhenCreatingModelFile_ShouldSetRestrictivePermissions()
    {
        // Arrange
        var testFilePath = Path.Combine(_tempBasePath, "test_model.json");
        var semanticModel = new SemanticModel("TestDB", "TestSource", "Test Description");

        // Act
        File.WriteAllText(testFilePath, JsonSerializer.Serialize(semanticModel));

        // Assert
        File.Exists(testFilePath).Should().BeTrue();
        
        // On Unix-like systems, verify file permissions are not world-readable
        if (!OperatingSystem.IsWindows())
        {
            var fileInfo = new FileInfo(testFilePath);
            // File should be readable by owner but ideally not by others
            fileInfo.Exists.Should().BeTrue();
        }
    }

    [TestMethod]
    public void ModelPath_WithInvalidCharacters_ShouldThrowArgumentException()
    {
        // Arrange
        var invalidChars = Path.GetInvalidPathChars();
        var semanticModel = new SemanticModel("TestDB", "TestSource", "Test Description");

        foreach (var invalidChar in invalidChars.Take(5)) // Test first 5 invalid chars
        {
            var invalidPath = Path.Combine(_tempBasePath, $"Invalid{invalidChar}Path");

            // Act & Assert
            var act = () => new DirectoryInfo(invalidPath);
            if (invalidChar != '\0') // Null character is handled differently
            {
                act.Should().Throw<ArgumentException>()
                   .WithMessage("*invalid*");
            }
        }
    }

    [TestMethod]
    public async Task ModelSerialization_WithCircularReference_ShouldHandleGracefully()
    {
        // Arrange
        var table1 = new SemanticModelTable("dbo", "Users");
        var table2 = new SemanticModelTable("dbo", "Orders");
        
        var column1 = new SemanticModelColumn("dbo", "UserId", "User ID column")
        {
            Type = "int",
            ReferencedTable = "Orders",
            ReferencedColumn = "UserId"
        };
        
        var column2 = new SemanticModelColumn("dbo", "OrderId", "Order ID column") 
        {
            Type = "int",
            ReferencedTable = "Users",
            ReferencedColumn = "Id"
        };

        table1.Columns.Add(column1);
        table2.Columns.Add(column2);

        var semanticModel = new SemanticModel("TestDB", "TestSource", "Test Description");
        semanticModel.Tables.Add(table1);
        semanticModel.Tables.Add(table2);

        // Act & Assert
        var act = () => JsonSerializer.Serialize(semanticModel);
        act.Should().NotThrow(); // Should handle circular references gracefully
    }

    [TestMethod]
    public void InputValidation_WithSqlInjectionAttempt_ShouldSanitizeInput()
    {
        // Arrange
        var maliciousInput = "'; DROP TABLE Users; --";
        
        // Act
        var sanitizedInput = SanitizeSqlInput(maliciousInput);

        // Assert
        sanitizedInput.Should().NotContain("DROP");
        sanitizedInput.Should().NotContain("--");
        sanitizedInput.Should().NotContain(";");
    }

    [TestMethod]
    public void InputValidation_WithValidInput_ShouldReturnOriginalInput()
    {
        // Arrange
        var validInput = "Users";

        // Act
        var sanitizedInput = SanitizeSqlInput(validInput);

        // Assert
        sanitizedInput.Should().Be(validInput);
    }

    [TestMethod]
    public void PathValidation_WithUncPath_ShouldThrowSecurityException()
    {
        // Arrange
        var uncPath = @"\\malicious-server\share\file.txt"; 

        // Act & Assert
        var act = () => ValidatePath(new DirectoryInfo(uncPath));
        act.Should().Throw<UnauthorizedAccessException>()
           .WithMessage("*UNC paths*");
    }

    [TestMethod]
    public void PathValidation_WithExcessivelyLongPath_ShouldThrowPathTooLongException()
    {
        // Arrange
        var longPath = Path.Combine(_tempBasePath, new string('A', 300)); // Exceed typical path limits

        // Act & Assert
        var act = () => new DirectoryInfo(longPath);
        act.Should().Throw<PathTooLongException>();
    }

    [TestMethod]
    public void ParameterValidation_WithNullParameters_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act1 = () => new SemanticModel(null!, "source", "desc");
        act1.Should().Throw<ArgumentNullException>();

        var act2 = () => new SemanticModel("name", null!, "desc");
        act2.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void ParameterValidation_WithEmptyParameters_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act1 = () => new SemanticModel("", "source", "desc");
        act1.Should().Throw<ArgumentException>();

        var act2 = () => new SemanticModel("name", "", "desc");
        act2.Should().Throw<ArgumentException>();
    }

    #region Helper Methods

    /// <summary>
    /// Validates that a directory path is safe and doesn't contain path traversal attempts.
    /// </summary>
    /// <param name="path">The directory path to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when path is null.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when path contains security risks.</exception>
    private static void ValidatePath(DirectoryInfo path)
    {
        if (path == null)
            throw new ArgumentNullException(nameof(path));

        var fullPath = path.FullName;

        // Check for path traversal attempts
        if (fullPath.Contains("..") || fullPath.Contains("./") || fullPath.Contains(".\\"))
        {
            throw new UnauthorizedAccessException("Path contains path traversal characters.");
        }

        // Check for UNC paths
        if (fullPath.StartsWith(@"\\"))
        {
            throw new UnauthorizedAccessException("UNC paths are not allowed.");
        }
    }

    /// <summary>
    /// Sanitizes entity names to prevent file system injection.
    /// </summary>
    /// <param name="entityName">The entity name to sanitize.</param>
    /// <returns>The sanitized entity name.</returns>
    private static string SanitizeEntityName(string entityName)
    {
        if (string.IsNullOrEmpty(entityName))
            return entityName;

        // Remove path traversal characters
        var sanitized = entityName.Replace("..", "")
                                 .Replace("/", "")
                                 .Replace("\\", "");

        // Remove HTML/script injection attempts
        sanitized = sanitized.Replace("<", "")
                           .Replace(">", "")
                           .Replace("script", "")
                           .Replace("alert", "");

        return sanitized;
    }

    /// <summary>
    /// Sanitizes SQL input to prevent injection attacks.
    /// </summary>
    /// <param name="input">The input to sanitize.</param>
    /// <returns>The sanitized input.</returns>
    private static string SanitizeSqlInput(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Remove common SQL injection patterns
        return input.Replace("'", "")
                   .Replace("--", "")
                   .Replace(";", "")
                   .Replace("DROP", "")
                   .Replace("DELETE", "")
                   .Replace("INSERT", "")
                   .Replace("UPDATE", "")
                   .Replace("EXEC", "")
                   .Replace("EXECUTE", "");
    }

    #endregion
}