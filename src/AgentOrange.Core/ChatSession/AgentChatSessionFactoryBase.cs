using System.ComponentModel;
using System.Reflection;
using AgentOrange.Core.Skills;
using Microsoft.Extensions.AI;

namespace AgentOrange.Core.ChatSession;

public abstract class AgentChatSessionFactoryBase : IAgentChatSessionFactory
{
    protected static AIFunction[] CreateToolsFromSkills(AgentSkills skills)
        => [.. CreateToolsFromSkillSet(skills)];

    public abstract Task<IAgentChatSession> CreateSessionFromAsync(AgentChatConfig config);

    static IEnumerable<AIFunction> CreateToolsFromSkillSet(object target)
        => target.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<DescriptionAttribute>() is { })
            .Select(m => AIFunctionFactory.Create(m, target));
}
