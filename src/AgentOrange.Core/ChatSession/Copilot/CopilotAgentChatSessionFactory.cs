using AgentOrange.Core.Extensions;
using AgentOrange.Core.Skills;
using GitHub.Copilot.SDK;
using Microsoft.Extensions.AI;

namespace AgentOrange.Core.ChatSession.Copilot;

sealed class CopilotAgentChatSessionFactory : AgentChatSessionFactoryBase
{
    public override async Task<IAgentChatSession> CreateSessionFromAsync(AgentChatConfig config)
    {
        var skills = new AgentSkills();
        var client = new CopilotClient(new CopilotClientOptions { AutoStart = true });
        var systemPrompt = await CreateSystemPromptAsync(config, client);
        var session = await client.CreateSessionAsync(new SessionConfig
        {
            Model = config.ModelName,
            Streaming = true,
            Tools = CreateToolsFromSkills(skills),
            // see: https://github.com/github/copilot-sdk/blob/main/dotnet/README.md#infinite-sessions
            InfiniteSessions = new()
            {
                Enabled = true,
                // Start compacting at 80% context usage (see docs for details)
                BackgroundCompactionThreshold = 0.80,
                // Block at 95% until compaction completes
                BufferExhaustionThreshold = 0.95
            },
            SystemMessage = new()
            {
                Mode = SystemMessageMode.Replace,
                Content = systemPrompt
            }
        });

        var chatClient = new CopilotChatClient(session);
        var chatSession = new CopilotAgentChatSession(client, chatClient, skills, config);
        skills.InitializeWith(chatClient, null);
        chatSession.AddToHistory(ChatRole.System, systemPrompt);
        return chatSession;
    }

    static async Task<string> CreateSystemPromptAsync(AgentChatConfig config, CopilotClient client)
    {
        var model = await client.FindModelInfoAsync(config.ModelName);
        return Utils.CreateSystemPromptFrom(config, model);
    }
}
