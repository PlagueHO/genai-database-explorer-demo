---
title: Web API Specification for Semantic Model CRUD (.NET 9)
version: 1.0
date_created: 2025-07-14
owner: GenAIDBExplorer Team
tags: [webapi, .NET, semantic-model, CRUD, app]
---

# Introduction

This specification defines the requirements, constraints, and interfaces for a .NET 9 Web API application that provides Create, Read, Update, and Delete (CRUD) operations over the `SemanticModel` entity. The API is intended for local development and testing, with no authentication or authorization enforced at this stage.

## 1. Purpose & Scope

The purpose is to expose a RESTful Web API for managing semantic models, supporting full CRUD operations. The scope includes API endpoints, data contracts, error handling, and testability. Intended for developers and AI agents integrating with the semantic model locally.

## 2. Definitions

- **Web API**: HTTP-based interface for programmatic access to application functionality.
- **CRUD**: Create, Read, Update, Delete operations.
- **SemanticModel**: Domain entity representing a database schema and its metadata.
- **DTO**: Data Transfer Object for API requests/responses.
- **.NET 9**: Target runtime and framework.

## 3. Requirements, Constraints & Guidelines

- **REQ-001**: Provide RESTful endpoints for CRUD operations on SemanticModel.
- **REQ-002**: Use .NET 9 and ASP.NET Core Web API project template.
- **REQ-003**: Return standard HTTP status codes and error messages.
- **REQ-004**: Use DTOs for request and response payloads.
- **REQ-005**: No authentication/authorization required (local only).
- **REQ-006**: Support JSON serialization for all payloads.
- **REQ-007**: Implement input validation and error handling.
- **CON-001**: Must be added to the solution file `GenAIDBExplorer.sln`.
- **GUD-001**: Use meaningful, self-documenting names for controllers, actions, and models.
- **PAT-001**: Use Dependency Injection for repository and service layers.

## 4. Interfaces & Data Contracts

### API Endpoints
| Method | Route                   | Description                  |
|--------|------------------------|------------------------------|
| GET    | /api/semanticmodels    | List all semantic models     |
| GET    | /api/semanticmodels/{id} | Get semantic model by ID     |
| POST   | /api/semanticmodels    | Create new semantic model    |
| PUT    | /api/semanticmodels/{id} | Update semantic model by ID  |
| DELETE | /api/semanticmodels/{id} | Delete semantic model by ID  |

### DTO Example
```json
{
  "id": "string",
  "name": "string",
  "tables": [ ... ],
  "metadata": { ... }
}
```

## 5. Acceptance Criteria

- **AC-001**: Given a valid request, When a CRUD endpoint is called, Then the correct operation is performed and a standard response is returned.
- **AC-002**: When invalid input is provided, Then a 400 Bad Request is returned with error details.
- **AC-003**: When a resource is not found, Then a 404 Not Found is returned.
- **AC-004**: All endpoints are covered by unit and integration tests.

## 6. Test Automation Strategy

- **Test Levels**: Unit, Integration
- **Frameworks**: MSTest, FluentAssertions, Moq
- **Test Data Management**: Use in-memory repository for tests
- **CI/CD Integration**: Automated tests in GitHub Actions pipelines
- **Coverage Requirements**: Minimum 85% code coverage

## 7. Rationale & Context

A local Web API enables rapid development, testing, and integration of semantic model features. Exposing CRUD operations via RESTful endpoints supports interoperability and future extension to cloud or secured environments.

## 8. Dependencies & External Integrations

### External Systems
- None (local only)

### Third-Party Services
- None

### Infrastructure Dependencies
- Localhost runtime

### Data Dependencies
- In-memory or file-based repository for semantic models

### Technology Platform Dependencies
- .NET 9, ASP.NET Core

### Compliance Dependencies
- None (local only)

## 9. Examples & Edge Cases

```http
POST /api/semanticmodels
{
  "name": "AdventureWorks",
  "tables": [ ... ],
  "metadata": { "description": "Sample model" }
}

GET /api/semanticmodels/invalid-id
// Returns 404 Not Found
```

## 10. Validation Criteria

- All endpoints return correct status codes and payloads.
- Input validation and error handling are verified.
- Solution file is updated to include the new Web API project.

## 11. Related Specifications / Further Reading

- [spec-data-semantic-model.md]
- [https://learn.microsoft.com/en-us/aspnet/core/web-api/]
- [https://learn.microsoft.com/en-us/dotnet/core/introduction]
