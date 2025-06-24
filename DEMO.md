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
    "servers": {
        "filesystem": {
            "command": "npx",
            "args": [
                "-y",
                "@modelcontextprotocol/server-filesystem",
                "${input:baseDirectory}"
            ],
            "type": "stdio",
            "env": {}
        },
        "playwright": {
            "command": "npx",
            "args": [
                "-y",
                "@playwright/mcp@latest"
            ],
            "type": "stdio",
            "env": {}
        },
        "github": {
            "command": "npx",
            "args": [
                "-y",
                "@modelcontextprotocol/server-github"
            ],
            "env": {
                "GITHUB_PERSONAL_ACCESS_TOKEN": "${input:github_token}"
            }
        },
        "giphy": {
            "command": "npx",
            "args": [
                "-y",
                "mcp-server-giphy"
            ],
            "env": {
                "GIPHY_API_KEY": "${input:giphy_api_key}"
            }
        }
    },
    "inputs": [
        {
            "id": "baseDirectory",
            "type": "promptString",
            "description": "Enter the base directory path for the server"
        },
        {
            "id": "github_token",
            "type": "promptString",
            "description": "GitHub Personal Access Token",
            "password": true
        },
        {
            "id": "giphy_api_key",
            "type": "promptString",
            "description": "Giphy API Key",
            "password": true
        }
    ]
}
```

## Part 2a - Infrastructure as Code Update using Custom Chat Modes

Switch to `azure_verified_modules_bicep` chat mode:

```md
Hey, do any of the Azure Verified Modules in the #file:main.bicep need updating? If so, what are details on the modules that need updating?
```

```md
Nah, can you use #create_issue and the #file:chore_request.yml template to create an issue for each of the modules that need an update? All good yeah?
```

## Part 2b - Infrastructure Deployment Bicep using Prompt File

Switch to `Agent` mode:

```md
/update_avm_modules_in_bicep in #file:main.bicep
```

## Part 3 - Upgrade Package with a Plan

Switch to `plan` mode:

```md
Create a plan for updating the package System.CommandLine in #file:GenAIDBExplorer.Console from the current version in the project to 2.0.0-beta5.25306.1. Refer to the migration instructions in https://learn.microsoft.com/en-us/dotnet/standard/commandline/migration-guide-2.0.0-beta5
```

Switch to `Agent` mode:

```md
/create_github_issue_feature_from_plan PlanFile:#file:upgrade-system-commandline-beta5.md 
```

## Part 4 - Adding Lazy Loading and Dirty Tracking

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
/create_spec SpecPurpose:Persisting the Semantic Model for a Database Schema for the Generative AI database explorer using a repository pattern. Define the specification requirements based on the current implementation in the #file:SchemaRepository.cs and #file:SemanticModelProvider.cs. Ensure you refer to the relevant interfaces.
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

```md
Add a new GitHub Issue template based on the #file:'feature_request.yml', but instead it should be a chore_request.yml. It should be to collect issues that are related to general solution hygiene, chores and technical debt remediation like package updates, refactoring that don't specifically change the application. They might cover moving files around, refactoring code, updating GitHub actions pipelines, package updates or improving, adding test coverage. Add anything else you might think is relevant. The goal will be that this chore_request template will be used to create chores to assign to GitHub Copilot Coding Agents to do, so collecting appropriate information in the issue to ensure they can work effectively is critical.
```
