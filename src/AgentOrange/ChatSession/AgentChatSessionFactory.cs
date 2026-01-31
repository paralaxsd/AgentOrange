using AgentOrange.ChatSession.Copilot;
using AgentOrange.ChatSession.Google;

namespace AgentOrange.ChatSession;

static class AgentChatSessionFactory
{
    public static async Task<IAgentChatSession> CreateSessionFromAsync(AgentChatConfig config)
    {
        IAgentChatSessionFactory factory = config.Provider switch
        {
            LlmProvider.Google => new GoogleAgentChatSessionFactory(),
            LlmProvider.Copilot => new CopilotAgentChatSessionFactory(),
            _ => throw new NotSupportedException($"Provider {config.Provider} not supported.")
        };
        
        return await factory.CreateSessionFromAsync(config);
    }
}
