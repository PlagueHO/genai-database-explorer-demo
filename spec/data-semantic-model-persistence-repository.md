---
title: Semantic Model Persistence Repository Pattern Specification
version: 1.0
date_created: 2025-06-22
last_updated: 2025-06-22
owner: GenAI Database Explorer Team
tags: [data, repository, persistence, semantic-model, generative-ai]
---

This specification defines the requirements, constraints, and interfaces for implementing the repository pattern to persist semantic models extracted from database schemas for the GenAI Database Explorer application.

## 1. Purpose & Scope

This specification defines the requirements for implementing a repository pattern to persist and manage semantic models extracted from database schemas. The semantic model represents a rich, AI-consumable representation of database structures including tables, views, stored procedures, and their relationships. The repository pattern provides abstraction for data access operations, enabling multiple persistence strategies and supporting both file-based and future database-based storage mechanisms.

**Intended Audience:** Software developers, architects, and AI engineers working on the GenAI Database Explorer application.

**Assumptions:**

- The application uses .NET 9 with modern C# language features
- Dependency injection is used for managing component dependencies
- Asynchronous programming patterns are preferred for I/O operations
- JSON serialization is the primary persistence format for file-based storage

## 2. Definitions

- **AI**: Artificial Intelligence
- **DI**: Dependency Injection
- **DTO**: Data Transfer Object
- **GenAI**: Generative Artificial Intelligence
- **I/O**: Input/Output operations
- **JSON**: JavaScript Object Notation
- **ORM**: Object-Relational Mapping
- **POCO**: Plain Old CLR Object
- **Repository Pattern**: A design pattern that encapsulates data access logic and provides a uniform interface for accessing domain objects
- **Semantic Model**: A rich representation of database schema that includes metadata, relationships, and AI-friendly descriptions
- **Schema Repository**: A repository specifically responsible for extracting and transforming raw database schema information
- **Semantic Model Provider**: A service that orchestrates the creation, extraction, and management of semantic models
- **Lazy Loading**: A design pattern that defers the loading of data until it is actually needed, reducing initial memory usage
- **Dirty Tracking**: A mechanism that monitors changes to objects and identifies which entities need to be persisted

## 3. Requirements, Constraints & Guidelines

### Core Requirements

- **REQ-001**: The repository pattern MUST provide abstraction for semantic model persistence operations
- **REQ-002**: The implementation MUST support asynchronous operations for all I/O-bound activities
- **REQ-003**: The repository MUST handle file-based persistence strategies
- **REQ-004**: The semantic model MUST be persisted in a hierarchical structure with separate files for entities
- **REQ-005**: The repository MUST support CRUD operations (Create, Read, Update, Delete) for semantic models
- **REQ-006**: The implementation MUST use dependency injection for component management
- **REQ-007**: The repository MUST provide proper error handling and logging capabilities
- **REQ-008**: The repository MUST implement comprehensive input validation for all public methods
- **REQ-009**: Error handling MUST provide specific exception types for different failure scenarios
- **REQ-010**: All operations MUST include structured logging with appropriate log levels

### Security Requirements

- **SEC-001**: All file I/O operations MUST validate input paths to prevent directory traversal attacks
- **SEC-002**: The repository MUST sanitize entity names before using them in file paths
- **SEC-003**: File operations MUST implement proper access controls and permissions validation
- **SEC-004**: All input parameters MUST be validated to prevent injection attacks and malformed data
- **SEC-005**: Sensitive information in file paths and names MUST be sanitized or encoded

### Performance Requirements

- **PER-001**: The repository MUST support concurrent operations without data corruption
- **PER-002**: File I/O operations MUST implement appropriate timeout mechanisms
- **PER-003**: Large semantic models MUST support streaming and chunked processing

### Error Handling Requirements

- **ERR-001**: All methods MUST validate input parameters and throw ArgumentException for invalid inputs
- **ERR-002**: File operations MUST throw specific exceptions (FileNotFoundException, UnauthorizedAccessException, etc.)
- **ERR-003**: Path validation failures MUST throw SecurityException with descriptive messages
- **ERR-004**: Serialization errors MUST throw JsonException or custom serialization exceptions
- **ERR-005**: Concurrent access violations MUST throw appropriate concurrency exceptions
- **ERR-006**: All exceptions MUST include contextual information for debugging and troubleshooting
- **ERR-007**: Critical errors MUST be logged at ERROR level with structured logging
- **ERR-008**: Recovery mechanisms MUST be implemented for transient failures

### Constraints

