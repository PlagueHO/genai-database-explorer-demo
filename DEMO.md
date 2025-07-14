# Demo steps

## Part 1 - Customize Copilot

### Set up Copilot Instructions

```md
# Instructions for AI Agents in this Repository
This is a .NET 9 solution that uses Generative AI to help users explore and query relational databases. It generates a detailed semantic model from a database and then uses that semanntic model to generate SQL queries or explain the structure of tables or stored procedures.

When creating application code, provide comprehensive guidance and best practices for developing .NET 9 applications that are designed to run in Azure. Use the latest C# 14 development features and language constructs to build a modern, scalable, and secure application.

## Key Principles
- Use the latest C# 14 language features and constructs to build modern, scalable, and secure applications.
- Use SOLID principles (Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, and Dependency Inversion) to design and implement your application.
- Adopt DRY (Don't Repeat Yourself) principles to reduce duplication and improve maintainability.
- Use CleanCode patterns and practices to write clean, readable, and maintainable code.
- Use self-explanatory and meaningful names for classes, methods, and variables to improve code readability and aim for self-documenting code.
- Security > Maintainability > Performance: prioritize security and maintainability over performance, but strive for a balance.
- Use Dependency Injection to manage dependencies and improve testability.
- Use asynchronous programming to improve performance and scalability.
- Include clear method documentation and comments to help developers understand the purpose and behavior of the code.
- Prioritize secure coding practices, such as input validation, output encoding, and parameterized queries, to prevent common security vulnerabilities.
- Use Semantic Kernel and Prompty SDKs to interact with the Generative AI models.
- Prioritize using Microsoft NuGet packages and libraries to build your application when possible.
- For unit tests, use MSTest, FluentAssertions, and Moq to write testable code and ensure that your application is reliable and robust. As well as using AAA pattern for test structure.
- Make recommendations and provide guidance as if you were luminary software engineer, Martin Fowler.

## High-level Architecture
- **Console App** (`GenAIDBExplorer.Console`): CLI for project management, model operations, and queries
- **Core Library** (`GenAIDBExplorer.Core`): domain logic, semantic providers, data dictionary, export, kernel memory
- **Tests**: MSTest + FluentAssertions + Moq, following AAA pattern in `src/GenAIDBExplorer/Tests/Unit`
- **Infrastructure**: Bicep templates under `infra/`, deployable via GitHub Actions workflows
- **Documentation**: usage guides in `docs/`
- **Samples**: `samples/AdventureWorksLT` for data dictionary preprocessing

## Style & Conventions
- Target .NET 9 with C# 11 features (async/await, records, pattern matching)
- Follow SOLID, DRY, CleanCode; meaningful, self-documenting names
- PascalCase for types/methods; camelCase for parameters/locals
- Dependency Injection via `HostBuilderExtensions` and `IOptions<T>`
- Secure coding: parameterized queries, input validation, output encoding
- Logging via `Microsoft.Extensions.Logging`
- Code formatting: run `dotnet format` (pre-commit), follow default .editorconfig or EditorConfig conventions
- Tests: Use AAA pattern, clear test names `Method_State_Expected`, mock with Moq, assert with FluentAssertions

## Agent Rules
- This `.github/copilot-instructions.md` directs AI agents in this repo
- Preserve existing Azure and infrastructure guidance
- Merge, don’t overwrite; be concise and factual
```

### Set up Commit Message Template

```json

    "github.copilot.chat.commitMessageGeneration.instructions": [
        {
            "text": "The first line should be summary of no more than 50 characters starting with classification of commit from: `CHORE:`|`FIX:`|`CHANGE:`|`BREAKING CHANGE:`|`TESTS:`|`SECURITY:`|`COMPLEX:`. The second line should be blank. The following lines should be the full summary with each item starting with a `-`. Any summary item that is security related should start with `SECURITY:`. Any summary item change that causes a breaking change to a feature/interface/dependency should start with `BREAKING CHANGE:`"
        }
    ]
```

### Set up MCP Servers
```json
{
    "inputs": [],
    "servers": {
        "playwright": {
            "command": "npx",
            "args": [
                "-y",
                "@playwright/mcp@latest"
            ],
            "type": "stdio",
            "env": {}
        },
       "microsoft.docs.mcp": {
            "type": "http",
            "url": "https://learn.microsoft.com/api/mcp"
        },
        "github": {
            "url": "https://api.githubcopilot.com/mcp/",
            "type": "http"
        }
    }
}
```

## Part 2 - Building a new Application

Switch to `Agent` chat mode:

```md
Add a .NET 9 Web API app project that provides CRUD operations over a Semantic Model found in #file:SemanticModel to the existing solution #file:GenAIDBExplorer.sln. Don't worry about authZ/authN as it will only run locally at the moment. It should use the #file:GenAIDBExplorer.Core project to the access to the semantic model.
```

