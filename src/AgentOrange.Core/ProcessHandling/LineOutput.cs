namespace AgentOrange.Core.ProcessHandling;

public record LineOutput(DateTime Timestamp, LogLevel Level, string Text)
{
    public DateTime LocalTimestamp => Timestamp.ToLocalTime();
    public Microsoft.Extensions.Logging.LogLevel MsLogLevel => Level == LogLevel.Info ?
        Microsoft.Extensions.Logging.LogLevel.Information : 
        Microsoft.Extensions.Logging.LogLevel.Error;
}
