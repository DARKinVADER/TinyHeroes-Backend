# Design: Install dotnet/skills Marketplace Plugins

Design specification for installing the official `.NET` skills marketplace plugins into the Antigravity agent.

## Goals
- Make all 15 plugins from the `dotnet/skills` marketplace repository available as active plugins in the Antigravity agent.
- Ensure the agent has procedural knowledge for `.NET` tasks (like unit testing, MSBuild, project upgrades, etc.).

## Proposed Approach
We will copy the cloned plugins directly into the active configuration directory of the Antigravity agent.

### Source Path
`/Users/I564521/.gemini/antigravity-ide/scratch/dotnet-skills/plugins/`

### Destination Path
`/Users/I564521/.gemini/config/plugins/`

### Plugins to Install
1. `dotnet`
2. `dotnet-ai`
3. `dotnet-aspnetcore`
4. `dotnet-blazor`
5. `dotnet-data`
6. `dotnet-diag`
7. `dotnet-experimental`
8. `dotnet-maui`
9. `dotnet-msbuild`
10. `dotnet-nuget`
11. `dotnet-template-engine`
12. `dotnet-test`
13. `dotnet-test-migration`
14. `dotnet-upgrade`
15. `dotnet11`

## Verification Plan
1. Check that each plugin folder was copied successfully.
2. Confirm files like `plugin.json` and the corresponding `skills/` folders are present in `/Users/I564521/.gemini/config/plugins/`.
