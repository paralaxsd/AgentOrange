using Microsoft.Extensions.AI;

namespace AgentOrange.TokenUsage;

record TokenUsageInfo(int? InputTokens, int? OutputTokens, int? TotalTokens);

interface IAgentTokenUsageProvider
{
    Task<TokenUsageInfo> GetTokenUsageAsync(
        List<ChatMessage> history, string userInput);
}
