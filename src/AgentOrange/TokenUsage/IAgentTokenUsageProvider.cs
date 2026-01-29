using Microsoft.Extensions.AI;

namespace AgentOrange.TokenUsage;

public interface IAgentTokenUsageProvider
{
    Task<TokenUsageInfo> GetTokenUsageAsync(
        List<ChatMessage> history, string userInput);
}

public sealed record TokenUsageInfo(
    int? InputTokens,
    int? OutputTokens,
    int? TotalTokens)
{
    public static TokenUsageInfo Empty => new(null, null, null);
}
