using AgentOrange.Core.Extensions;
using AgentOrange.Skills;
using AgentOrange.TokenUsage;
using Google.GenAI;
using Microsoft.Extensions.AI;

namespace AgentOrange.ChatSession.Google;

sealed class GoogleAgentChatSession(
    AgentChatConfig config, Client modelClient,
    IChatClient chatClient, IAgentTokenUsageProvider usageProvider, AgentSkills skills)
    : AgentChatSession<Client>(modelClient, chatClient, usageProvider, skills, config)
{
    /******************************************************************************************
     * METHODS
     * ***************************************************************************************/
    internal async Task InitializeHistoryAsync(AgentChatConfig config)
    {
        var modelInfo = await GetModelInfoAsync();
        var systemPrompt = Utils.CreateSystemPromptFrom(config, modelInfo);
        this.AddToHistory(ChatRole.System, systemPrompt);
    }

    public override async Task<ModelInfo?> GetModelInfoAsync()
    {
        var model = await ModelClient.Models.GetAsync(Config.ModelName);
        var displayName = model.DisplayName ?? $"Unknown Google Model <{Config.ModelName}>";
        return new(displayName, model.InputTokenLimit, model.OutputTokenLimit);
    }
}