- **CON-001**: The implementation MUST be compatible with .NET 9 and modern C# language features
- **CON-002**: File-based persistence MUST use UTF-8 encoding for all text operations
- **CON-003**: Entity files MUST be saved with human-readable JSON formatting
- **CON-004**: The repository MUST maintain backward compatibility with existing semantic model formats
- **CON-005**: Maximum entity name length is limited to 128 characters per database restrictions

### Guidelines

- **GUD-001**: Use modern C# language features including primary constructors and nullable reference types
- **GUD-002**: Follow SOLID principles in repository design and implementation
- **GUD-003**: Implement comprehensive logging using structured logging patterns
- **GUD-004**: Use async/await patterns consistently throughout the implementation
- **GUD-005**: Apply the repository pattern to separate persistence concerns from business logic

### Patterns to Follow

- **PAT-001**: Use the Guard Clause pattern for input validation at method entry points
- **PAT-002**: Use the Factory pattern for creating repository instances based on persistence strategy
- **PAT-003**: Use the Strategy pattern for different validation and sanitization approaches
- **PAT-004**: Use the Builder pattern for complex semantic model construction operations
- **PAT-005**: Use the Template Method pattern for common error handling and logging workflows

## 4. Interfaces & Data Contracts

### Core Repository Interfaces

```csharp
/// <summary>
/// Defines the contract for schema repository operations that extract and transform 
/// raw database schema information into semantic model entities.
/// </summary>
public interface ISchemaRepository
{
    // Schema Discovery Operations
    Task<Dictionary<string, TableInfo>> GetTablesAsync(string? schema = null);
    Task<Dictionary<string, ViewInfo>> GetViewsAsync(string? schema = null);
    Task<Dictionary<string, StoredProcedureInfo>> GetStoredProceduresAsync(string? schema = null);
    
    // Schema Detail Operations
    Task<string> GetViewDefinitionAsync(ViewInfo view);
    Task<List<SemanticModelColumn>> GetColumnsForTableAsync(TableInfo table);
    Task<List<SemanticModelColumn>> GetColumnsForViewAsync(ViewInfo view);
    Task<List<Dictionary<string, object>>> GetSampleTableDataAsync(TableInfo tableInfo, int numberOfRecords = 5, bool selectRandom = false);
    Task<List<Dictionary<string, object>>> GetSampleViewDataAsync(ViewInfo viewInfo, int numberOfRecords = 5, bool selectRandom = false);
    
    // Semantic Model Factory Operations
    Task<SemanticModelTable> CreateSemanticModelTableAsync(TableInfo table);
    Task<SemanticModelView> CreateSemanticModelViewAsync(ViewInfo view);
    Task<SemanticModelStoredProcedure> CreateSemanticModelStoredProcedureAsync(StoredProcedureInfo storedProcedure);
}

/// <summary>
/// Defines the contract for semantic model provider operations that orchestrate
/// the creation, extraction, and management of complete semantic models.
/// </summary>
public interface ISemanticModelProvider
{
    /// <summary>
    /// Creates a new empty semantic model configured with project information.
    /// </summary>
    SemanticModel CreateSemanticModel();
    
    /// <summary>
    /// Loads an existing semantic model from the specified path.
    /// </summary>
    Task<SemanticModel> LoadSemanticModelAsync(DirectoryInfo modelPath);
    
    /// <summary>
    /// Extracts a complete semantic model from the connected database.
    /// </summary>
    Task<SemanticModel> ExtractSemanticModelAsync();
}

/// <summary>
/// Defines the contract for semantic model persistence operations.
/// </summary>
public interface ISemanticModel
{
    string Name { get; set; }
    string Source { get; set; }
    string? Description { get; set; }
    
    // Entity Collections
    List<SemanticModelTable> Tables { get; set; }
    List<SemanticModelView> Views { get; set; }
    List<SemanticModelStoredProcedure> StoredProcedures { get; set; }
    
    // Persistence Operations
    Task SaveModelAsync(DirectoryInfo modelPath);
    Task<SemanticModel> LoadModelAsync(DirectoryInfo modelPath);
    
    // Entity Management Operations
    void AddTable(SemanticModelTable table);
    bool RemoveTable(SemanticModelTable table);
    SemanticModelTable? FindTable(string schemaName, string tableName);
    List<SemanticModelTable> SelectTables(TableList tableList);
    
    void AddView(SemanticModelView view);
    bool RemoveView(SemanticModelView view);
    SemanticModelView? FindView(string schemaName, string viewName);
    
    void AddStoredProcedure(SemanticModelStoredProcedure storedProcedure);
    bool RemoveStoredProcedure(SemanticModelStoredProcedure storedProcedure);
    SemanticModelStoredProcedure? FindStoredProcedure(string schemaName, string procedureName);
      // Visitor Pattern Support
    void Accept(ISemanticModelVisitor visitor);
}

/// <summary>
/// Defines the contract for semantic model entities with persistence capabilities.
/// </summary>
public interface ISemanticModelEntity
{
    string Schema { get; set; }
    string Name { get; set; }
    string? Description { get; set; }
    string? SemanticDescription { get; set; }
    DateTime? SemanticDescriptionLastUpdate { get; set; }
    bool NotUsed { get; set; }
    string? NotUsedReason { get; set; }
    
    // Persistence Operations
    Task SaveModelAsync(DirectoryInfo folderPath);
    Task LoadModelAsync(DirectoryInfo folderPath);
    FileInfo GetModelEntityFilename();
    DirectoryInfo GetModelPath();    void SetSemanticDescription(string semanticDescription);
    
    // Visitor Pattern Support
    void Accept(ISemanticModelVisitor visitor);
}

/// <summary>
/// Defines the contract for path validation and sanitization operations.
/// </summary>
public interface IPathValidator
{
    /// <summary>
    /// Validates that a file path is safe and prevents directory traversal attacks.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <returns>True if the path is valid and safe.</returns>
    bool ValidatePath(string path);
    
    /// <summary>
    /// Sanitizes an entity name for safe use in file paths.
    /// </summary>
    /// <param name="entityName">The entity name to sanitize.</param>
    /// <returns>A sanitized entity name safe for file system use.</returns>
    string SanitizeEntityName(string entityName);
    
    /// <summary>
    /// Ensures a path is within the allowed base directory.
    /// </summary>
    /// <param name="basePath">The base directory path.</param>
    /// <param name="targetPath">The target path to validate.</param>
    /// <returns>True if the target path is within the base path.</returns>
    bool IsPathWithinBase(string basePath, string targetPath);
}

/// <summary>
/// Defines the contract for repository factory operations.
/// </summary>
public interface IRepositoryFactory
{
    /// <summary>
    /// Creates a schema repository instance based on the specified strategy.
    /// </summary>
    /// <param name="strategy">The persistence strategy to use.</param>
    /// <returns>A configured schema repository instance.</returns>
    ISchemaRepository CreateSchemaRepository(PersistenceStrategy strategy);
    
    /// <summary>
    /// Creates a semantic model provider instance based on the specified strategy.
    /// </summary>
    /// <param name="strategy">The persistence strategy to use.</param>
    /// <returns>A configured semantic model provider instance.</returns>
    ISemanticModelProvider CreateSemanticModelProvider(PersistenceStrategy strategy);
}

/// <summary>
/// Defines persistence strategy options for the repository pattern.
/// </summary>
public enum PersistenceStrategy
{
    FileSystem,
    Database,
    InMemory,
    Hybrid
}
```

