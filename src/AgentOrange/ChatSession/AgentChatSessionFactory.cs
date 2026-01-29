namespace AgentOrange.ChatSession;

static class AgentChatSessionFactory
{
    public static async Task<IAgentChatSession> CreateSessionFromAsync(AgentChatConfig config) =>
        config.Provider switch
        {
            LlmProvider.Google => await new GoogleAgentChatSessionFactory().CreateSessionFromAsync(config),
            _ => throw new NotSupportedException($"Provider {config.Provider} not supported.")
        };
}
