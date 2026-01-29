namespace AgentOrange;

enum LlmProvider { Google, OpenAI, Azure, Claude }

record AgentChatConfig(LlmProvider Provider, string ModelName, string ApiKey);