## 5. Rationale & Context

### Repository Pattern Selection

The repository pattern was chosen to provide a clean abstraction layer between the domain logic and data access concerns. This pattern enables:

1. **Testability**: Easy mocking of data access operations for unit testing
2. **Flexibility**: Support for file-based persistence strategies
3. **Maintainability**: Clear separation of concerns between business logic and data access
4. **Extensibility**: Easy addition of new persistence mechanisms without affecting existing code

### File-Based Persistence Structure

The hierarchical file structure with separate entity files provides:

1. **Human Readability**: Individual JSON files can be examined and edited manually
2. **Version Control Compatibility**: Changes to individual entities create focused diffs
3. **Parallel Processing**: Multiple entities can be processed concurrently

### JSON Serialization Strategy

JSON was selected as the primary serialization format because:

1. **AI Compatibility**: Generative AI models work effectively with JSON structures
2. **Human Readability**: Developers can easily inspect and modify semantic models
3. **Language Agnostic**: JSON can be consumed by various programming languages
4. **Tooling Support**: Extensive ecosystem of JSON processing tools available

## 6. Examples & Edge Cases

### Basic Semantic Model Creation and Persistence

```csharp
// Create a semantic model provider
var semanticModelProvider = serviceProvider.GetRequiredService<ISemanticModelProvider>();

// Extract semantic model from database
var semanticModel = await semanticModelProvider.ExtractSemanticModelAsync();

// Persist the model to file system
var modelPath = new DirectoryInfo(@"C:\SemanticModels\MyDatabase");
await semanticModel.SaveModelAsync(modelPath);

// Load the model back from file system
var loadedModel = await semanticModelProvider.LoadSemanticModelAsync(modelPath);
```

