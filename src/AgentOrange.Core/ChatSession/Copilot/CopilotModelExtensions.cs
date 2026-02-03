namespace AgentOrange.Core.ChatSession.Copilot;

static class CopilotModelExtensions
{
    public static ModelInfo? ToModelInfo(this GitHub.Copilot.SDK.ModelInfo? modelInfo) =>
        modelInfo is { } ?
            new ModelInfo(modelInfo.Name,
                modelInfo.Capabilities.Limits.MaxPromptTokens,
                modelInfo.Capabilities.Limits.MaxContextWindowTokens)
            : null;

    public static async Task<ModelInfo?> FindModelInfoAsync(this GitHub.Copilot.SDK.CopilotClient client, string modelId)
    {
        var models = await client.ListModelsAsync();
        return models.FirstOrDefault(m => m.Id == modelId)?.ToModelInfo();
    }
}
