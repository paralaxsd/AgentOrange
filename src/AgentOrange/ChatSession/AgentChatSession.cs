using AgentOrange.Skills;
using AgentOrange.TokenUsage;
using Microsoft.Extensions.AI;

namespace AgentOrange.ChatSession;

public abstract class AgentChatSession<TClient>(
    TClient modelClient, IChatClient chatClient, IAgentTokenUsageProvider tokenUsageProvider, 
    AgentSkills skills)
    : IAgentChatSession
    where TClient : IAsyncDisposable
{
    /******************************************************************************************
     * PROPERTIES
     * ***************************************************************************************/
    public TClient ModelClient { get; } = modelClient;
    public IChatClient ChatClient { get; } = chatClient;
    public IAgentTokenUsageProvider TokenUsageProvider { get; } = tokenUsageProvider;
    public AgentSkills Skills { get; } = skills;
    public List<ChatMessage> History { get; } = [];

    /******************************************************************************************
     * METHODS
     * ***************************************************************************************/
    public abstract Task<ModelInfo?> GetModelInfoAsync();

    public virtual async ValueTask DisposeAsync()
    {
        Skills.Dispose();
        ChatClient.Dispose();
        await ModelClient.DisposeAsync();
    }
}

interface IAgentChatSessionFactory
{
    Task<IAgentChatSession> CreateSessionFromAsync(AgentChatConfig config);
}