### Repository Usage for Schema Extraction

```csharp
// Get schema repository instance
var schemaRepository = serviceProvider.GetRequiredService<ISchemaRepository>();

// Extract tables for specific schema
var tables = await schemaRepository.GetTablesAsync("Sales");

// Create semantic model entities
var semanticTables = new List<SemanticModelTable>();
foreach (var table in tables.Values)
{
    var semanticTable = await schemaRepository.CreateSemanticModelTableAsync(table);
    semanticTables.Add(semanticTable);
}
```

### Error Handling Edge Cases

```csharp
// Handle missing directory
try
{
    var model = await semanticModelProvider.LoadSemanticModelAsync(
        new DirectoryInfo(@"C:\NonExistent\Path"));
}
catch (DirectoryNotFoundException ex)
{
    logger.LogError(ex, "Semantic model directory not found: {Path}", ex.Message);
    // Fallback to creating new model
    model = semanticModelProvider.CreateSemanticModel();
}

// Handle corrupted model files
try
{
    var model = await semanticModelProvider.LoadSemanticModelAsync(modelPath);
}
catch (JsonException ex)
{
    logger.LogError(ex, "Corrupted semantic model JSON: {Path}", modelPath);
    // Fallback to re-extracting from database
    model = await semanticModelProvider.ExtractSemanticModelAsync();
}
```

### Security Validation Examples

```csharp
// Path validation implementation
public class PathValidator : IPathValidator
{
    private readonly string _baseDirectory;
    private readonly ILogger<PathValidator> _logger;

    public PathValidator(string baseDirectory, ILogger<PathValidator> logger)
    {
        _baseDirectory = Path.GetFullPath(baseDirectory);
        _logger = logger;
    }

    public bool ValidatePath(string path)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            
            // Check for directory traversal
            if (!fullPath.StartsWith(_baseDirectory, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Path traversal attempt detected: {Path}", path);
                return false;
            }

            // Check for invalid characters
            if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                _logger.LogWarning("Invalid path characters detected: {Path}", path);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Path validation failed for: {Path}", path);
            return false;
        }
    }

    public string SanitizeEntityName(string entityName)
    {
        ArgumentException.ThrowIfNullOrEmpty(entityName);

        // Remove invalid filename characters
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", entityName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

        // Ensure length constraints
        if (sanitized.Length > 128)
        {
            sanitized = sanitized[..125] + "...";
        }

        return sanitized;
    }

    public bool IsPathWithinBase(string basePath, string targetPath)
    {
        var baseFullPath = Path.GetFullPath(basePath);
        var targetFullPath = Path.GetFullPath(targetPath);
        
        return targetFullPath.StartsWith(baseFullPath, StringComparison.OrdinalIgnoreCase);
    }
}
```

### Enhanced Error Handling with Custom Exceptions

```csharp
// Custom exception types for semantic model operations
public class SemanticModelException : Exception
{
    public SemanticModelException(string message) : base(message) { }
    public SemanticModelException(string message, Exception innerException) : base(message, innerException) { }
}

public class SemanticModelValidationException : SemanticModelException
{
    public string PropertyName { get; }
    
    public SemanticModelValidationException(string propertyName, string message) 
        : base($"Validation failed for {propertyName}: {message}")
    {
        PropertyName = propertyName;
    }
}

public class SemanticModelSecurityException : SemanticModelException
{
    public SemanticModelSecurityException(string message) : base(message) { }
    public SemanticModelSecurityException(string message, Exception innerException) : base(message, innerException) { }
}

// Usage in repository implementation
public async Task SaveModelAsync(DirectoryInfo modelPath)
{
    // Validate input parameters
    ArgumentNullException.ThrowIfNull(modelPath);
    
    // Validate path security
    if (!_pathValidator.ValidatePath(modelPath.FullName))
    {
        throw new SemanticModelSecurityException($"Invalid or unsafe path: {modelPath.FullName}");
    }

    try
    {
        _logger.LogInformation("Saving semantic model to {Path}", modelPath.FullName);
        
        // Implementation details...
        
        _logger.LogInformation("Successfully saved semantic model to {Path}", modelPath.FullName);
    }
    catch (UnauthorizedAccessException ex)
    {
        var message = $"Access denied when saving semantic model to {modelPath.FullName}";
        _logger.LogError(ex, message);
        throw new SemanticModelException(message, ex);
    }
    catch (IOException ex)
    {
        var message = $"I/O error occurred while saving semantic model to {modelPath.FullName}";
        _logger.LogError(ex, message);
        throw new SemanticModelException(message, ex);
    }
    catch (JsonException ex)
    {
        var message = $"JSON serialization failed for semantic model";
        _logger.LogError(ex, message);
        throw new SemanticModelException(message, ex);
    }
}
```

