using AgentOrange.Skills;
using AgentOrange.TokenUsage;
using Google.GenAI;
using Microsoft.Extensions.AI;

namespace AgentOrange.ChatSession.Google;

sealed class GoogleAgentChatSessionFactory : AgentChatSessionFactoryBase
{
    /******************************************************************************************
     * METHODS
     * ***************************************************************************************/
    public override async Task<IAgentChatSession> CreateSessionFromAsync(AgentChatConfig config)
    {
        var googleClient = new Client(apiKey: config.ApiKey);
        var chatClient = googleClient.AsIChatClient(config.ModelName);
        var tokenProvider = new GoogleTokenUsageProvider(googleClient, config.ModelName);
        var skills = new AgentSkills();
        var session = new GoogleAgentChatSession(config, googleClient, chatClient, tokenProvider, skills);
        await session.InitializeHistoryAsync(config);

        // Tools/Skills nachträglich am Client registrieren
        var toolEnabledClient = CreateToolConsumingClientFrom(chatClient, skills);
        skills.InitializeWith(toolEnabledClient, chatClient);

        return session;
    }

    static IChatClient CreateToolConsumingClientFrom(IChatClient chatClient, AgentSkills skills) =>
        chatClient.AsBuilder()
            .ConfigureOptions(opts =>
            {
                opts.AllowMultipleToolCalls = true;
                opts.Tools = [..CreateToolsFromSkills(skills)];
            })
            .UseFunctionInvocation()
            .Build();
}
