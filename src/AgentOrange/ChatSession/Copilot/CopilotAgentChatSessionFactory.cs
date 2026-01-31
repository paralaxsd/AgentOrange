using AgentOrange.Core.Extensions;
using GitHub.Copilot.SDK;
using Microsoft.Extensions.AI;

namespace AgentOrange.ChatSession.Copilot;

sealed class CopilotAgentChatSessionFactory : IAgentChatSessionFactory
{
    public async Task<IAgentChatSession> CreateSessionFromAsync(AgentChatConfig config)
    {
        var client = new CopilotClient(new CopilotClientOptions()
        {
            AutoStart = true,
        });

        var systemPrompt = await CreateSystemPromptAsync(config, client);
        
        var session = await client.CreateSessionAsync(new SessionConfig
        {
            Model = config.ModelName,
            Streaming = true,
            InfiniteSessions = new() // also see: https://github.com/github/copilot-sdk/blob/main/dotnet/README.md#infinite-sessions
            {
                Enabled = true,
                BackgroundCompactionThreshold = 0.80, // Start compacting at 80% context usage
                BufferExhaustionThreshold = 0.95      // Block at 95% until compaction completes
            },
            SystemMessage = new()
            {
                Mode = SystemMessageMode.Replace,
                Content = systemPrompt
            }
        });
        
        var chatClient = new CopilotChatClient(session);
        var chatSession = new CopilotAgentChatSession(client, chatClient);
        chatSession.AddToHistory(ChatRole.System, systemPrompt);

        return chatSession;
    }

    static async Task<string> CreateSystemPromptAsync(AgentChatConfig config, CopilotClient client)
    {
        var models = (await client.ListModelsAsync());
        var thisCopilotModel = models.FirstOrDefault(m => m.Id == config.ModelName);
        var model = thisCopilotModel.ToModelInfo();
        
        return Utils.CreateSystemPromptFrom(config, model);
    }
}
