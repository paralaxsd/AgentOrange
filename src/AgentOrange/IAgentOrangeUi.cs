namespace AgentOrange;

public interface IAgentOrangeUi
{
    void WriteWelcome();
    string? ReadUserInput();
    void Write(string text);
    void WriteLine(string text);
    void WriteError(Exception ex);
    void WriteBlankLine();
}
