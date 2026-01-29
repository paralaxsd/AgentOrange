using Microsoft.Extensions.AI;

namespace AgentOrange.TokenUsage;

sealed class GoogleTokenUsageProvider(Google.GenAI.Client googleClient, string modelName) : IAgentTokenUsageProvider
{
    public async Task<TokenUsageInfo> GetTokenUsageAsync(List<ChatMessage> history, string? userInput = null)
    {
        var content = new List<Google.GenAI.Types.Content>();
        foreach (var msg in history)
        {
            var text = string.Join("\n", msg.Contents.OfType<TextContent>().Select(tc => tc.Text));
            var role = msg.Role == ChatRole.Assistant ? "model" : msg.Role == ChatRole.System ? "system" : "user";
            content.Add(new()
            {
                Role = role,
                Parts = [new() { Text = text }]
            });
        }
        if (!string.IsNullOrWhiteSpace(userInput))
        {
            content.Add(new()
            {
                Role = "user",
                Parts = [new() { Text = userInput }]
            });
        }

        var result = await googleClient.Models.CountTokensAsync(modelName, content);
        var total = (int?)result?.TotalTokens;
        return new TokenUsageInfo(null, null, total);
    }
}
