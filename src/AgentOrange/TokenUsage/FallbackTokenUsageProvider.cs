using Microsoft.Extensions.AI;

namespace AgentOrange.TokenUsage;

sealed class FallbackTokenUsageProvider : IAgentTokenUsageProvider
{
    public Task<TokenUsageInfo> GetTokenUsageAsync(IList<ChatMessage> history, string? userInput = null)
    {
        var lastAssistant = history.LastOrDefault(m => m.Role == ChatRole.Assistant);
        var usage = lastAssistant?.Contents.OfType<UsageContent>().FirstOrDefault()?.Details;
        var input = usage?.InputTokenCount is { } l1 ? (int?)l1 : null;
        var output = usage?.OutputTokenCount is { } l2 ? (int?)l2 : null;
        var total = usage?.TotalTokenCount is { } l3 ? (int?)l3 : null;
        return Task.FromResult(new TokenUsageInfo(input, output, total));
    }
}
