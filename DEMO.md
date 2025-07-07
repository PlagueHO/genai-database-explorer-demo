# Demo steps

## Part 1 - Customize Copilot

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

## Part 2 - Upgrade Package with a Plan

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

## Part 3 - Adding Lazy Loading and Dirty Tracking

### Create a specification for the Semantic Model Persistence Repository

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

## Part 4 - Create Tests

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

## Part 5a - Infrastructure as Code Update using Custom Chat Modes

Switch to `azure_verified_modules_bicep` chat mode:

```md
Hey, do any of the Azure Verified Modules in the #file:main.bicep need updating? If so, what are details on the modules that need updating?
```

```md
Nah, can you use #create_issue and the #file:chore_request.yml template to create an issue for each of the modules that need an update? All good yeah?
```

## Part 5b - Infrastructure Deployment Bicep using Prompt File

Switch to `Agent` mode:

```md
/update_avm_modules_in_bicep in #file:main.bicep
```

## Part 6 - Admin Tasks

```md
Add a new GitHub Issue template based on the #file:'feature_request.yml', but instead it should be a chore_request.yml. It should be to collect issues that are related to general solution hygiene, chores and technical debt remediation like package updates, refactoring that don't specifically change the application. They might cover moving files around, refactoring code, updating GitHub actions pipelines, package updates or improving, adding test coverage. Add anything else you might think is relevant. The goal will be that this chore_request template will be used to create chores to assign to GitHub Copilot Coding Agents to do, so collecting appropriate information in the issue to ensure they can work effectively is critical.
```
