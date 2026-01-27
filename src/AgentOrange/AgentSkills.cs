using Microsoft.Extensions.AI;
using System.ComponentModel;

namespace AgentOrange;

sealed class AgentSkills(IChatClient baseClient) : IDisposable
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

    // --- Web Capabilities ---

    [Description("Ruft den rohen Inhalt einer URL ab.")]
    public async Task<string> FetchUrl(string url)
        => await _http.GetStringAsync(url);

    [Description("Ruft eine Webseite auf und liefert eine gesäuberte Zusammenfassung des Inhalts via LLM.")]
    public async Task<string> SummarizeUrl(string url)
    {
        string rawContent;
        try
        {
            rawContent = await _http.GetStringAsync(url);
        }
        catch (Exception ex)
        {
            return $"Fehler beim Laden der URL: {ex.Message}";
        }

        // Interne, isolierte History für den Summarizer Task
        var internalHistory = new List<ChatMessage>
        {
            new(ChatRole.System,
                "Du bist ein Extraktor. Deine Aufgabe ist es, aus dem folgenden HTML-Content " +
                "nur die wichtigsten News-Schlagzeilen und einen Einleitungssatz zu extrahieren. " +
                "Ignoriere Menüs, Footer und Werbung. Antworte extrem kompakt."),
            new(ChatRole.User, $"Hier ist der Content von {url}:\n\n{rawContent}")
        };

        var response = await baseClient.GetResponseAsync(internalHistory);
        return response.Messages.FirstOrDefault()?.Text ?? "<keine Zusammenfassung>";
    }

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