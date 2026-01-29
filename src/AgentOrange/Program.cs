using System.ComponentModel;
using System.Reflection;
using System.Text;
using AgentOrange.Core.ProcessHandling;
using Microsoft.Extensions.AI;
using Environment = System.Environment;

namespace AgentOrange;

static class Program
{
    public static async Task Main(string[] args)
    {
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
                // ReSharper disable once AccessToDisposedClosure
                opts.Tools = [.. CreateToolsFromSkillset(skills)];
            })
            .UseFunctionInvocation()
            .Build();

        skills.ToolEnabledClient = chatClient;
        var history = new List<ChatMessage>();

        await TestInfrastructureAsync();

        while (true)
        {
            Console.Write("> ");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                break;

            history.Add(new ChatMessage(ChatRole.User, input));

            string response;
            try
            {
                response = await FetchResponseAsync(chatClient, history);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                Console.WriteLine();
                Console.WriteLine();
            }
            

            history.Add(new ChatMessage(ChatRole.Assistant, response));
            // History kürzen, damit Gemini nicht aus dem Kontext fliegt
            const int maxTurns = 20;
            if (history.Count > maxTurns * 2)
                history.RemoveRange(0, history.Count - maxTurns * 2);
        }
    }

    static async Task TestInfrastructureAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("--- 🛠️ Infrastructure Sanity Check ---");

        // Test 1: Happy Path (dotnet version abfragen)
        var check = await ProcessRunner.LaunchWithAsync("dotnet", "--version");
        Console.WriteLine($"[1] Dotnet Access: {(check.ExitCode == 0 ? "✅" : "❌")}");
        if (check.ExitCode == 0)
            Console.WriteLine($"    Version: {check.GetCombinedOutput().Trim()}");

        // Test 2: Error Handling (Quatsch aufrufen)
        var failCheck = await ProcessRunner.LaunchWithAsync("dotnet", "--gibt-es-nicht");
        Console.WriteLine($"[2] Error Catching: {(failCheck.ExitCode != 0 ? "✅" : "❌")}");
        if (failCheck.Lines.Any(l => l.Level == LogLevel.Error))
            Console.WriteLine("    Stderr captured successfully.");

        Console.WriteLine("--------------------------------------\n");
        Console.ResetColor();
    }

    static IEnumerable<AIFunction> CreateToolsFromSkillset(object target)
    {
        return target.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<DescriptionAttribute>() != null) // Nur Methoden mit Description
            .Select(m => AIFunctionFactory.Create(m, target));
    }

    static async Task<string> FetchResponseAsync(IChatClient chatClient1, List<ChatMessage> chatMessages)
    {
        var assistantText = new StringBuilder();
        await foreach (var part in chatClient1.GetStreamingResponseAsync(chatMessages))
        {
            foreach (var c in part.Text)
            {
                Console.Write(c);
                await Task.Delay(2); // optional für Typing-Feeling
            }
            assistantText.Append(part.Text);
        }
        return assistantText.ToString();
    }
}