namespace AgentOrange;

static class Program
{
    public static async Task Main()
    {
        if (CreateAgentConfig() is { } config)
        {
            await using var app = new AgentOrangeApp(config);
            await app.RunAsync();
        }
    }

    static AgentChatConfig? CreateAgentConfig()
    {
        var providerStr = Environment.GetEnvironmentVariable("AO_LLM_PROVIDER") ?? "Google";
        var modelName = Environment.GetEnvironmentVariable("AO_MODEL_NAME") ?? "gemini-3-flash-preview";
        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.WriteLine("GEMINI_API_KEY fehlt.");
            return null;
        }

        if (!Enum.TryParse<LlmProvider>(providerStr, true, out var provider))
            provider = LlmProvider.Google;

        return new(provider, modelName, apiKey);
    }
}
