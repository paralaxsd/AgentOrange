using AgentOrange.TokenUsage;
using Microsoft.Extensions.AI;

namespace AgentOrange.ChatSession;

public sealed class AgentChatSession(
    IChatClient chatClient,
    IAgentTokenUsageProvider tokenUsageProvider,
    IAsyncDisposable disposable,
    Func<Task<ModelInfo?>> getModelInfo)
    : IAgentChatSession
{
    /******************************************************************************************
     * FIELDS
     * ***************************************************************************************/
    readonly IAsyncDisposable _disposable = disposable;
    readonly Func<Task<ModelInfo?>> _getModelInfo = getModelInfo;

    /******************************************************************************************
     * PROPERTIES
     * ***************************************************************************************/
    public IChatClient ChatClient { get; } = chatClient;
    public IAgentTokenUsageProvider TokenUsageProvider { get; } = tokenUsageProvider;

    /******************************************************************************************
     * METHODS
     * ***************************************************************************************/
    public async ValueTask DisposeAsync() => await _disposable.DisposeAsync();
    public Task<ModelInfo?> GetModelInfoAsync() => _getModelInfo();
}

interface IAgentChatSessionFactory
{
    Task<IAgentChatSession> CreateSessionFromAsync(AgentChatConfig config);
}
