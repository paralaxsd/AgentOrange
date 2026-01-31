using AgentOrange.TokenUsage;
using Google.GenAI;
using Microsoft.Extensions.AI;

namespace AgentOrange.ChatSession.Google;

sealed class GoogleAgentChatSessionFactory : IAgentChatSessionFactory
{
    public async Task<IAgentChatSession> CreateSessionFromAsync(AgentChatConfig config)
    {
        var googleClient = new Client(apiKey: config.ApiKey);
        var chatClient = googleClient.AsIChatClient(config.ModelName);
        var tokenProvider = new GoogleTokenUsageProvider(googleClient, config.ModelName);
        var session = new GoogleAgentChatSession(config, googleClient, chatClient, tokenProvider);
        await session.InitializeHistoryAsync(config);

        return session;
    }
}
