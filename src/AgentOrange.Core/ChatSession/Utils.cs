using AgentOrange.Core.Extensions;

namespace AgentOrange.Core.ChatSession;

static class Utils
{
    public static string CreateSystemPromptFrom(AgentChatConfig config, ModelInfo? modelInfo)
    {
        var displayName = modelInfo?.DisplayName ?? config.ModelName;
        var inputTokenLimit = modelInfo?.InputTokenLimit ?? 8192;
        var outputTokenLimit = modelInfo?.OutputTokenLimit ?? 8192;
        return config.SystemPrompt.WithArgs(displayName, inputTokenLimit, outputTokenLimit);
    }
}