This will do OK... but probably not perfectly. So let's refine it a bit.

```md
Add a .NET 9 Web API app project that provides CRUD operations over a Semantic Model found in #file:SemanticModel to the existing solution #file:GenAIDBExplorer.sln. Don't worry about authZ/authN as it will only run locally at the moment. It should be added as a new project to the #file:GenAIDBExplorer.sln . Call it `spec-app-semanticmodel-webapi`. Make sure you include guidance from https://learn.microsoft.com/en-us/aspnet/core/fundamentals/apis?view=aspnetcore-9.0.
```

```md
Add a .NET 9 Web API app project that provides CRUD operations over a Semantic Model found in #file:SemanticModel to the existing solution #file:GenAIDBExplorer.sln . Don't worry about authZ/authN as it will only run locally at the moment. It should be added as a new project to the #file:GenAIDBExplorer.sln . Call it `spec-app-semanticmodel-webapi`. Make sure you include guidance from https://learn.microsoft.com/en-us/aspnet/core/fundamentals/apis?view=aspnetcore-9.0.
```

```md
/create_specification for a .NET 9 Web API app project that provides CRUD operations over a Semantic Model #file:SemanticModel . Don't worry about authZ/authN as it will only run locally at the moment. It should be added as a new project to the #file:GenAIDBExplorer.sln . Call it `spec-app-semanticmodel-webapi`. Make sure you include guidance from https://learn.microsoft.com/en-us/aspnet/core/fundamentals/apis?view=aspnetcore-9.0.
```

Switch to `mentor` mode:

```md
Check the #file:spec-app-semanticmodel-webapi.md. Is there anything I should consider adding to this?
```

```md
Why not use Query String based API versioning?
```

Switch to `Agent` mode:

```md
/create-implementation-plan from the spec #file:spec-app-semanticmodel-webapi.md. Break it down into small atomoic phases with a complete working, buildable project at the end of each phase. Call it `plan-app-semanticmodel-webapi-v1`.
```

```md
Build out Phase 1 of the implementation plan #file:plan-app-semanticmodel-webapi-v1.md. Update the completion status of the plan as you go.
```

## Part 3 - Workspace Index

Switch to `Agent` chat mode:

```md
@workspace How do I add a new Command to the Console App to remove semantic model items?
```

