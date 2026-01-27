using AgentOrange;
using Microsoft.Extensions.AI;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using Environment = System.Environment;

Console.WriteLine("AgentOrange 🍊 — Gemini Console Chat");
Console.WriteLine("Tippe 'exit' zum Beenden.\n");

var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.WriteLine("GEMINI_API_KEY fehlt.");
    return;
}

await using var googleClient = new Google.GenAI.Client(apiKey:apiKey);

using var baseClient = googleClient.AsIChatClient("gemini-3-flash-preview");
using var skills = new AgentSkills(baseClient);
using var summarizerClient = baseClient.AsBuilder().Build();
using var chatClient = baseClient.AsBuilder()
    .ConfigureOptions(opts =>
    {
        opts.AllowMultipleToolCalls = true;
        opts.Tools = [.. CreateToolsFromSkillset(skills)];
    })
    .UseFunctionInvocation()
    .Build();

skills.ToolEnabledClient = chatClient;
var history = new List<ChatMessage>();

while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input))
        continue;

    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;

    history.Add(new ChatMessage(ChatRole.User, input));
    
    var assistantText = new StringBuilder();
    await foreach (var part in chatClient.GetStreamingResponseAsync(history))
    {
        foreach (var c in part.Text)
        {
            Console.Write(c);
            await Task.Delay(2); // optional für Typing-Feeling
        }
        assistantText.Append(part.Text);
    }
    Console.WriteLine();

    history.Add(new ChatMessage(ChatRole.Assistant, assistantText.ToString()));
    // History kürzen, damit Gemini nicht aus dem Kontext fliegt
    const int maxTurns = 20;
    if (history.Count > maxTurns * 2)
        history.RemoveRange(0, history.Count - maxTurns * 2);
}

static IEnumerable<AIFunction> CreateToolsFromSkillset(object target)
{
    return target.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .Where(m => m.GetCustomAttribute<DescriptionAttribute>() != null) // Nur Methoden mit Description
        .Select(m => AIFunctionFactory.Create(m, target));
}