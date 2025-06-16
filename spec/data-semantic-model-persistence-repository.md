---
title: Data Persistence for Semantic Model Repository Pattern
version: 1.1
date_created: 2025-06-16
last_updated: 2025-06-16
owner: GenAI Database Explorer Development Team
tags: [data, repository, semantic-model, persistence, database, generative-ai, lazy-loading, change-tracking]
---

A specification defining the requirements, constraints, and interfaces for persisting semantic database models using a repository pattern in the Generative AI database explorer application.

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
- **Entity Proxy**: A surrogate object that implements lazy loading by intercepting property access and loading data on demand
- **Change Tracker**: A component that monitors modifications to entities and maintains state information for persistence optimization

## 3. Requirements, Constraints & Guidelines

### Core Requirements

- **REQ-001**: The repository pattern MUST provide abstraction for semantic model persistence operations
- **REQ-002**: The implementation MUST support asynchronous operations for all I/O-bound activities
- **REQ-003**: The repository MUST handle both file-based and future database-based persistence strategies
- **REQ-004**: The semantic model MUST be persisted in a hierarchical structure with separate files for entities
- **REQ-005**: The repository MUST support CRUD operations (Create, Read, Update, Delete) for semantic models
- **REQ-006**: The implementation MUST use dependency injection for component management
- **REQ-007**: The repository MUST provide proper error handling and logging capabilities
- **REQ-008**: The repository MUST support lazy loading of semantic model entities to optimize memory usage
- **REQ-009**: The repository MUST implement dirty tracking to identify modified entities for selective persistence
- **REQ-010**: The repository MUST support partial model loading based on entity access patterns

### Security Requirements

- **SEC-001**: All file I/O operations MUST validate paths to prevent directory traversal attacks
- **SEC-002**: Sensitive connection string information MUST be handled securely in persistence operations
- **SEC-003**: File permissions MUST be appropriately set for semantic model storage directories

### Performance Requirements

- **PER-001**: The repository MUST support parallel processing for entity extraction and persistence
- **PER-002**: Large semantic models MUST be persisted incrementally to avoid memory exhaustion
- **PER-003**: The implementation MUST provide configurable parallelism levels
- **PER-004**: Lazy loading MUST be implemented to defer entity loading until first access
- **PER-005**: Dirty tracking MUST minimize save operations by persisting only modified entities
- **PER-006**: Entity proxies MUST be used to enable transparent lazy loading behavior

### Constraints

- **CON-001**: The repository implementation MUST be thread-safe for concurrent operations
- **CON-002**: File-based persistence MUST use JSON format for human readability and tool compatibility
- **CON-003**: The implementation MUST maintain backward compatibility with existing semantic model formats
- **CON-004**: Memory usage MUST be optimized for large database schemas with thousands of entities
- **CON-005**: Lazy loaded entities MUST maintain referential integrity when accessed across different contexts
- **CON-006**: Dirty tracking state MUST be thread-safe for concurrent entity modifications

### Guidelines

- **GUD-001**: Use modern C# language features including primary constructors and nullable reference types
- **GUD-002**: Follow SOLID principles in repository design and implementation
- **GUD-003**: Implement comprehensive logging using structured logging patterns
- **GUD-004**: Use async/await patterns consistently throughout the implementation
- **GUD-005**: Apply the repository pattern to separate persistence concerns from business logic
- **GUD-006**: Implement lazy loading using proxy patterns or virtual properties to minimize memory footprint
- **GUD-007**: Use change tracking mechanisms similar to Entity Framework for dirty detection
- **GUD-008**: Implement smart loading strategies based on entity access patterns and relationships

### Patterns to Follow

