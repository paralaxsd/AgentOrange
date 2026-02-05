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

        var lines = new List<string>();
        var currentLine = "";

        while (true)
        {
            var key = System.Console.ReadKey(intercept: true);

            switch (key.Key)
            {
                case ConsoleKey.Enter
                    when (key.Modifiers & ConsoleModifiers.Shift) != 0
                         || System.Console.KeyAvailable:
                    lines.Add(currentLine);
                    currentLine = "";
                    System.Console.WriteLine();
                    System.Console.Write("  ");
                    break;

                case ConsoleKey.Enter:
                    lines.Add(currentLine);
                    System.Console.WriteLine();
                    return string.Join(Environment.NewLine, lines);

                case ConsoleKey.Backspace when currentLine.Length > 0:
                    currentLine = currentLine[..^1];
                    System.Console.Write("\b \b");
                    break;

                default:
                    if (!char.IsControl(key.KeyChar))
                    {
                        currentLine += key.KeyChar;
                        System.Console.Write(key.KeyChar);
                    }
                    break;
            }
        }
    }

    public void Write(string text) => System.Console.Write(text);

    public void WriteLine(string text) => System.Console.WriteLine(text);

    public void WriteError(Exception ex)
    {
        System.Console.WriteLine($"[Fehler] {ex.Message}");
    }

    public void WriteBlankLine() => System.Console.WriteLine();
}
