using GitHub.Copilot.SDK;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using AgentOrange.Core.Extensions;

namespace AgentOrange.ChatSession.Copilot;

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
        var prompt = messages
            .Select(m => m.Contents.OfType<TextContent>().FirstOrDefault()?.Text)
            .ExceptDefault()
            .LastOrDefault();

        if (prompt is null) { yield break; }

        _ = SendAndHandleStreamingEventsAsync(channel, prompt, token);

        await foreach (var update in channel.Reader.ReadAllAsync(token))
            yield return update;
    }
    
    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var prompt = string.Join("\n", messages.Select(m => m.Contents.OfType<TextContent>().FirstOrDefault()?.Text));
        var response = await _session.SendAndWaitAsync(new MessageOptions { Prompt = prompt }, cancellationToken: cancellationToken);
        var content = response?.Data.Content ?? string.Empty;
        var message = new ChatMessage(ChatRole.Assistant, content);
        return new ChatResponse(message);
    }

    public object? GetService(Type serviceType, object? serviceProvider) => null;

    public void Dispose() => _session.DisposeAsync().AsTask().GetAwaiter().GetResult();

    public async ValueTask DisposeAsync() => await _session.DisposeAsync();

    async Task SendAndHandleStreamingEventsAsync(
        Channel<ChatResponseUpdate> channel, string prompt, CancellationToken token)
    {
        try
        {
            _session.On(evt => OnCopilotEvent(evt, channel.Writer, token));

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
    }

    static void OnCopilotEvent(
        object evt, ChannelWriter<ChatResponseUpdate> writer, CancellationToken token)
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
                writer.TryWrite(new(ChatRole.Assistant, msg.Data.DeltaContent));
                break;

            case ToolExecutionStartEvent toolStart:
                var tsData = toolStart.Data;
                var args = tsData.Arguments as IDictionary<string, object?>;
                writer.TryWrite(new()
                {
                    Contents = [new FunctionCallContent(tsData.ToolCallId, tsData.ToolName, args)]
                });
                break;

            case ToolExecutionCompleteEvent toolEnd:
                var teData = toolEnd.Data;
                writer.TryWrite(new()
                {
                    Contents = [new FunctionResultContent(teData.ToolCallId, teData.Result)]
                });
                break;

            case SessionUsageInfoData uid:
                var uidDetails = new UsageDetails()
                {
                    TotalTokenCount = (long)uid.CurrentTokens,
                };
                writer.TryWrite(new()
                {
                    Contents = [new UsageContent(uidDetails)]
                });
                break;

            case AssistantUsageData aud:
                var audDetails = new UsageDetails()
                {
                    InputTokenCount = (long?)aud.InputTokens,
                    OutputTokenCount = (long?)aud.OutputTokens,
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
}
