namespace AgentOrange.Console;

sealed class AgentOrangeConsoleUi : IAgentOrangeUi
{
    public void WriteWelcome()
    {
        System.Console.WriteLine("AgentOrange 🍊 — Gemini Console Chat");
        System.Console.WriteLine("Tippe 'exit' zum Beenden.\n");
    }

    public string? ReadUserInput()
    {
        System.Console.Write("> ");
        return System.Console.ReadLine();
    }

    public void Write(string text) => System.Console.Write(text);

    public void WriteLine(string text) => System.Console.WriteLine(text);

    public void WriteError(Exception ex)
    {
        System.Console.WriteLine($"[Fehler] {ex.Message}");
    }

    public void WriteBlankLine() => System.Console.WriteLine();
}
