namespace AgentOrange;

public interface IAgentOrangeUi
{
    void WriteWelcome();
    string? ReadUserInput();
    void WriteResponse(string response);
    void WriteError(Exception ex);
    void WriteBlankLine();
}
