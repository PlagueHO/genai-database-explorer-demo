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
        }        
    ]
}
```

## Part 2 - Building a Specification

```markdown
/create_spec SpecPurpose:Persisting the Semantic Model for a Database Schema for the Generative AI database explorer using a repository pattern. Define the specification requirements based on the current implementation in the #file:SchemaRepository.cs and #file:SemanticModelProvider.cs. Ensure you refer to the relevant interfaces.
```

Check that the #file:main.bicep meets the specifications outlined in #file:infrastructure-deployment-bicep-avm.md. If it doesn't meet any of the requirements, list them in a table and identify why they don't meet the requirement and how to resolve it.

Add a requirement to the #file:infrastructure-deployment-bicep-avm.md that requires that the main.bicepparam reads all values from environment variables named appropriately for az developer CLI and formatted upper case with underscore separating names. E.g.
param environmentName = readEnvironmentVariable('AZURE_ENV_NAME', 'azdtemp')
param location = readEnvironmentVariable('AZURE_LOCATION', 'EastUS2')


Add a new GitHub Issue template based on the #file:'feature_request.yml', but instead it should be a chore_request.yml. It should be to collect issues that are related to general solution hygiene, chores and technical debt remediation like package updates, refactoring that don't specifically change the application. They might cover moving files around, refactoring code, updating GitHub actions pipelines, package updates or improving, adding test coverage. Add anything else you might think is relevant. The goal will be that this chore_request template will be used to create chores to assign to GitHub Copilot Coding Agents to do, so collecting appropriate information in the issue to ensure they can work effectively is critical.