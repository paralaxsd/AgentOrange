using AgentOrange.ChatSession;
using AgentOrange.Core.Extensions;
using AgentOrange.Core.ProcessHandling;
using AgentOrange.Skills;
using Microsoft.Extensions.AI;
using System.ComponentModel;
using System.Reflection;

// ReSharper disable AccessToDisposedClosure

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
        await InitializeSessionAsync();
        await new AgentChatSessionLoop().RunAsync(
            _ui.NotNull(), _session.NotNull(), _history, _chatClient.NotNull());
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
                opts.Tools = [.. CreateToolsFromSkillset(skills)];
            })
            .UseFunctionInvocation()
            .Build();
        skills.ToolEnabledClient = _chatClient;
        await TestInfrastructureAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_session is { })
        {
            await _session.DisposeAsync();
        }
    }

    async Task InitializeSystemPromptAsync()
    {
        if (_session is null)
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

    static async Task TestInfrastructureAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("--- 🛠️ Infrastructure Sanity Check ---");
        var check = await ProcessRunner.LaunchWithAsync("dotnet", "--version");
        Console.WriteLine($"[1] Dotnet Access: {(check.ExitCode == 0 ? "✅" : "❌")}");
        if (check.ExitCode == 0)
            Console.WriteLine($"    Version: {check.GetCombinedOutput().Trim()}");
        var failCheck = await ProcessRunner.LaunchWithAsync("dotnet", "--gibt-es-nicht");
        Console.WriteLine($"[2] Error Catching: {(failCheck.ExitCode != 0 ? "✅" : "❌")}");
        if (failCheck.Lines.Any(l => l.Level == LogLevel.Error))
            Console.WriteLine("    Stderr captured successfully.");
        Console.WriteLine("--------------------------------------\n");
        Console.ResetColor();
    }

    static IEnumerable<AIFunction> CreateToolsFromSkillset(object target)
        => target.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<DescriptionAttribute>() != null)
            .Select(m => AIFunctionFactory.Create(m, target));
}
