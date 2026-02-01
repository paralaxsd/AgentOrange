using AgentOrange.Skills;
using AgentOrange.TokenUsage;
using GitHub.Copilot.SDK;
using Microsoft.Extensions.AI;

namespace AgentOrange.ChatSession.Copilot;

sealed class CopilotAgentChatSession(
    CopilotClient modelClient, IChatClient chatClient, AgentSkills skills)
    : AgentChatSession<CopilotClient>(
        modelClient, chatClient, new FallbackTokenUsageProvider(), skills)
{
    /******************************************************************************************
     * METHODS
     * ***************************************************************************************/
    public override Task<ModelInfo?> GetModelInfoAsync() => 
        Task.FromResult<ModelInfo?>(null);
}