### Concurrent Operations with Error Handling

```csharp
// Thread-safe repository operations
public class FileBasedSemanticModelRepository
{
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private readonly ILogger<FileBasedSemanticModelRepository> _logger;

    public async Task<SemanticModel> LoadModelAsync(DirectoryInfo modelPath)
    {
        await _fileLock.WaitAsync();
        try
        {
            _logger.LogDebug("Acquiring file lock for loading model from {Path}", modelPath.FullName);
            
            // Validate path
            if (!_pathValidator.ValidatePath(modelPath.FullName))
            {
                throw new SemanticModelSecurityException($"Path validation failed: {modelPath.FullName}");
            }

            // Load implementation with timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            
            // Implementation details...
            
            _logger.LogInformation("Successfully loaded semantic model from {Path}", modelPath.FullName);
            return model;
        }
        catch (OperationCanceledException)
        {
            var message = $"Loading semantic model from {modelPath.FullName} timed out";
            _logger.LogError(message);
            throw new SemanticModelException(message);
        }
        finally
        {
            _fileLock.Release();
            _logger.LogDebug("Released file lock for {Path}", modelPath.FullName);
        }
    }
}
```

## 7. Security Implementation Guidelines

### Input Validation Requirements

All repository implementations MUST implement comprehensive input validation following these guidelines:

1. **Parameter Validation**: Use guard clauses at method entry points
2. **Path Validation**: Implement `IPathValidator` for all file system operations
3. **Entity Name Sanitization**: Clean entity names before file system usage
4. **Length Validation**: Enforce maximum lengths for entity names and paths
5. **Character Validation**: Remove or escape invalid file system characters

### Error Recovery Strategies

1. **Transient Failures**: Implement retry logic with exponential backoff
2. **File Corruption**: Provide fallback to re-extract from source database
3. **Access Violations**: Log security events and provide meaningful error messages
4. **Timeout Handling**: Implement cancellation tokens for long-running operations
5. **Partial Failures**: Support resumable operations where possible

### Logging and Monitoring Requirements

1. **Security Events**: Log all path validation failures and access violations
2. **Performance Metrics**: Track operation durations and failure rates
3. **Structured Logging**: Use semantic logging with correlation IDs
4. **Error Context**: Include relevant context in exception messages
5. **Audit Trail**: Maintain logs of all persistence operations

## 8. Validation Criteria

### Functional Validation

- **VAL-001**: Repository MUST successfully persist and retrieve semantic models without data loss
- **VAL-002**: All async operations MUST complete within specified performance thresholds
- **VAL-003**: Concurrent access scenarios MUST not result in data corruption

### Performance Validation

- **VAL-006**: Model extraction for 100 tables MUST complete within 30 seconds
- **VAL-010**: File I/O operations MUST be optimized to minimize disk seeks

### Security Validation

- **VAL-011**: Path traversal attacks MUST be prevented through input validation
- **VAL-012**: Entity name sanitization MUST prevent file system injection
- **VAL-013**: JSON deserialization MUST be protected against malicious payloads
- **VAL-014**: All file operations MUST validate permissions before execution
- **VAL-015**: Path validation MUST reject relative paths and symbolic links

### Integration Validation

- **VAL-016**: Repository MUST integrate seamlessly with dependency injection container
- **VAL-017**: Error handling MUST provide meaningful diagnostics for troubleshooting
- **VAL-018**: Logging output MUST follow structured logging patterns
- **VAL-020**: Backward compatibility MUST be maintained for existing semantic models

## 8. Related Specifications / Further Reading

- [Infrastructure Deployment Bicep AVM Specification](./infrastructure-deployment-bicep-avm.md)
- [Microsoft .NET Application Architecture Guides](https://docs.microsoft.com/en-us/dotnet/architecture/)
- [Repository Pattern Documentation](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design)
- [Azure Well-Architected Framework](https://docs.microsoft.com/en-us/azure/architecture/framework/)
- [Entity Framework Core Change Tracking](https://docs.microsoft.com/en-us/ef/core/change-tracking/)
- [System.Text.Json Serialization Guide](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-overview)
- [Async Programming Best Practices](https://docs.microsoft.com/en-us/dotnet/csharp/async)
- [SOLID Principles in C#](https://docs.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/architectural-principles)
