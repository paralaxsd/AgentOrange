// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable UnusedMember.Global

using System.Diagnostics.CodeAnalysis;

namespace AgentOrange.Core;

public enum LlmProvider { Google, OpenAI, Azure, Claude, Copilot }

public class AgentChatConfig
{
    /******************************************************************************************
     * PROPERTIES
     * ***************************************************************************************/
    public required LlmProvider Provider { get; init; }
    public required string ModelName { get; init; }
    public required string ApiKey { get; init; }
    public required string SystemPrompt { get; init; } = Constants.DefaultSystemPrompt;
    public string? CopilotToken { get; init; }

    /******************************************************************************************
     * STRUCTORS
     * ***************************************************************************************/
    public AgentChatConfig() { }

    [SetsRequiredMembers]
    public AgentChatConfig(LlmProvider provider,
        string modelName,
        string apiKey,
        string systemPrompt,
        string? copilotToken = null)
    {
        Provider = provider;
        ModelName = modelName;
        ApiKey = apiKey;
        SystemPrompt = systemPrompt;
        CopilotToken = copilotToken;
    }
}
