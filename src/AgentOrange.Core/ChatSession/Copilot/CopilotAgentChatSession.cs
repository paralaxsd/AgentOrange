using AgentOrange.Core.Skills;
using AgentOrange.Core.TokenUsage;
using GitHub.Copilot.SDK;
using Microsoft.Extensions.AI;

namespace AgentOrange.Core.ChatSession.Copilot;

sealed class CopilotAgentChatSession(
    CopilotClient modelClient, IChatClient chatClient, AgentSkills skills, AgentChatConfig config)
    : AgentChatSession<CopilotClient>(
        modelClient, chatClient, new FallbackTokenUsageProvider(), skills, config)
{
    /******************************************************************************************
     * METHODS
     * ***************************************************************************************/
    public override Task<ModelInfo?> GetModelInfoAsync() =>
        ModelClient.FindModelInfoAsync(Config.ModelName);
}