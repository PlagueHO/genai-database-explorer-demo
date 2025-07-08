---
mode: 'agent'
description: 'Ensure .NET/C# code meets best practices for the solution/project.'
---
# .NET/C# Best Practices

Your goal is to help ensure that all .NET and C# code in this solution follows best practices for maintainability, performance, security, and clarity.

## General Guidelines
- Use the latest C# language features and .NET APIs
- Follow SOLID, DRY, and Clean Code principles
- Use meaningful, self-documenting names
- Prefer dependency injection for managing dependencies
- Write async code where appropriate
- Validate all inputs and use parameterized queries for data access
- Use logging and error handling best practices
- Write unit tests for all critical logic

## Code Style
- Use PascalCase for types and methods, camelCase for locals and parameters
- Keep methods short and focused
- Use expression-bodied members where appropriate
- Prefer records and pattern matching for data structures

## Security
- Never trust user input; always validate and sanitize
- Use secure defaults and avoid exposing sensitive data

## Example
```csharp
public record User(string Name, string Email);

public class UserService
{
    private readonly ILogger<UserService> _logger;
    public UserService(ILogger<UserService> logger) => _logger = logger;

    public async Task<User> GetUserAsync(int id)
    {
        // ...implementation...
    }
}
```
