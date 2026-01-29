using AgentOrange.Core.Extensions;
using Google.GenAI.Types;
using Microsoft.Extensions.AI;

namespace AgentOrange.TokenUsage;

sealed class GoogleTokenUsageProvider(Google.GenAI.Client googleClient, string modelName) : IAgentTokenUsageProvider
{
    public async Task<TokenUsageInfo> GetTokenUsageAsync(List<ChatMessage> history, string? userInput = null)
    {
        var content =
            (from msg in history
             let text = msg.Contents.OfType<TextContent>().Select(tc => tc.Text).JoinedBy("\n")
             let role = msg.Role == ChatRole.Assistant ? "model" : msg.Role == ChatRole.System ? "system" : "user"
             select new Content()
             {
                 Role = role,
                 Parts = [new() { Text = text }]
             }).ToList();
        if (userInput.HasContent)
        {
            content.Add(new()
            {
                Role = "user",
                Parts = [new() { Text = userInput }]
            });
        }

        var result = await googleClient.Models.CountTokensAsync(modelName, content);
        var total = result?.TotalTokens;
        return new(null, null, total);
    }
}
