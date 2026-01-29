using Microsoft.Extensions.AI;
using System.ComponentModel;

namespace AgentOrange.Skills;

sealed partial class AgentSkills(IChatClient baseClient) : IDisposable
{
    /******************************************************************************************
     * FIELDS
     * ***************************************************************************************/
    readonly HttpClient _http = new();

    /******************************************************************************************
     * PROPERTIES
     * ***************************************************************************************/
    public IChatClient? ToolEnabledClient { get; set; }

    /******************************************************************************************
     * METHODS
     * ***************************************************************************************/
    // --- Math & Utility ---

    [Description("Addiert zwei Zahlen.")]
    public decimal Add(decimal lhs, decimal rhs) => lhs + rhs;

    [Description("Multipliziert zwei Zahlen.")]
    public decimal Multiply(decimal lhs, decimal rhs) => lhs * rhs;

    [Description("Dividiert zwei Zahlen.")]
    public decimal Divide(decimal lhs, decimal rhs) => lhs / rhs;

    [Description("Gibt die aktuelle Uhrzeit zurück.")]
    public DateTimeOffset GetCurrentTime() => DateTimeOffset.Now;

    // --- Agentic Capabilities ---

    [Description("Erlaubt es einen Sub-Agenten mit einem spezifischen System-Prompt eine Frage lösen zu lassen.")]
    public async Task<string> Subcontract(
        [Description("Die Rolle/Aufgabe des Sub-Agenten")] string systemPrompt,
        [Description("Die konkrete Aufgabe")] string prompt)
    {
        var clientToUse = ToolEnabledClient ?? baseClient;

        var internalHistory = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, prompt)
        };

        // Hier nutzen wir den internalClient rekursiv
        var response = await clientToUse.GetResponseAsync(internalHistory);
        return response.Messages.FirstOrDefault()?.Text ?? "<keine Antwort>";
    }

    public void Dispose() => _http.Dispose();
}