using AgentOrange.Skills;
using Microsoft.Extensions.AI;
using System.ComponentModel;
using System.Reflection;

namespace AgentOrange.ChatSession;

abstract class AgentChatSessionFactoryBase : IAgentChatSessionFactory
{
    protected static AIFunction[] CreateToolsFromSkills(AgentSkills skills)
        => [.. CreateToolsFromSkillSet(skills)];

    public abstract Task<IAgentChatSession> CreateSessionFromAsync(AgentChatConfig config);

    static IEnumerable<AIFunction> CreateToolsFromSkillSet(object target)
        => target.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<DescriptionAttribute>() is { })
            .Select(m => AIFunctionFactory.Create(m, target));
}
