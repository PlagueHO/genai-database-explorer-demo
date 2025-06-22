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

### Security Requirements

- **SEC-001**: All file I/O operations MUST validate input paths to prevent directory traversal attacks
- **SEC-002**: The repository MUST sanitize entity names before using them in file paths

### Performance Requirements

- **PER-001**: The repository MUST support concurrent operations without data corruption

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

- **PAT-002**: Use the Factory pattern for creating repository instances based on persistence strategy
- **PAT-004**: Use the Builder pattern for complex semantic model construction operations

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
    DirectoryInfo GetModelPath();
    void SetSemanticDescription(string semanticDescription);
    
    // Visitor Pattern Support
    void Accept(ISemanticModelVisitor visitor);
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

## 7. Validation Criteria

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
