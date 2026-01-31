using AgentOrange.TokenUsage;
using GitHub.Copilot.SDK;
using Microsoft.Extensions.AI;

namespace AgentOrange.ChatSession.Copilot;

sealed class CopilotAgentChatSession(CopilotClient modelClient, IChatClient chatClient)
    : AgentChatSession<CopilotClient>(modelClient, chatClient, new FallbackTokenUsageProvider())
{
    /******************************************************************************************
     * METHODS
     * ***************************************************************************************/
    public override Task<ModelInfo?> GetModelInfoAsync() => 
        Task.FromResult<ModelInfo?>(null);
}