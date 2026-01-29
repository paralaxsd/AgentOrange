# AgentOrange

**AgentOrange** is a modern, modular LLM agent for .NET 10, designed with a focus on clean code, portability, and extensibility.

## Features

- Provider-agnostic architecture (Google, OpenAI, etc. possible)
- UI-independent core (easily adaptable to console, web, bot, etc.)
- Token-based, robust history pruning
- Modular skills/tools
- System prompt currently in code, configurable in the future
- Modern C# features, Resharper-green, Copilot-style
- [Nuke](https://nuke.build/) build integration

## Build & Run

- **Requirements:** .NET 10 SDK, [Nuke](https://nuke.build/) build system
- **Build:**
  ```
  nuke
  ```
- **Run:**
  ```
  dotnet run --project src/AgentOrange/AgentOrange.csproj
  ```

## Architecture

- **AgentOrangeApp:** Orchestrates initialization, precondition check, session, and loop
- **AgentChatSessionLoop:** Encapsulates the actual chat interaction, UI-independent
- **Skills:** Modular, extensible, clean code
- **TokenUsageProvider:** Provider- and model-agnostic token handling
- **PreconditionChecker:** Checks external dependencies (e.g., dotnet CLI)

## System Prompt

- Currently hardcoded in the app (see `AgentOrangeApp`)
- TODO: Make configurable (e.g., from file, database, or feature provider)

## Code Style

- Modern C# features (file-scoped, records, var, pattern matching, etc.)
- Resharper-green, no unnecessary namespaces
- Logical blocks for fields/methods, no regions
- Minimal negations, strategic blank lines
- See `.github/copilot-instructions.md` for details

## TODO / Roadmap

- Web UI (ASP.NET Core, Blazor)
- Configurable system prompt
- Additional providers (OpenAI, Azure, etc.)
- Extended precondition checks
- More skills/tools
