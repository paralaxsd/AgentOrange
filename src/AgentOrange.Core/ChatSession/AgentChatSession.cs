using AgentOrange.Core.Skills;
using AgentOrange.Core.TokenUsage;
using Microsoft.Extensions.AI;

namespace AgentOrange.Core.ChatSession;

/// <summary>
/// Central service for managing and disposing IAsyncDisposable resources in Blazor Server.
/// Ensures that registered resources are disposed exactly once, either when explicitly requested
/// or when the application/service is disposed (e.g. on circuit shutdown or app exit).
/// Use this to avoid resource leaks for long-lived objects (e.g. chat sessions, clients)
/// that are not directly managed by the Blazor component lifecycle.
/// </summary>
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

public interface IAgentChatSessionFactory
{
    Task<IAgentChatSession> CreateSessionFromAsync(AgentChatConfig config);
}
