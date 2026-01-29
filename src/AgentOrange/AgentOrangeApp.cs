using AgentOrange.ChatSession;
using AgentOrange.Skills;
using Microsoft.Extensions.AI;
using System.ComponentModel;
using System.Reflection;
using AgentOrange.Core.Extensions;

namespace AgentOrange;

sealed class AgentOrangeApp(AgentChatConfig config) : IAsyncDisposable
{
    /******************************************************************************************
     * FIELDS
     * ***************************************************************************************/
    readonly AgentChatConfig _config = config;
    readonly List<ChatMessage> _history = [];

    IAgentChatSession? _session;
    IAgentOrangeUi? _ui;
    IChatClient? _chatClient;

    /******************************************************************************************
     * METHODS
     * ***************************************************************************************/
    public async Task RunAsync()
    {
        await PreconditionChecker.ValidateOrThrowAsync();
        await InitializeSessionAsync();
        await new AgentChatSessionLoop().RunAsync(
            _ui.NotNull(), _session.NotNull(), _history, _chatClient.NotNull());
    }

    public async ValueTask DisposeAsync()
    {
        if (_session is { })
            await _session.DisposeAsync();
    }

    async Task InitializeSessionAsync()
    {
        _ui = new AgentOrangeConsoleUi();
        _session = await AgentChatSessionFactory.CreateSessionFromAsync(_config);
        await InitializeSystemPromptAsync();

        var baseClient = _session.ChatClient;
        using var skills = new AgentSkills(baseClient);
        using var summarizerClient = baseClient.AsBuilder().Build();
        _chatClient = baseClient.AsBuilder()
            .ConfigureOptions(opts =>
            {
                opts.AllowMultipleToolCalls = true;
                // ReSharper disable once AccessToDisposedClosure
                opts.Tools = [.. CreateToolsFromSkillset(skills)];
            })
            .UseFunctionInvocation()
            .Build();
        skills.ToolEnabledClient = _chatClient;
    }

    async Task InitializeSystemPromptAsync()
    {
        if (_session is not { })
            throw new InvalidOperationException("Chat session not initialized");

        var modelInfo = await _session.GetModelInfoAsync();
        var inputTokenLimit = modelInfo?.InputTokenLimit ?? 8192;
        var outputTokenLimit = modelInfo?.OutputTokenLimit ?? 8192;
        var displayName = modelInfo?.DisplayName ?? _config.ModelName;
        var systemPrompt =
            $"""
            Du bist ein professioneller KI-Assistent mit Fokus auf Softwareentwicklung, Teamarbeit und technische Kommunikation.
            Dein Modell ist '{displayName}' mit einem Input-Token-Limit von {inputTokenLimit} und einem Output-Token-Limit von {outputTokenLimit}.
            Behalte bei jeder Antwort beide Limits im Blick und fasse dich bei langen Konversationen oder großen Kontexten möglichst prägnant, um Kontextverluste zu vermeiden.
            Nach jeder Antwort erhältst du einen Meta-Block mit aktueller Zeit und Usage-Details, die du für die Optimierung deiner nächsten Antwort nutzen sollst.
            Antworte stets hilfreich, präzise und auf Augenhöhe mit erfahrenen Entwickler:innen.
            Wenn du Code generierst, halte dich an moderne, teamtaugliche Standards und erkläre deine Vorschläge bei Bedarf kurz und verständlich.

            Wichtige Regeln:
            - Überschreite niemals das Input- oder Output-Token-Limit, auch nicht bei längeren Antworten.
            - Nutze den Meta-Block aktiv, um deine Antwortlänge und Detailtiefe zu steuern.
            - Sei proaktiv, freundlich und lösungsorientiert.
            - Fokussiere dich auf Softwareentwicklung, Architektur, Best Practices und Teamwork.
            """;
        _history.Add(new(ChatRole.System, systemPrompt));
    }

    static IEnumerable<AIFunction> CreateToolsFromSkillset(object target)
        => target.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<DescriptionAttribute>() is { })
            .Select(m => AIFunctionFactory.Create(m, target));
}