- **PAT-001**: Implement the Unit of Work pattern for managing related semantic model operations
- **PAT-002**: Use the Factory pattern for creating repository instances based on persistence strategy
- **PAT-003**: Apply the Strategy pattern for different persistence mechanisms (file, database, cloud)
- **PAT-004**: Use the Builder pattern for complex semantic model construction operations
- **PAT-005**: Implement the Proxy pattern for lazy loading of semantic model entities
- **PAT-006**: Use the Observer pattern for dirty tracking and change notification
- **PAT-007**: Apply the Command pattern for tracking entity modifications and implementing undo operations

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
```

### Lazy Loading and Change Tracking Interfaces

```csharp
/// <summary>
/// Defines the contract for lazy loading behavior in semantic model entities.
/// </summary>
public interface ILazyLoadable
{
    /// <summary>
    /// Gets a value indicating whether the entity has been fully loaded from storage.
    /// </summary>
    bool IsLoaded { get; }
    
    /// <summary>
    /// Forces the lazy loading of the entity if not already loaded.
    /// </summary>
    Task LoadAsync();
    
    /// <summary>
    /// Gets a value indicating whether the entity is currently being loaded.
    /// </summary>
    bool IsLoading { get; }
}

/// <summary>
/// Defines the contract for change tracking in semantic model entities.
/// </summary>
public interface ITrackable
{
    /// <summary>
    /// Gets a value indicating whether the entity has been modified since last save.
    /// </summary>
    bool IsDirty { get; }
    
    /// <summary>
    /// Gets the original values of the entity before modifications.
    /// </summary>
    IDictionary<string, object?> OriginalValues { get; }
    
    /// <summary>
    /// Gets the current values of the entity.
    /// </summary>
    IDictionary<string, object?> CurrentValues { get; }
    
    /// <summary>
    /// Marks the entity as clean (not modified).
    /// </summary>
    void MarkAsClean();
    
    /// <summary>
    /// Marks the entity as dirty (modified).
    /// </summary>
    void MarkAsDirty();
    
    /// <summary>
    /// Gets the specific properties that have been modified.
    /// </summary>
    IEnumerable<string> GetModifiedProperties();
}

/// <summary>
/// Combines lazy loading and change tracking capabilities.
/// </summary>
public interface ISemanticModelEntity : ILazyLoadable, ITrackable
{
    /// <summary>
    /// Gets the unique identifier for the entity.
    /// </summary>
    string EntityId { get; }
    
    /// <summary>
    /// Gets the entity type for tracking purposes.
    /// </summary>
    string EntityType { get; }
    
    /// <summary>
    /// Gets the timestamp when the entity was last modified.
    /// </summary>
    DateTimeOffset LastModified { get; }
}

/// <summary>
/// Defines the contract for a change tracker that monitors entity modifications.
/// </summary>
public interface IChangeTracker
{
    /// <summary>
    /// Starts tracking changes for the specified entity.
    /// </summary>
    void StartTracking<T>(T entity) where T : ITrackable;
    
    /// <summary>
    /// Stops tracking changes for the specified entity.
    /// </summary>
    void StopTracking<T>(T entity) where T : ITrackable;
    
    /// <summary>
    /// Gets all entities that have been modified.
    /// </summary>
    IEnumerable<ITrackable> GetModifiedEntities();
    
    /// <summary>
    /// Gets all entities of a specific type that have been modified.
    /// </summary>
    IEnumerable<T> GetModifiedEntities<T>() where T : ITrackable;
    
    /// <summary>
    /// Accepts all changes and marks all tracked entities as clean.
    /// </summary>
    void AcceptAllChanges();
    
    /// <summary>
    /// Rejects all changes and reverts all tracked entities to their original state.
    /// </summary>
    void RejectAllChanges();
}

/// <summary>
/// Defines the contract for lazy loading services.
/// </summary>
public interface ILazyLoader
{
    /// <summary>
    /// Loads the specified entity asynchronously.
    /// </summary>
    Task LoadAsync<T>(T entity) where T : ILazyLoadable;
    
    /// <summary>
    /// Loads the specified property of an entity asynchronously.
    /// </summary>
    Task LoadPropertyAsync<T>(T entity, string propertyName) where T : ILazyLoadable;
    
