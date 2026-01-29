using AgentOrange.TokenUsage;
using Microsoft.Extensions.AI;

namespace AgentOrange.ChatSession;

sealed class GoogleAgentChatSessionFactory : IAgentChatSessionFactory
{
    public Task<IAgentChatSession> CreateSessionFromAsync(AgentChatConfig config)
    {
        var googleClient = new Google.GenAI.Client(apiKey: config.ApiKey);
        var chatClient = googleClient.AsIChatClient(config.ModelName);
        var tokenProvider = new GoogleTokenUsageProvider(googleClient, config.ModelName);
        var session = new AgentChatSession(chatClient, tokenProvider, googleClient, GetModelInfo);

        return Task.FromResult<IAgentChatSession>(session);

        async Task<ModelInfo?> GetModelInfo()
        {
            var model = await googleClient.Models.GetAsync(config.ModelName);
            var displayName = model.DisplayName ?? "Unknown Google Model";

            return new(displayName, model.InputTokenLimit, model.OutputTokenLimit);
        }
    }
}
