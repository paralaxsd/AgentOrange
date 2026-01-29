using AgentOrange.ChatSession;
using Microsoft.Extensions.AI;

namespace AgentOrange.Core.Extensions;

static class HistoryPruningExtensions
{
    public static async Task PruneAsync(this IList<ChatMessage> history, IAgentChatSession session)
    {
        if (session?.TokenUsageProvider is not { } provider)
            return;
        var modelInfo = await session.GetModelInfoAsync();
        var limit = (modelInfo?.InputTokenLimit ?? 8192) * 0.7;
        while (true)
        {
            var usage = await provider.GetTokenUsageAsync(history);
            if (usage.TotalTokens <= limit || history.Count <= 2)
                break;

            history.RemoveAt(1); // System-Prompt bleibt immer erhalten
        }
    }
}
