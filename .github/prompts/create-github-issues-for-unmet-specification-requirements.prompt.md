---
mode: 'agent'
description: 'Create GitHub Issues for unimplemented requirements from specification files using feature_request.yml template.'
tools: ['codebase', 'search', 'github', 'create_issue', 'search_issues', 'update_issue']
---
# Create GitHub Issues for Unmet Specification Requirements

Analyze the specification file `${file}` and compare it to the current implementation to identify requirements that are not yet implemented. For each unmet requirement, create a GitHub Issue using the `feature_request.yml` template.

## Process

1. Parse the specification file to extract all requirements
2. Compare requirements to the current codebase
3. For each unmet requirement, check for an existing issue using `search_issues`
4. If no issue exists, create a new issue using `create_issue` and the `feature_request.yml` template
5. Use clear, structured titles and descriptions for each issue
6. Label issues appropriately

## Issue Content

- Title: Requirement name or summary
- Description: Details and context from the specification
- Labels: Feature, Spec, Unmet Requirement
