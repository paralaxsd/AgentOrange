using Microsoft.Extensions.AI;
using System.ComponentModel;
using AgentOrange.Core.Extensions;

namespace AgentOrange.Core.Skills;

public sealed partial class AgentSkills : IDisposable
{
    Func<Task<IChatClient>>? _createSubcontractClient;

    public IChatClient ToolEnabledClient { get; private set; } = default!;

    [Description("Addiert zwei Zahlen.")]
    public decimal Add(decimal lhs, decimal rhs) => lhs + rhs;

    [Description("Multipliziert zwei Zahlen.")]
    public decimal Multiply(decimal lhs, decimal rhs) => lhs * rhs;

    [Description("Dividiert zwei Zahlen.")]
    public decimal Divide(decimal lhs, decimal rhs) => lhs / rhs;

    [Description("Gibt die aktuelle Uhrzeit zurück.")]
    public DateTimeOffset GetCurrentTime() => DateTimeOffset.Now;

    [Description("Erlaubt es einen Sub-Agenten mit einem spezifischen System-Prompt eine Frage lösen zu lassen.")]
    public async Task<string> Subcontract(
        [Description("Die Rolle/Aufgabe des Sub-Agenten")] string systemPrompt,
        [Description("Die konkrete Aufgabe")] string prompt)
    {
        var internalHistory = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, prompt)
        };

        return await SendToSubcontractorAsync(internalHistory);
    }

    [Description("Gründet einen Arbeitskreis mit bis zu 5 " +
        "spezialisierten Mitarbeitern für komplexe Aufgaben.")]
    public async Task<string> EstablishWorkingGroup(
        [Description("Die zu lösende Aufgabe")]
        string task,
        [Description("Bis zu 5 Rollen, komma-getrennt")]
        string roles)
    {
        var roleList = roles.Split(',')
            .Select(r => r.Trim())
            .Take(5)
            .ToList();

        var results = new List<string>();

        // Phase 1: Kick-off Meeting
        results.Add("=== ARBEITSKREIS KICK-OFF ===");
        results.Add($"Aufgabe: {task}");
        results.Add($"Team: {roleList.JoinedBy(", ")}");
        results.Add("");

        // Phase 2: Parallel delegation
        var tasks = roleList.Select(async role =>
        {
            var prompt = $"Als {role}: {task}";
            return await Subcontract(
                $"You are a {role}. Be professional.",
                prompt
            );
        });

        var responses = await Task.WhenAll(tasks);

        // Phase 3: Consolidation
        for (var i = 0; i < roleList.Count; i++)
        {
            results.Add($"--- {roleList[i]} ---");
            results.Add(responses[i]);
            results.Add("");
        }

        // Phase 4: Schnittchen! 🥪
        results.Add("=== CATERING ===");
        results.Add("☑ Schnittchen bereitgestellt");
        results.Add("☑ Kaffee verfügbar");
        results.Add("");

        // Phase 5: Executive Summary
        var summary = await Subcontract(
            "You are an executive assistant creating " +
            "a summary for the board.",
            $"Summarize these findings: " +
            $"{responses.JoinedBy("\n\n")}"
        );

        results.Add("=== EXECUTIVE SUMMARY ===");
        results.Add(summary);

        return results.JoinedBy("\n");
    }

    async Task<string> SendToSubcontractorAsync(List<ChatMessage> messages)
    {
        try
        {
            IChatClient client;
            IAsyncDisposable? disposable;

            if (_createSubcontractClient is { } factory)
            {
                client = await factory();
                disposable = client as IAsyncDisposable;
            }
            else
            {
                client = ToolEnabledClient;
                disposable = null;
            }

            await using (disposable)
            {
                var response = await client.GetResponseAsync(messages);
                return response.Messages.FirstOrDefault()?.Text ?? "<keine Antwort>";
            }
        }
        catch (Exception ex)
        {
            return $"[Subcontract-Fehler] {ex.Message}";
        }
    }

    public void InitializeWith(IChatClient toolClient, Func<Task<IChatClient>>? createSubcontractClient = null) =>
        (ToolEnabledClient, _createSubcontractClient) = (toolClient, createSubcontractClient);

    public void Dispose() => _http.Dispose();
}
