using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace AgentOrange.Core.Skills;

sealed partial class AgentSkills
{
    readonly HttpClient _http = new();

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

        var internalHistory = new List<ChatMessage>
        {
            new(ChatRole.System,
                "Du bist ein Extraktor. Deine Aufgabe ist es, aus dem folgenden HTML-Content " +
                "nur die wichtigsten News-Schlagzeilen und einen Einleitungssatz zu extrahieren. " +
                "Ignoriere Menüs, Footer und Werbung. Antworte extrem kompakt."),
            new(ChatRole.User, $"Hier ist der Content von {url}:\n\n{rawContent}")
        };

        var client = _baseClient ?? ToolEnabledClient;
        var response = await client.GetResponseAsync(internalHistory);
        return response.Messages.FirstOrDefault()?.Text ?? "<keine Zusammenfassung>";
    }
}
