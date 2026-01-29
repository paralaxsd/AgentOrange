namespace AgentOrange;

static class Program
{
    public static async Task Main()
    {
        var providerStr = Environment.GetEnvironmentVariable("AO_LLM_PROVIDER") ?? "Google";
        var modelName = Environment.GetEnvironmentVariable("AO_MODEL_NAME") ?? "gemini-3-flash-preview";
        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.WriteLine("GEMINI_API_KEY fehlt.");
            return;
        }

        if (!Enum.TryParse<LlmProvider>(providerStr, true, out var provider))
            provider = LlmProvider.Google;

        var config = new AgentChatConfig(provider, modelName, apiKey);
        await using var app = new AgentOrangeApp(config);
        await app.RunAsync();
    }
}
