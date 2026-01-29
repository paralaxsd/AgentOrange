using AgentOrange.TokenUsage;
using Microsoft.Extensions.AI;

namespace AgentOrange.ChatSession;

public interface IAgentChatSession : IAsyncDisposable
{
    IChatClient ChatClient { get; }
    IAgentTokenUsageProvider TokenUsageProvider { get; }
    Task<ModelInfo?> GetModelInfoAsync();
}

public sealed record ModelInfo(
    string DisplayName, 
    int? InputTokenLimit, 
    int? OutputTokenLimit);