1. Click the GitHub Copilot icon in the taskbar.
2. Show `Remotely Indexed` which indicates that the workspace is indexed in GitHub. Local Indexing is available for non GitHub repositories, but it's not as good as the GitHub index.
3. Open [https://github.com/PlagueHO/genai-database-explorer-demo](https://github.com/PlagueHO/genai-database-explorer-demo) and press `Shift+C` to open the Copilot Chat.
4. Enter:

```md
How do I add a new Command to the Console App to remove semantic model items?
```

## Part 4 - Upgrade Package with a Plan

Switch to `Agent` chat mode "This agent will create a plan to upgrade the System.CommandLine package in the GenAIDBExplorer.Console project to version 2.0.0-beta5.25306.1. - but it probably won't do a great job of it":

```md
Refactor the #file:GenAIDBExplorer.Console.csproj to update the System.CommandLine package to use version 2.0.0-beta5.25306.1.
```

Switch to `implementation_plan` mode:

```md
Create a plan for updating the package System.CommandLine in #file:GenAIDBExplorer.Console from the current version in the project to 2.0.0-beta5.25306.1. Refer to the migration instructions in https://learn.microsoft.com/en-us/dotnet/standard/commandline/migration-guide-2.0.0-beta5
```

Switch to `Agent` mode:

```md
/create_github_issue_from_implementation_plan PlanFile:#file:upgrade-system-commandline-beta5.md using #file:chore_request.yml template
```

Swtich to Coding Agent:

Open [GitHub Issues](https://github.com/PlagueHO/genai-database-explorer-demo/issues) and find the issue created by the previous step. Review the issue and then assign it to Copilot.

Open Coding Agents page: [https://github.com/copilot/agents](https://github.com/copilot/agents)

## Part 5 - Adding Lazy Loading and Dirty Tracking

### Create a specification for the Semantic Model Persistence Repository

Switch to `Agent` mode:

```md
/create_specification for the current implementation of the Semantic Model in the #file:GenAIDBExplorer.Core.
```

Switch to `/expert_dotnet_software_engineer` chat mode:

```md
How could the implementation of this specification be improved to meet best practices?
```

Switch to `/expert_dotnet_software_engineer` chat mode:

```md
Review the #file:data-semantic-model-persistence-repository.md  making any note of performance problems, especially as we increase the number of entities? List these as a table containing:
- Issue description
- Recommendation to resolve
- Priority (High, Medium, Low)
- Diffiulty to implement (High, Medium, Low)
- Benefit of resolving the issue
- Which engineering discipline recommended it.
- Include diagrams if appropriate to demonstrating the issue.
```

Switch to `janitor` chat mode:

```md
Hey, what janitorial work is needed for the semantic repository?
```

```md
Can you read the #file:spec-data-semantic-model-repository.md and review your janitorial recommendations against that and make sure you're not recommending to remove something that is in the spec?
```

### Identify performance issues and suggest improvements

```md
What performance issues might be faced with the current implementation of the specification of #file:data-semantic-model-persistence-repository.md ? Put the answer in a table and include suggestions for improving performance of the specification.
```

### Add Lazy Loading and Dirty Tracking Requirements to the specification

```md
I'd like to implement Lazy loading so that schema items are only completely loaded when they are acccessed and marking schema items as dirty, so that only changed items need to be saved. Please update the #file:data-semantic-model-persistence-repository.md specification with this requirement.
```

### Identify missing implementations in the current code

```md
Compare the #file:data-semantic-model-persistence-repository.md specification with the current implementation of the #file:SchemaRepository.cs and #file:SemanticModelProvider.cs and identify what is not implemented.
```

### Create the GitHub Issue for the feature request

```md
/create_github_issue_feature_from_spec based on the features that are not currently implemented in the SpecFile:#file:data-semantic-model-persistence-repository.md . Identify the tasks that need to be included based on what is missing from #file:SchemaRepository.cs and #file:SemanticModelProvider.cs. Use the #create_issue tool to create the issue in the GitHub repository using the #file:feature_request.yml template.
```

### Force the creation of the GitHub issue

```md
Go ahead and create that as an GitHub issue using the #file:feature_request.yml issue template
```

```md
Check that the #file:main.bicep meets the specifications outlined in #file:infrastructure-deployment-bicep-avm.md. If it doesn't meet any of the requirements, list them in a table and identify why they don't meet the requirement and how to resolve it.
```

```md
Add a requirement to the #file:infrastructure-deployment-bicep-avm.md that requires that the main.bicepparam reads all values from environment variables named appropriately for az developer CLI and formatted upper case with underscore separating names. E.g.
param environmentName = readEnvironmentVariable('AZURE_ENV_NAME', 'azdtemp')
param location = readEnvironmentVariable('AZURE_LOCATION', 'EastUS2')
````

### Create a plan for the Semantic Model Persistence Repository updates

```md
/create_implementation_plan PlanPurpose:'Data Semantic Model Repository Updates' based on the plan that was just defined to implement the missing requirements. After each Phase of the plan, the application should still work correctly and no functionality should be not working - can you confirm this?
```

### Start executing the plan

Switch to `Agent` mode:

```md
Start executing the implementation plan in #file:upgrade-data-semantic-model-repository.md. Execute it phase by phase, ensuring that after each phase the application still works correctly and no functionality is broken. If you have any questions or need clarification on any part of the plan, ask before proceeding. Make sure to update the implementation plan file with the progress and any changes made during the execution.
```

## Part 6 - Create Tests

Switch to `Agent` chat mode:

```md
/suggest_awesome_github_copilot_prompts MSTests
```

```md
Install MSTest Best Practices prompt
```

```md
/csharp-mstest Review #file:spec-data-semantic-model-repository.md and identify any areas that need unit tests. Create a list of unit tests that should be created to ensure the functionality is tested.
```

```md
/create_plan for implementing the unit tests identified in the previous step.
```

```md
Create the tests in the implementation plan #file:...
```

## Part 7a - Infrastructure as Code Update using Custom Chat Modes

Switch to `azure_verified_modules_bicep` chat mode:

```md
Hey, do any of the Azure Verified Modules in the #file:main.bicep need updating? If so, what are details on the modules that need updating?
```

```md
Nah, can you use #create_issue and the #file:chore_request.yml template to create an issue for each of the modules that need an update? All good yeah?
```

## Part 7b - Infrastructure Deployment Bicep using Prompt File

Switch to `Agent` mode:

```md
/update_avm_modules_in_bicep in #file:main.bicep
```

## Part 8 - Admin Tasks

```md
Add a new GitHub Issue template based on the #file:'feature_request.yml', but instead it should be a chore_request.yml. It should be to collect issues that are related to general solution hygiene, chores and technical debt remediation like package updates, refactoring that don't specifically change the application. They might cover moving files around, refactoring code, updating GitHub actions pipelines, package updates or improving, adding test coverage. Add anything else you might think is relevant. The goal will be that this chore_request template will be used to create chores to assign to GitHub Copilot Coding Agents to do, so collecting appropriate information in the issue to ensure they can work effectively is critical.
```
