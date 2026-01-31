namespace AgentOrange.ChatSession.Copilot;

static class Maps
{
    public static ModelInfo? ToModelInfo(this GitHub.Copilot.SDK.ModelInfo? modelInfo) =>
        modelInfo is { } ? 
            new ModelInfo(modelInfo.Name,
                modelInfo.Capabilities.Limits.MaxPromptTokens,
                modelInfo.Capabilities.Limits.MaxContextWindowTokens)
            : null;
}