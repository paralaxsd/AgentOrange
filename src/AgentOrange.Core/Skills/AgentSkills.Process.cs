using AgentOrange.Core.ProcessHandling;
using System.ComponentModel;

namespace AgentOrange.Core.Skills;

sealed partial class AgentSkills
{
    [Description("Führt einen Prozess/Shell-Befehl aus und gibt das Ergebnis zurück.")]
    public async Task<string> RunProcess(string fileName, string arguments = "", 
        int timeoutMs = 30000, string? workingDir = null)
    {
        var runner = new ProcessRunner(fileName, arguments, WorkingDirectory:workingDir, TimeoutMs: timeoutMs);
        var result = await runner.RunAsync();

        if (!result.HasCompleted)
            return $"Prozess nicht erfolgreich beendet (Timeout: {result.WasTimeout}, ExitCode: {result.ExitCode})";

        var output = string.Join("\n", result.Lines.Select(line => $"[{line.Level}] {line.Text}"));
        return $"ExitCode: {result.ExitCode}\n{output}";
    }
}
