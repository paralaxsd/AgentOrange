// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable UnusedMember.Global
namespace AgentOrange.Core;

public enum LlmProvider { Google, OpenAI, Azure, Claude, Copilot }

public record AgentChatConfig(
    LlmProvider Provider,
    string ModelName,
    string ApiKey,
    string SystemPrompt,
    string? CopilotToken = null // Optional: Copilot-specific token
);
