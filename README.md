# AgentOrange [![compile](https://github.com/paralaxsd/AgentOrange/actions/workflows/compile.yml/badge.svg)](https://github.com/paralaxsd/AgentOrange/actions/workflows/compile.yml)

**AgentOrange** is a modern, modular LLM agent for .NET 10, designed with a focus on clean code, portability, and extensibility.

## Features

- Provider-agnostic architecture (Google Gemini, GitHub Copilot, with OpenAI/Azure/Claude possible)
- UI-independent core with console and web frontends
- Configurable system prompt via `AgentChatConfig`
- Modular skills/tools with sub-agent support (Subcontract, EstablishWorkingGroup)
- Token-based, robust history pruning
- Modern C# features, ReSharper-green
- [Nuke](https://nuke.build/) build integration

## Projects

| Project | Description |
|---|---|
| `AgentOrange.Core` | Provider-agnostic core: chat sessions, skills, token usage |
| `AgentOrange.Console` | Console frontend with Spectre.Console UI |
| `AgentOrange.Web` | Web frontend (ASP.NET Core) |

## Build & Run

- **Requirements:** .NET 10 SDK, [Nuke](https://nuke.build/) build system
- **Build:**
  ```
  nuke
  ```
- **Run (Console):**
  ```
  dotnet run --project src/AgentOrange.Console/AgentOrange.Console.csproj
  ```

## Architecture

- **AgentOrangeApp:** Orchestrates initialization, precondition checks, session, and loop
- **AgentChatSessionLoop:** Encapsulates the chat interaction, UI-independent
- **AgentChatSessionFactory:** Per-provider factory (Google, Copilot) creating sessions with tools and sub-agent support
- **Skills:** Modular partial classes (`AgentSkills.*.cs`) covering math, web, filesystem, process execution, code sandboxing, console rendering, and sub-agent delegation
- **TokenUsageProvider:** Provider- and model-agnostic token handling
- **PreconditionChecker:** Checks external dependencies (e.g., dotnet CLI)

## Skills

| Skill | Description |
|---|---|
| Add, Multiply, Divide | Basic arithmetic |
| GetCurrentTime | Current timestamp |
| Subcontract | Delegates a task to a sub-agent with its own session |
| EstablishWorkingGroup | Parallel multi-role sub-agent delegation with executive summary |
| FetchUrl, SummarizeUrl | Raw URL fetch and LLM-powered summarization |
| RunCode | C# code execution in an isolated sandbox |
| RunProcess | Shell/process execution |
| ListDirectory, ReadFile, WriteFile, DeleteFile, SearchFiles, GetFileInfo | Filesystem operations |
| RenderMarkup, RenderMarkupLine | Spectre.Console markup rendering |

## Code Style

- Modern C# features (file-scoped, records, var, pattern matching, etc.)
- ReSharper-green, no unnecessary namespaces
- Logical blocks for fields/methods, no regions
- Minimal negations, strategic blank lines
- See `.github/copilot-instructions.md` for details