    /// <summary>
    /// Configures lazy loading behavior for a specific entity type.
    /// </summary>
    void Configure<T>(Func<T, Task> loadAction) where T : ILazyLoadable;
}
```

### Persistence Strategy Interfaces

```csharp
/// <summary>
/// Defines the contract for different semantic model persistence strategies.
/// </summary>
public interface ISemanticModelPersistenceStrategy
{
    Task SaveAsync(SemanticModel model, string location);
    Task<SemanticModel> LoadAsync(string location);
    Task<bool> ExistsAsync(string location);
    Task DeleteAsync(string location);
    
    // Lazy Loading Support
    Task<T> LoadEntityAsync<T>(string entityId, string location) where T : ISemanticModelEntity;
    Task<bool> EntityExistsAsync(string entityId, string location);
    
    // Selective Persistence Support
    Task SaveModifiedEntitiesAsync(IEnumerable<ITrackable> modifiedEntities, string location);
    Task SaveEntityAsync<T>(T entity, string location) where T : ISemanticModelEntity;
}

/// <summary>
/// File-based persistence strategy for semantic models with lazy loading support.
/// </summary>
public interface IFileSemanticModelPersistenceStrategy : ISemanticModelPersistenceStrategy
{
    Task SaveAsync(SemanticModel model, DirectoryInfo directory);
    Task<SemanticModel> LoadAsync(DirectoryInfo directory);
    Task<bool> ExistsAsync(DirectoryInfo directory);
    Task DeleteAsync(DirectoryInfo directory);
    
    // Lazy Loading Support
    Task<T> LoadEntityAsync<T>(string entityId, DirectoryInfo directory) where T : ISemanticModelEntity;
    Task<IEnumerable<string>> GetEntityIdsAsync(DirectoryInfo directory, Type entityType);
    
    // Selective Persistence Support
    Task SaveModifiedEntitiesAsync(IEnumerable<ITrackable> modifiedEntities, DirectoryInfo directory);
    Task<FileInfo> GetEntityFilePathAsync(string entityId, DirectoryInfo directory, Type entityType);
}
```

### Data Transfer Objects

```csharp
/// <summary>
/// Represents database table metadata for schema extraction.
/// </summary>
public sealed record TableInfo(string SchemaName, string TableName);

/// <summary>
/// Represents database view metadata for schema extraction.
/// </summary>
public sealed record ViewInfo(string SchemaName, string ViewName);

/// <summary>
/// Represents stored procedure metadata for schema extraction.
/// </summary>
public sealed record StoredProcedureInfo(
    string SchemaName, 
    string ProcedureName, 
    string Type, 
    string? Parameters, 
    string Definition);

/// <summary>
/// Represents reference/foreign key relationship metadata.
/// </summary>
public sealed record ReferenceInfo(
    string SchemaName,
    string TableName,
    string ColumnName,
    string ReferencedTableName,
    string ReferencedColumnName);
```

## 5. Rationale & Context

### Repository Pattern Selection

The repository pattern was chosen to provide a clean separation between the domain logic and data access logic. This abstraction enables:

1. **Testability**: Repository interfaces can be easily mocked for unit testing
2. **Flexibility**: Multiple persistence strategies can be implemented without changing business logic
3. **Maintainability**: Data access concerns are centralized and can be modified independently
4. **Future Extensibility**: Additional persistence mechanisms (database, cloud storage) can be added seamlessly

### File-Based Persistence Strategy

The initial implementation uses file-based persistence because:

1. **Simplicity**: No additional database infrastructure required
2. **Version Control**: Semantic models can be tracked in source control systems
3. **Human Readability**: JSON format enables manual inspection and debugging
4. **Portability**: Models can be easily shared between environments

### Hierarchical Storage Structure

The semantic model is persisted in a hierarchical directory structure:

```text
semantic-model/
├── semanticmodel.json          # Main model metadata
├── tables/                     # Table entities
│   ├── schema.table1.json
│   └── schema.table2.json
├── views/                      # View entities
│   ├── schema.view1.json
│   └── schema.view2.json
└── storedprocedures/          # Stored procedure entities
    ├── schema.proc1.json
    └── schema.proc2.json
