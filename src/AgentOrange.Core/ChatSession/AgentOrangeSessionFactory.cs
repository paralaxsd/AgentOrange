namespace AgentOrange.Core.ChatSession;

public sealed class AgentOrangeSessionFactory : IAgentChatSessionFactory
{
    public async Task<IAgentChatSession> CreateSessionFromAsync(AgentChatConfig config)
    {
        IAgentChatSessionFactory factory = config.Provider switch
        {
            LlmProvider.Google => new Google.GoogleAgentChatSessionFactory(),
            LlmProvider.Copilot => new Copilot.CopilotAgentChatSessionFactory(),
            _ => throw new NotSupportedException($"Provider {config.Provider} not supported.")
        };
        return await factory.CreateSessionFromAsync(config);
    }
}
