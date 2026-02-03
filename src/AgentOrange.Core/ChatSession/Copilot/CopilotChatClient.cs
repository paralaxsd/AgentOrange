using System.Runtime.CompilerServices;
using System.Threading.Channels;
using AgentOrange.Core.Extensions;
using GitHub.Copilot.SDK;
using Microsoft.Extensions.AI;

namespace AgentOrange.Core.ChatSession.Copilot;

sealed class CopilotChatClient(CopilotSession session) : IChatClient, IAsyncDisposable
{
    /******************************************************************************************
     * FIELDS
     * ***************************************************************************************/
    readonly CopilotSession _session = session;

    /******************************************************************************************
     * METHODS
     * ***************************************************************************************/
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken token = default)
    {
        var channel = Channel.CreateUnbounded<ChatResponseUpdate>();
        var prompt = GetUserPromptOrDefaultFrom(messages);

        if (prompt is null) { yield break; }

        var scopeTask = Task.FromResult<IDisposable>(NullScope.Instance);
        try
        {
            scopeTask = SendAndHandleStreamingEventsAsync(channel, prompt, token);

            await foreach (var update in channel.Reader.ReadAllAsync(token))
                yield return update;
        }
        finally
        {
            using var _ = await scopeTask;
        }
    }

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if(GetUserPromptOrDefaultFrom(messages) is not { } prompt)
        {
            prompt = "<no user prompt provided>";
        }

        var msgOpts = new MessageOptions { Prompt = prompt };
        var response = await _session.SendAndWaitAsync(msgOpts, cancellationToken: cancellationToken);
        var content = response?.Data.Content ?? string.Empty;
        var message = new ChatMessage(ChatRole.Assistant, content);
        return new ChatResponse(message);
    }

    public object? GetService(Type serviceType, object? serviceProvider) => null;

    public void Dispose() => _session.DisposeAsync().AsTask().GetAwaiter().GetResult();

    public async ValueTask DisposeAsync() => await _session.DisposeAsync();

    async Task<IDisposable> SendAndHandleStreamingEventsAsync(
        Channel<ChatResponseUpdate> channel, string prompt, CancellationToken token)
    {
        IDisposable res = NullScope.Instance;

        try
        {
            res = _session.On(evt => OnCopilotEvent(evt, channel.Writer, token));

            var msgOpts = new MessageOptions { Prompt = prompt };
            await _session.SendAsync(msgOpts, token);
        }
        catch (Exception ex)
        {
            var errorUpdate = new ChatResponseUpdate()
            {
                Contents = [new ErrorContent(ex.Message)]
            };
            channel.Writer.TryWrite(errorUpdate);
            channel.Writer.TryComplete(ex);
        }

        return res;
    }

    static void OnCopilotEvent(
        SessionEvent evt, ChannelWriter<ChatResponseUpdate> writer, CancellationToken token)
    {
        if (token.IsCancellationRequested)
        {
            writer.TryComplete();
            return;
        }

        switch (evt)
        {
            case AssistantMessageEvent:
                // ignored - this is the full message, we stream deltas instead
                break;

            case AssistantMessageDeltaEvent msg:
                writer.TryWrite(new()
                {
                    Contents = [new TextContent(msg.Data.DeltaContent)]
                });
                break;

            case AssistantReasoningDeltaEvent rdEvt:
                writer.TryWrite(new()
                {
                    Contents = [new TextReasoningContent(rdEvt.Data.DeltaContent)]
                });
                break;

            case AssistantReasoningEvent rEvt:
                writer.TryWrite(new()
                {
                    Contents = [new TextReasoningContent(rEvt.Data.Content)]
                });
                break;

            case SystemMessageEvent sysMsg:
                writer.TryWrite(new()
                {
                    Contents = [new TextReasoningContent(sysMsg.Data.Content)]
                });
                break;

            case ToolExecutionStartEvent toolStart:
                var tsData = toolStart.Data;
                var callArgs = tsData.Arguments as IDictionary<string, object?>;

                List<AIContent> contents = [new FunctionCallContent(tsData.ToolCallId, tsData.ToolName, callArgs)];
                TryExtractReasoningContentInto(contents, tsData);
                writer.TryWrite(new()
                {
                    Contents = contents
                });
                break;

            case ToolExecutionCompleteEvent toolEnd:
                var teData = toolEnd.Data;
                writer.TryWrite(new()
                {
                    Contents = [new FunctionResultContent(teData.ToolCallId, teData.Result)]
                });
                break;

            case SessionUsageInfoEvent uid:
                var uidDetails = new UsageDetails()
                {
                    TotalTokenCount = (long)uid.Data.CurrentTokens,
                };
                writer.TryWrite(new()
                {
                    Contents = [new UsageContent(uidDetails)]
                });
                break;

            case AssistantUsageEvent aud:
                var audDetails = new UsageDetails()
                {
                    InputTokenCount = (long?)aud.Data.InputTokens,
                    OutputTokenCount = (long?)aud.Data.OutputTokens,
                };
                writer.TryWrite(new()
                {
                    Contents = [new UsageContent(audDetails)]
                });
                break;

            case SessionIdleEvent:
                writer.TryComplete();
                break;
        }
    }

    static void TryExtractReasoningContentInto(
        List<AIContent> contents, ToolExecutionStartData tsData)
    {
        if (tsData is not
        {
            ToolName: "report_intent", Arguments: System.Text.Json.JsonElement jsonEl
        }) { return; }

        var intent = jsonEl.TryGetProperty("intent", out var intentProp) && 
            intentProp.ValueKind == System.Text.Json.JsonValueKind.String
            ? intentProp.GetString() : null;
        if(intent.HasContent)
        {
            contents.Add(new TextReasoningContent(intent));
        }
    }

    static string? GetUserPromptOrDefaultFrom(IEnumerable<ChatMessage> messages) =>
        messages
            .Select(m => m.Contents.OfType<TextContent>().FirstOrDefault()?.Text)
            .ExceptDefault()
            .LastOrDefault();
}
