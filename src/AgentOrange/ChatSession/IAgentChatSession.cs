using AgentOrange.Skills;
using AgentOrange.TokenUsage;
using Microsoft.Extensions.AI;

namespace AgentOrange.ChatSession;

public interface IAgentChatSession : IAsyncDisposable
{
    IChatClient ChatClient { get; }
    IAgentTokenUsageProvider TokenUsageProvider { get; }
    AgentSkills Skills { get; }
    List<ChatMessage> History { get; }
    AgentChatConfig Config { get; }
    Task<ModelInfo?> GetModelInfoAsync();
}

/// <param name="DisplayName"></param>
/// <param name="InputTokenLimit">
/// The maximum number of input tokens that the model can handle.
/// </param>
/// <param name="OutputTokenLimit">
/// The maximum number of output tokens that the model can generate.
/// </param>
public sealed record ModelInfo(
    string DisplayName,
    int? InputTokenLimit,
    int? OutputTokenLimit);