using AgentOrange.ChatSession;
using AgentOrange.Core.Extensions;
using AgentOrange.TokenUsage;
using Microsoft.Extensions.AI;
using System.Text;
using Spectre.Console;

namespace AgentOrange;

sealed class AgentChatSessionLoop(IAgentOrangeUi ui, IAgentChatSession session)
{
    /******************************************************************************************
     * FIELDS
     * ***************************************************************************************/
    readonly IAgentOrangeUi _ui = ui;
    readonly IAgentChatSession _session = session;

    /******************************************************************************************
     * PROPERTIES
     * ***************************************************************************************/
    List<ChatMessage> History => _session.History;
    IChatClient ChatClient => _session.Skills.ToolEnabledClient;

    /******************************************************************************************
     * METHODS
     * ***************************************************************************************/
    public async Task RunAsync()
    {
        _ui.WriteWelcome();

        while (true)
        {
            var input = _ui.ReadUserInput();
            if (input.IsEmpty)
                continue;
            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                break;

            History.Add(await CreateUserMessageAsync(input));

            try
            {
                var (response, usage) = await FetchResponseAsync();
                AddAssistantResponseToHistory(response, usage);
            }
            catch (HttpRequestException e)
            {
                _ui.WriteError(e);
            }
            finally
            {
                _ui.WriteBlankLine();
                _ui.WriteBlankLine();
            }
            await History.PruneAsync(_session);
        }
    }

    async Task<ChatMessage> CreateUserMessageAsync(string input)
    {
        var timestamp = DateTime.Now.ToString("yyyy - MM - dd HH:mm(ddd)");
        var metaSeg = $"META: {timestamp}";
        var usage = _session.TokenUsageProvider is { } provider ?
            await provider.GetTokenUsageAsync(History, input) : TokenUsageInfo.Empty;
        var inputTokens = usage.InputTokens is { } inCount ? $"In: {inCount}" : null;
        var outputTokens = usage.OutputTokens is { } outCount ? $"Out: {outCount}" : null;
        var totalTokens = usage.TotalTokens is { } tokenCount ? $"Total: {tokenCount}" : null;
        IEnumerable<string?> segments = [metaSeg, inputTokens, outputTokens, totalTokens];
        var metaBlock = $"[{segments.ExceptDefault().JoinedBy(" | ")}]";
        var fullInput = $"{metaBlock}\n{input}";
        return new(ChatRole.User, fullInput);
    }

    void AddAssistantResponseToHistory(string response, UsageContent? usage)
    {
        var msg = CreateChatMessageFrom(response, usage);
        History.Add(msg);
    }
    
    async Task<(string, UsageContent?)> FetchResponseAsync()
    {
        const int maxRetries = 2;
        var assistantText = new StringBuilder();
        UsageContent? usage = null;

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                await foreach (var part in ChatClient.GetStreamingResponseAsync(History))
                {
                    usage = part.Contents.FirstOrDefault<UsageContent>();

                    if(part.Contents.FirstOrDefault<FunctionCallContent>() is { }  funcCall)
                    {
                        AnsiConsole.MarkupLine($"\t[cyan]→ Running: [bold]{funcCall.Name}[/] ({funcCall.CallId})[/]");
                    }
                    if(part.Contents.FirstOrDefault<FunctionResultContent>() is { } funcResult) 
                    {
                        AnsiConsole.MarkupLine($"\t[cyan]→ Finished: call {funcResult.CallId}[/]");
                    }
                    if (part.Contents.FirstOrDefault<TextReasoningContent>() is { } reasoning)
                    {
                        AnsiConsole.MarkupLine($"\t*[yellow]{reasoning.Text}[/]*");
                    }
                    
                    if(part.Text.HasContent)
                    {
                        foreach (var c in part.Text)
                        {
                            _ui.Write(c.ToString());
                            await Task.Delay(2);
                        }
                        assistantText.Append(part.Text);
                    }
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
                AnsiConsole.MarkupLine($"[bold red]Error while streaming:[/] [red]{ex.Message}[/]");
                break;
            }
        }
        return (assistantText.ToString(), usage);
    }

    static ChatMessage CreateChatMessageFrom(string response, UsageContent? usage)
    {
        IList<AIContent> content = [new TextContent(response)];
        if (usage is { })
            content.Add(usage);

        return new(ChatRole.Assistant, content);
    }
}
