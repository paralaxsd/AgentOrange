using AgentOrange.ChatSession;
using Microsoft.Extensions.AI;

namespace AgentOrange.Core.Extensions;

static class AgentChatSessionExtensions
{
    public static void AddToHistory(this IAgentChatSession session, ChatRole role, string? content) => 
        session.History.Add(new ChatMessage(role, content));
}