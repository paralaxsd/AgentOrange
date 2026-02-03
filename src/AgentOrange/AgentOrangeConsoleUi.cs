namespace AgentOrange;

sealed class AgentOrangeConsoleUi : IAgentOrangeUi
{
    public void WriteWelcome()
    {
        Console.WriteLine("AgentOrange 🍊 — Gemini Console Chat");
        Console.WriteLine("Tippe 'exit' zum Beenden.\n");
    }

    public string? ReadUserInput()
    {
        Console.Write("> ");
        return Console.ReadLine();
    }

    public void Write(string text) => Console.Write(text);

    public void WriteLine(string text) => Console.WriteLine(text);

    public void WriteError(Exception ex)
    {
        Console.WriteLine($"[Fehler] {ex.Message}");
    }

    public void WriteBlankLine() => Console.WriteLine();
}
