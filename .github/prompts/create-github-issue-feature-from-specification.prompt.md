---
mode: 'agent'
description: 'Create GitHub Issue for feature request from specification file using feature_request.yml template.'
tools: ['codebase', 'search', 'github', 'create_issue', 'search_issues', 'update_issue']
---
# Create GitHub Issue from Specification

Create a GitHub Issue for a feature request based on the requirements and features described in the specification file `${file}`.

## Process

1. Analyze the specification file to identify unimplemented or missing features
2. Check existing issues using `search_issues`
3. Create a new issue for each missing feature using `create_issue` and the `feature_request.yml` template
4. Use clear, structured titles and descriptions for each issue
5. Include only changes required by the specification
6. Verify against existing issues before creation

## Issue Content

- Title: Feature or requirement name from the specification
- Description: Details, requirements, and context from the specification
- Labels: Appropriate for feature requests