```

This structure provides:

- **Scalability**: Large schemas don't result in monolithic files
- **Granular Updates**: Individual entities can be modified without affecting others
- **Parallel Processing**: Multiple entities can be processed concurrently
- **Organization**: Clear separation of different entity types

### Asynchronous Operations

All I/O operations are asynchronous to:

- **Improve Responsiveness**: Prevent UI blocking during long-running operations
- **Enable Parallelism**: Multiple database queries can execute concurrently
- **Optimize Resource Usage**: Thread pool threads are not blocked during I/O waits
- **Support Scalability**: Better handling of concurrent requests in server scenarios

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

### Parallel Processing Configuration

```csharp
// Configure parallelism for large schemas
var parallelOptions = new ParallelOptions
{
    MaxDegreeOfParallelism = Environment.ProcessorCount / 2 // Conservative approach
};

// Extract tables in parallel
await Parallel.ForEachAsync(tableInfos, parallelOptions, async (table, ct) =>
{    var semanticTable = await schemaRepository.CreateSemanticModelTableAsync(table);
    semanticModel.AddTable(semanticTable);
});
```

### Lazy Loading Implementation Example

```csharp
// Create a semantic model with lazy loading enabled
var semanticModel = await semanticModelProvider.LoadSemanticModelAsync(modelPath);

// Tables are loaded as proxies - actual data is not loaded yet
var salesTable = semanticModel.FindTable("Sales", "Orders");

// Accessing columns will trigger lazy loading
var columns = salesTable.Columns; // Data is loaded on first access

// Check if entity has been loaded
if (salesTable is ILazyLoadable lazyTable && !lazyTable.IsLoaded)
{
    await lazyTable.LoadAsync(); // Explicitly load if needed
}
```

### Dirty Tracking and Selective Persistence Example

```csharp
// Get change tracker service
var changeTracker = serviceProvider.GetRequiredService<IChangeTracker>();

// Load semantic model with change tracking
var semanticModel = await semanticModelProvider.LoadSemanticModelAsync(modelPath);

// Modify an entity
var table = semanticModel.FindTable("Sales", "Customers");
if (table is ITrackable trackableTable)
{
    changeTracker.StartTracking(trackableTable);
    table.Description = "Updated customer information table";
    // Entity is automatically marked as dirty
}

// Save only modified entities
var modifiedEntities = changeTracker.GetModifiedEntities();
if (modifiedEntities.Any())
{
    var persistenceStrategy = serviceProvider.GetRequiredService<IFileSemanticModelPersistenceStrategy>();
    await persistenceStrategy.SaveModifiedEntitiesAsync(modifiedEntities, modelPath);
    changeTracker.AcceptAllChanges(); // Mark all as clean
}
```

### Smart Loading Based on Access Patterns

```csharp
// Configure lazy loader for optimized loading
var lazyLoader = serviceProvider.GetRequiredService<ILazyLoader>();

// Configure table loading to include frequently accessed properties
lazyLoader.Configure<SemanticModelTable>(async table =>
{
    // Load table with columns and indexes in a single operation
    var tableInfo = new TableInfo(table.Schema, table.Name);
    table.Columns.AddRange(await schemaRepository.GetColumnsForTableAsync(tableInfo));
    table.Indexes.AddRange(await schemaRepository.GetIndexesForTableAsync(tableInfo));
});

// Access patterns are optimized based on configuration
var table = semanticModel.FindTable("Sales", "Orders");
await table.LoadAsync(); // Loads columns and indexes together
```

### Batch Operations with Dirty Tracking

```csharp
// Perform bulk modifications
var changeTracker = serviceProvider.GetRequiredService<IChangeTracker>();

foreach (var table in semanticModel.Tables.Where(t => t.Schema == "Sales"))
{
    if (table is ITrackable trackable)
    {
        changeTracker.StartTracking(trackable);
        table.Description = $"Sales schema table: {table.Name}";
    }
}

// Get only modified tables for persistence
var modifiedTables = changeTracker.GetModifiedEntities<SemanticModelTable>();
logger.LogInformation("Saving {Count} modified tables", modifiedTables.Count());

