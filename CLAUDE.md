# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

This is a **.NET 10** solution using the [Nuke](https://nuke.build/) build system.

```bash
# Build (default target is Compile, which depends on Restore)
nuke
nuke Compile
nuke Clean     # Cleans bin/obj directories

# NuGet packaging
nuke PackNuget      # Packs AgentOrange.Core as NuGet package (output: artifacts/)
nuke PublishNuget   # Publishes to GitHub Packages (requires NuGetApiKey parameter)

# Run console app
dotnet run --project src/AgentOrange.Console/AgentOrange.Console.csproj

# Run web app
dotnet run --project src/AgentOrange.Web/AgentOrange.Web.csproj
```

There is no test project yet.

### NuGet Publishing

The Core library is packaged as a NuGet package with:
- Source Link support for debugging into library code
- Symbol package (.snupkg) with embedded sources
- Automatic publishing to GitHub Packages on tag push (`v*`)
- GitHub Actions workflow: `.github/workflows/publish_nuget.yml`

## Architecture

Three-project solution (`AgentOrange.slnx`):

- **AgentOrange.Core** — Provider-agnostic library: chat sessions, skills, token usage, extensions
- **AgentOrange.Console** — Console frontend (Spectre.Console UI, interactive REPL loop)
- **AgentOrange.Web** — Blazor Server frontend (vector store, semantic search, data ingestion)

### Provider Abstraction

LLM providers are pluggable via a factory pattern:

```
IAgentChatSessionFactory → AgentChatSessionFactoryBase (abstract)
    ├── GoogleAgentChatSessionFactory
    └── CopilotAgentChatSessionFactory

IAgentChatSession → AgentChatSession<TClient> (generic abstract)
    ├── GoogleAgentChatSession
    └── CopilotAgentChatSession
```

`AgentChatSessionFactory` is a static router that selects the provider based on `AgentChatConfig.Provider` (enum: `Google`, `OpenAI`, `Azure`, `Claude`, `Copilot`).

### Skills System

Skills are modular partial classes on `AgentSkills`, split by domain:
- `AgentSkills.cs` — Core (math, sub-agents: Subcontract, EstablishWorkingGroup)
- `AgentSkills.Web.cs` — FetchUrl, SummarizeUrl
- `AgentSkills.Filesystem.cs` — ListDirectory, ReadFile, WriteFile, etc.
- `AgentSkills.Code.cs` — ExecuteSafeCSharp (Roslyn sandbox)
- `AgentSkills.Process.cs` — RunProcess
- `AgentSkills.Console.cs` — Spectre markup rendering

Methods are auto-registered as LLM tools via `AIFunctionFactory.Create()`.

### Token-Based History Pruning

`HistoryPruningExtensions.PruneAsync()` monitors token usage via `IAgentTokenUsageProvider` and removes oldest messages (preserving the system prompt at index 0) to keep history at ~70% of the model's input token limit.

### Resource Management

The web app uses `ResourceDisposer` (singleton) as a central registry for `IAsyncDisposable` resources with thread-safe disposal via `SemaphoreSlim`.

### Configuration

**Console:** Environment variables — `AO_LLM_PROVIDER`, `AO_MODEL_NAME`, `AO_SYSTEM_PROMPT`, `GEMINI_API_KEY`

**Web:** `appsettings.json` with `AgentChatConfig` section (Provider, ModelName). GitHub token via user secrets (`GitHubModels:Token`).

## C# Style Rules (Non-Negotiable)

Refer to `.github/copilot-instructions.md` for the full style guide. Key rules:

- **100-character line limit**
- **`sealed` by default** — unseal only with clear inheritance intent
- **`record` over `class`** for data-carrying types with value semantics
- **`var` always** for local variables; targeted `new()` for instantiations
- **No explicit `private`/`internal`** — they are the defaults, writing them is noise
- **File-scoped namespaces**, primary constructors, pattern matching, collection expressions `[]`
- **Expression-bodied members** for single-expression methods/properties
- **Member ordering**: Fields → Properties → Constructors → Methods, each sorted by visibility (public → internal → protected → private), static after instance
- **Field naming**: `_camelCase` for instance fields, `PascalCase` for static/protected fields
- **Early returns** to reduce cyclomatic complexity
- **Functional expressions over loops** where readability is preserved
- **Strategic blank lines** to visually separate logical blocks
- **No regions** — use partial classes instead
- **Minimize negations** in conditionals
- Prefer extension types for utilities; use existing extensions (`StringExtensions`, `EnumerableExtensions`, etc.) before writing new helpers
- SOLID, DRY, YAGNI — do not over-engineer

## Chat Language

Chat/comments with the user in German (first-name basis). Code and identifiers in US-English.
