using AgentOrange.Skills;
using AgentOrange.TokenUsage;
using Microsoft.Extensions.AI;

namespace AgentOrange.ChatSession;

public abstract class AgentChatSession<TClient>(
    TClient modelClient, IChatClient chatClient, IAgentTokenUsageProvider tokenUsageProvider, 
    AgentSkills skills, AgentChatConfig config)
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
    public AgentChatConfig Config { get; } = config;

    /******************************************************************************************
     * METHODS
     * ***************************************************************************************/
    public abstract Task<ModelInfo?> GetModelInfoAsync();

    public virtual async ValueTask DisposeAsync()
    {
        Skills.Dispose();
        if(ChatClient is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else
        {
            ChatClient.Dispose();
        }
        await ModelClient.DisposeAsync();
    }
}

interface IAgentChatSessionFactory
{
    Task<IAgentChatSession> CreateSessionFromAsync(AgentChatConfig config);
}
