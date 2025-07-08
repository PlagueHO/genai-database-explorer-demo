---
mode: 'agent'
tools: ['changes', 'codebase', 'editFiles', 'problems', 'search']
description: 'Get best practices for MSTest unit testing, including data-driven tests'
---

# MSTest Best Practices

Your goal is to help me follow best practices when writing MSTest unit tests for .NET applications.

## Test Structure
- Use the AAA (Arrange, Act, Assert) pattern for all tests
- Name tests using the `Method_State_Expected` convention
- Group related tests in the same class

## Data-Driven Tests
- Use `[DataTestMethod]` and `[DataRow]` for parameterized tests
- Prefer clear, simple data sets for readability

## Assertions
- Use FluentAssertions for expressive, readable assertions
- Avoid multiple assertions per test unless necessary

## Mocking
- Use Moq for mocking dependencies
- Mock only what you own; avoid over-mocking

## Test Coverage
- Ensure all public methods and critical paths are covered
- Use code coverage tools to identify gaps

## Test Performance
- Keep tests fast and isolated
- Avoid external dependencies unless integration testing

## Example
```csharp
[TestClass]
public class CalculatorTests
{
    [TestMethod]
    public void Add_TwoNumbers_ReturnsSum()
    {
        // Arrange
        var calc = new Calculator();
        // Act
        var result = calc.Add(2, 3);
        // Assert
        result.Should().Be(5);
    }
}
```