// Efficient batch save operation
await persistenceStrategy.SaveModifiedEntitiesAsync(modifiedTables, modelPath);
```

### Memory Optimization with Lazy Loading

```csharp
// Load semantic model metadata only
var semanticModel = await semanticModelProvider.LoadSemanticModelAsync(modelPath);
logger.LogInformation("Loaded model with {TableCount} tables (proxies)", semanticModel.Tables.Count);

// Tables are proxies - minimal memory usage
var totalMemoryBefore = GC.GetTotalMemory(false);

// Access specific table - only this table's data is loaded
var ordersTable = semanticModel.FindTable("Sales", "Orders");
var columnCount = ordersTable.Columns.Count; // Triggers loading for this table only

var totalMemoryAfter = GC.GetTotalMemory(false);
logger.LogInformation("Memory increased by {MemoryDelta} bytes for single table", 
    totalMemoryAfter - totalMemoryBefore);
```

## 7. Validation Criteria

### Functional Validation

1. **Model Extraction**: Semantic model successfully extracts all tables, views, and stored procedures from a test database
2. **Persistence**: Semantic model can be saved to and loaded from the file system without data loss
3. **Entity Relationships**: Foreign key relationships are correctly captured and persisted
4. **Incremental Updates**: Individual entities can be updated without affecting the entire model
5. **Error Recovery**: System gracefully handles corrupted files and missing directories
6. **Lazy Loading**: Entities are loaded on-demand and memory usage remains minimal until access
7. **Dirty Tracking**: Only modified entities are identified and persisted during save operations
8. **Proxy Behavior**: Lazy-loaded entities behave identically to fully-loaded entities from a consumer perspective

### Performance Validation

1. **Large Schema Handling**: System can process databases with 1000+ tables within acceptable time limits
2. **Memory Usage**: Memory consumption remains stable during processing of large schemas
3. **Parallel Processing**: Multi-threaded operations complete faster than sequential processing
4. **File I/O Efficiency**: Persistence operations complete within expected time boundaries
5. **Lazy Loading Performance**: Initial model loading time is significantly reduced compared to eager loading
6. **Selective Persistence**: Save operations process only dirty entities, reducing I/O overhead
7. **Memory Efficiency**: Memory usage scales with accessed entities, not total entity count

### Quality Validation

1. **Thread Safety**: Concurrent operations do not cause data corruption or deadlocks
2. **Resource Cleanup**: All file handles and database connections are properly disposed
3. **Logging Coverage**: All significant operations and errors are properly logged
4. **Exception Handling**: All exceptions are caught and handled appropriately
5. **Change Tracking Accuracy**: Dirty detection correctly identifies all modified properties and entities
6. **Lazy Loading Transparency**: Consumers cannot distinguish between lazy-loaded and eagerly-loaded entities
7. **State Consistency**: Entity state remains consistent across lazy loading and change tracking operations

### Security Validation

1. **Path Validation**: File paths are validated to prevent directory traversal attacks
2. **Connection String Security**: Database connection information is handled securely
3. **File Permissions**: Created files have appropriate access permissions
4. **Input Sanitization**: All user inputs are properly validated and sanitized

## 8. Related Specifications / Further Reading

- [Repository Pattern Documentation](https://martinfowler.com/eaaCatalog/repository.html) - Martin Fowler's Enterprise Application Architecture Catalog
- [.NET Dependency Injection](https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection) - Microsoft Documentation
- [Async/Await Best Practices](https://docs.microsoft.com/en-us/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming) - MSDN Magazine
- [JSON Serialization in .NET](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-overview) - Microsoft Documentation
- [Database Schema Documentation](../docs/gaidbexp/README.md) - GenAI Database Explorer Documentation
- [SOLID Principles](https://en.wikipedia.org/wiki/SOLID) - Object-oriented design principles
- [Unit of Work Pattern](https://martinfowler.com/eaaCatalog/unitOfWork.html) - Enterprise Application Architecture Pattern
