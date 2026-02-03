// ReSharper disable NotAccessedPositionalProperty.Global
namespace AgentOrange.Core.ProcessHandling;

public sealed record ProcessResult(
    string FileName,
    bool HasCompleted,
    bool WasTimeout,
    int? ExitCode,
    DateTime StartTime,
    DateTime EndTime,
    IReadOnlyList<LineOutput> Lines)
{
    public TimeSpan Duration => EndTime - StartTime;
    public bool WasKilled => !HasCompleted;

    public string GetCombinedOutput() =>
        string.Join(Environment.NewLine, Lines.Select(l => l.Text));

    public string GetFormattedOutput() =>
        string.Join(Environment.NewLine, Lines.Select(
            l => $"[{l.LocalTimestamp:HH:mm:ss}] [{l.Level}] {l.Text}"
        ));
}
