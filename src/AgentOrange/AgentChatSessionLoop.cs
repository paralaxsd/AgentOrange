using AgentOrange.ChatSession;
using AgentOrange.Core.Extensions;
using AgentOrange.TokenUsage;
using Microsoft.Extensions.AI;
using System.Text;

namespace AgentOrange;

sealed class AgentChatSessionLoop
{
    public async Task RunAsync(
        IAgentOrangeUi ui, IAgentChatSession session, IList<ChatMessage> history, IChatClient chatClient)
    {
        ui.WriteWelcome();
        while (true)
        {
            var input = ui.ReadUserInput();
            if (string.IsNullOrWhiteSpace(input))
                continue;
            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                break;

            history.Add(await CreateUserMessageAsync(input, session, history));

            try
            {
                var (response, usage) = await FetchResponseAsync(chatClient, history);
                ui.WriteResponse(response);
                AddAssistantResponseToHistory(response, usage, history);
            }
            catch (HttpRequestException e)
            {
                ui.WriteError(e);
            }
            finally
            {
                ui.WriteBlankLine();
                ui.WriteBlankLine();
            }
            await history.PruneAsync(session);
        }
    }

    static async Task<ChatMessage> CreateUserMessageAsync(string input, IAgentChatSession session, IList<ChatMessage> history)
    {
        var timestamp = DateTime.Now.ToString("yyyy - MM - dd HH:mm(ddd)");
        var metaSeg = $"META: {timestamp}";
        var usage = session.TokenUsageProvider is { } provider ?
            await provider.GetTokenUsageAsync(history, input) : TokenUsageInfo.Empty;
        var inputTokens = usage.InputTokens is { } inCount ? $"In: {inCount}" : null;
        var outputTokens = usage.OutputTokens is { } outCount ? $"Out: {outCount}" : null;
        var totalTokens = usage.TotalTokens is { } tokenCount ? $"Total: {tokenCount}" : null;
        IEnumerable<string?> segments = [metaSeg, inputTokens, outputTokens, totalTokens];
        var metaBlock = $"[{segments.ExceptDefault().JoinedBy(" | ")}]";
        var fullInput = $"{metaBlock}\n{input}";
        return new(ChatRole.User, fullInput);
    }

    static void AddAssistantResponseToHistory(string response, UsageContent? usage, IList<ChatMessage> history)
    {
        IList<AIContent> content = [new TextContent(response)];
        if (usage is { })
            content.Add(usage);
        var msg = new ChatMessage(ChatRole.Assistant, content);
        history.Add(msg);
    }

    static async Task<(string, UsageContent?)> FetchResponseAsync(
        IChatClient chatClient, IList<ChatMessage> chatMessages)
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
                Spectre.Console.AnsiConsole.MarkupLine($"[bold red]Fehler beim Streamen:[/] [red]{ex.Message}[/]");
                break;
            }
        }
        return (assistantText.ToString(), usage);
    }
}
