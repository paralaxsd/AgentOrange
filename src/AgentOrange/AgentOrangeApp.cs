using AgentOrange.ChatSession;
using Microsoft.Extensions.AI;
using System.ComponentModel;
using System.Reflection;

namespace AgentOrange;

sealed class AgentOrangeApp(AgentChatConfig config) : IAsyncDisposable
{
    /******************************************************************************************
     * FIELDS
     * ***************************************************************************************/
    readonly AgentChatConfig _config = config;

    IAgentChatSession? _session;
    IChatClient? _chatClient;

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

        var baseClient = _session.ChatClient;
        var skills = _session.Skills;

        _chatClient = baseClient.AsBuilder()
            .ConfigureOptions(opts =>
            {
                opts.AllowMultipleToolCalls = true;
                opts.Tools = [.. CreateToolsFromSkillSet(skills)];
            })
            .UseFunctionInvocation()
            .Build();
        skills.ToolEnabledClient = _chatClient;

        return new(ui, _session, _chatClient);
    }
    
    static IEnumerable<AIFunction> CreateToolsFromSkillSet(object target)
        => target.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<DescriptionAttribute>() is { })
            .Select(m => AIFunctionFactory.Create(m, target));
}
