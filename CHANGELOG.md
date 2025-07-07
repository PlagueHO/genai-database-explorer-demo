# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed
- **Breaking Change**: Upgraded System.CommandLine package from 2.0.0-beta4.22272.1 to 2.0.0-beta5.25306.1
  - Updated all command handlers to use new API patterns
  - Replaced `IsRequired = true` with `Required = true` for options
  - Replaced `AddOption()` with `Options.Add()` for adding options to commands
  - Replaced `SetHandler()` with `SetAction()` for command handler methods
  - Updated handler signatures to use `ParseResult` parameter instead of individual parameters
  - Replaced `AddCommand()` with `Subcommands.Add()` for adding subcommands
  - Updated `ArgumentHelpName` property to `HelpName` for options
  - Added `using System.CommandLine.Parsing;` to all command handlers

### Technical Details
This upgrade addresses breaking changes introduced in System.CommandLine 2.0.0-beta5 while maintaining 
full backward compatibility for CLI users. All commands continue to work exactly as before.

**Migration Reference**: [System.CommandLine 2.0.0-beta5 Migration Guide](https://learn.microsoft.com/en-us/dotnet/standard/commandline/migration-guide-2.0.0-beta5)

**Affected Command Handlers**:
- InitProjectCommandHandler
- ExtractModelCommandHandler  
- DataDictionaryCommandHandler
- EnrichModelCommandHandler
- ExportModelCommandHandler
- QueryModelCommandHandler
- ShowObjectCommandHandler

All handlers now use the new `ParseResult`-based action pattern for improved performance and consistency.