using AgentOrange.Core.Extensions;
using AgentOrange.Core.ProcessHandling;
using AgentOrange.Skills;
using AgentOrange.TokenUsage;
using Microsoft.Extensions.AI;
using Spectre.Console;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using AgentOrange.ChatSession;

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

    /******************************************************************************************
     * METHODS
     * ***************************************************************************************/
    public async Task RunAsync()
    {
        Console.WriteLine("AgentOrange 🍊 — Gemini Console Chat");
        Console.WriteLine("Tippe 'exit' zum Beenden.\n");

        _session = await AgentChatSessionFactory.CreateSessionFromAsync(_config);

        await InitializeSystemPromptAsync();

        var baseClient = _session.ChatClient;
        using var skills = new AgentSkills(baseClient);
        using var summarizerClient = baseClient.AsBuilder().Build();
        using var chatClient = baseClient.AsBuilder()
            .ConfigureOptions(opts =>
            {
                opts.AllowMultipleToolCalls = true;
                opts.Tools = [.. CreateToolsFromSkillset(skills)];
            })
            .UseFunctionInvocation()
            .Build();

        skills.ToolEnabledClient = chatClient;

        await TestInfrastructureAsync();

        while (true)
        {
            Console.Write("> ");
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
                continue;
            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                break;
            _history.Add(await CreateUserMessageFromAsync(input));

            try
            {
                var (response, usage) = await FetchResponseAsync(chatClient, _history);
                AddAssistantResponseToHistory(response, usage);
            }
            catch (HttpRequestException e)
            {
                AnsiConsole.WriteException(e);
            }
            finally
            {
                Console.WriteLine();
                Console.WriteLine();
            }

            await PruneHistoryAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_session is not null)
            await _session.DisposeAsync();
    }

    async Task PruneHistoryAsync()
    {
        if (_session?.TokenUsageProvider is not { } provider)
            return;

        var modelInfo = await _session.GetModelInfoAsync();
        var limit = (modelInfo?.InputTokenLimit ?? 8192) * 0.7;

        while (true)
        {
            var usage = await provider.GetTokenUsageAsync(_history);
            if (usage.TotalTokens <= limit || _history.Count <= 2)
                break;
            _history.RemoveAt(1); // System-Prompt bleibt immer erhalten
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

    void AddAssistantResponseToHistory(string response, UsageContent? usage)
    {
        IList<AIContent> content = [new TextContent(response)];
        if(usage is { })
        {
            content.Add(usage);
        }
        var msg = new ChatMessage(ChatRole.Assistant, content);
        _history.Add(msg);
    }


    async Task<ChatMessage> CreateUserMessageFromAsync(string input)
    {
        var metaBlock = await CreateMetaBlockAsync(input);
        var fullInput = $"{metaBlock}\n{input}";
        return new(ChatRole.User, fullInput);
    }

    async Task<string> CreateMetaBlockAsync(string input)
    {
        var timestamp = DateTime.Now.ToString("yyyy - MM - dd HH:mm(ddd)");
        var metaSeg = $"META: {timestamp}";
        var usage = _session?.TokenUsageProvider is { } provider ? 
            await (provider.GetTokenUsageAsync(_history, input)) : TokenUsageInfo.Empty;
        var inputTokens = usage.InputTokens is { } inCount ? $"In: {inCount}" : null;
        var outputTokens = usage.OutputTokens is { } outCount ? $"Out: {outCount}" : null;
        var totalTokens = usage.TotalTokens is { } tokenCount ? $"Total: {tokenCount}" : null;
        IEnumerable<string?> segments = [metaSeg, inputTokens, outputTokens, totalTokens];
        return $"[{segments.ExceptDefault().JoinedBy(" | ")}]";
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

    static async Task<(string, UsageContent?)> FetchResponseAsync(
        IChatClient chatClient, List<ChatMessage> chatMessages)
    {
        const int maxRetries = 2;
        var assistantText = new StringBuilder();
        UsageContent? usage = null;

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                await foreach (var part in chatClient.GetStreamingResponseAsync(chatMessages))
                {
                    usage = part.Contents.OfType<UsageContent>().FirstOrDefault();
                    foreach (var c in part.Text)
                    {
                        Console.Write(c);
                        await Task.Delay(2);
                    }
                    assistantText.Append(part.Text);
                }
                return (assistantText.ToString(), usage);
            }
            catch (InvalidOperationException ex)
                when (ex.Message.Contains("Incomplete JSON segment") && attempt < maxRetries)
            {
                await Task.Delay(500);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[bold red]Fehler beim Streamen:[/] [red]{ex.Message}[/]");
                break;
            }
        }

        return (assistantText.ToString(), usage);
    }
}
