using AgentOrange.ChatSession;

namespace AgentOrange;

sealed class AgentOrangeApp(AgentChatConfig config) : IAsyncDisposable
{
    /******************************************************************************************
     * FIELDS
     * ***************************************************************************************/
    readonly AgentChatConfig _config = config;

    IAgentChatSession? _session;

    /******************************************************************************************
     * METHODS
     * ***************************************************************************************/
    public async Task RunAsync()
    {
        await PreconditionChecker.ValidateOrThrowAsync();

        var loop = await CreateChatSessionLoopAsync();
        await loop.RunAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_session is { })
            await _session.DisposeAsync();
    }

    async Task<AgentChatSessionLoop> CreateChatSessionLoopAsync()
    {
        var ui = new AgentOrangeConsoleUi();
        _session = await AgentChatSessionFactory.CreateSessionFromAsync(_config);

        return new(ui, _session);
    }
}
