using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using static System.Threading.Tasks.TaskCreationOptions;

#if !NETCOREAPP2_0_OR_GREATER
// for deconstructing a KeyValuePair instance when enumerating a dictionary:
using Burnside.Core.Extensions;
#endif

namespace AgentOrange.Core.ProcessHandling;

/// <summary>
/// Provides the means to execute a process asynchronously and capture its output.
/// </summary>
public sealed record ProcessRunner(
    string FileName, string Arguments = "", int TimeoutMs = Timeout.Infinite,
    Action<LineOutput>? LineCallback = null, string? WorkingDirectory = null,
    IReadOnlyDictionary<string, string>? Environment = null, ILogger? Logger = null,
    bool UseShellExecute = false, CancellationToken CancellationToken = default)
{
    /******************************************************************************************
     * FIELDS
     * ***************************************************************************************/
    readonly ConcurrentQueue<LineOutput> _outputLines = new();
    readonly TaskCompletionSource<bool> _outputClosed = new(RunContinuationsAsynchronously);
    readonly TaskCompletionSource<bool> _errorClosed = new(RunContinuationsAsynchronously);

    /******************************************************************************************
     * EVENTS
     * ***************************************************************************************/
    /// <summary>
    /// Raised when the process has started successfully.
    /// </summary>
    public event Action<Process>? Started;

    /// <summary>
    /// Raised when the process finished.
    /// </summary>
    public event Action<ProcessResult>? Finished;

    /******************************************************************************************
     * PROPERTIES
     * ***************************************************************************************/
    bool AreStreamsRedirected => !UseShellExecute;

    /******************************************************************************************
     * METHODS
     * ***************************************************************************************/
    public async Task<ProcessResult> RunAsync()
    {
        using var process = CreateProcess();
        return await WaitForProcessResultAsync(process);
    }

    public static Task<ProcessResult> LaunchWithAsync(string fileName, string arguments = "",
        int timeoutMs = Timeout.Infinite, Action<LineOutput>? lineCallback = null,
        string? workingDirectory = null, IReadOnlyDictionary<string, string>? environment = null,
        ILogger? logger = null, bool useShellExecute = false, CancellationToken token = default) =>
        new ProcessRunner(
            fileName, arguments, timeoutMs, lineCallback, workingDirectory,
            environment, logger, useShellExecute, token)
            .RunAsync();

    Process CreateProcess()
    {
        Process? process = null;
        try
        {
            process = new()
            {
                StartInfo = CreateProcessStartInfo(),
            };

            if (AreStreamsRedirected)
            {
                process.OutputDataReceived += OnOutputDataReceived;
                process.ErrorDataReceived += OnErrorDataReceived;
            }

            return process;
        }
        catch
        {
            process?.Dispose();
            throw;
        }
    }

    async Task<ProcessResult> WaitForProcessResultAsync(Process process)
    {
        var startTime = DateTime.UtcNow;
        int? exitCode = null;
        var completed = false;
        var wasTimeout = false;
        ProcessResult res;

        CancellationToken.ThrowIfCancellationRequested();

#if NET
        await
#endif
        using var ctr = CancellationToken.Register(() => Kill(process));
        using var scope = Logger?.BeginScope($"> {FileName} {Arguments}");

        try
        {
            // Start the process. Note: Process.Start() returns false if a new process
            // resource was not created (for example the shell handled the request or an
            // existing instance was reused). That is not always a fatal error.
            if (process.Start())
            {
                // This may be necessary to prevent a race condition where the process
                // couldn't be killed by the cancellation token registration above,
                // right during process start.
                HandleCancellationOf(process);

                Started?.Invoke(process);

                if (AreStreamsRedirected)
                {
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    // Handle extremely short-lived processes: if the process has already exited
                    // immediately after Start(), ensure the stream-completion TCS objects are set
                    // so that WaitForExitAsync + stream tasks can complete.
                    if (process.HasExited)
                    {
                        _outputClosed.TrySetResult(true);
                        _errorClosed.TrySetResult(true);
                    }
                }

#if NETFRAMEWORK
                // Custom Task for WaitForExit
                var waitForExitTask = Task.Run(() =>
                {
                    // This blocks a thread from the ThreadPool until the process exits
                    process.WaitForExit(); 
                    // Ensure output/error streams are completely flushed before marking as complete
                    // This is crucial because WaitForExit can return before all stream data is received.
                    _outputClosed.TrySetResult(true); 
                    _errorClosed.TrySetResult(true);
                }, CancellationToken);
#else
                var waitForExitTask = process.WaitForExitAsync(CancellationToken);
#endif
                Task[] waitTasks = AreStreamsRedirected ?
                    [waitForExitTask, _outputClosed.Task, _errorClosed.Task] : [waitForExitTask];
                var allTasks = Task.WhenAll(waitTasks);
                var delayTask = Task.Delay(TimeoutMs, CancellationToken);
                var completedTask = await Task.WhenAny(delayTask, allTasks);

                HandleCancellationOf(process);

                if (completedTask == allTasks)
                {
                    completed = true;
                    exitCode = process.ExitCode;
                }
                else
                {
                    wasTimeout = delayTask.IsCompleted;
                    Kill(process);
                }
            }
            else
            {
                // If streams are redirected we expect to interact with the process output.
                // In that case Start() == false likely indicates a real failure.
                if (AreStreamsRedirected)
                {
                    AddError("Failed to start process.");
                }
                else
                {
                    // When UseShellExecute == true the shell may have handled the request
                    // (e.g. opening a URL or reusing an application instance). Treat this as info
                    // and mark the operation as completed (no timeout).
                    Logger?.LogInformation("Process.Start returned false — existing process/resource was reused.");
                    completed = true;
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Process execution failed.");
        }
        finally
        {
            var endTime = DateTime.UtcNow;

            res = new(FileName, completed, wasTimeout, exitCode,
                startTime, endTime, _outputLines.ToArray());

            OnFinished(res);
        }

        return res;
    }

    ProcessStartInfo CreateProcessStartInfo()
    {
        var procStartInfo = new ProcessStartInfo
        {
            FileName = FileName,
            Arguments = Arguments,
            UseShellExecute = UseShellExecute,
            RedirectStandardOutput = AreStreamsRedirected,
            RedirectStandardError = AreStreamsRedirected,
            CreateNoWindow = true,
        };

        if (WorkingDirectory != null)
        {
            procStartInfo.WorkingDirectory = WorkingDirectory;
        }

        if (Environment?.Count > 0)
        {
            foreach (var (key, value) in Environment)
            {
                procStartInfo.Environment[key] = value;
            }
        }

        return procStartInfo;
    }

    void HandleCancellationOf(Process process)
    {
        if (!CancellationToken.IsCancellationRequested) { return; }

        Kill(process);
        CancellationToken.ThrowIfCancellationRequested();
    }

    void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data == null)
        {
            _outputClosed.TrySetResult(true);
        }
        else
        {
            AddInfo(e.Data);
        }
    }

    void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data == null)
        {
            _errorClosed.TrySetResult(true);
        }
        else
        {
            AddError(e.Data);
        }
    }

    void OnFinished(ProcessResult res)
    {
        try
        {
            Finished?.Invoke(res);
        }
        catch (Exception ex)
        {
            // Do not let event handler exceptions break the runner; log and continue.
            Logger?.LogWarning(ex, "Finished event handler threw an exception.");
        }
    }

    void AddInfo(string message) => AddLine(message, LogLevel.Info);

    void AddError(string message) => AddLine(message, LogLevel.Error);

    void AddLine(string message, LogLevel level) =>
        AddLine(new(DateTime.UtcNow, level, message));

    void AddLine(LineOutput line)
    {
        _outputLines.Enqueue(line);
        LineCallback?.Invoke(line);
    }

    static void Kill(Process process)
    {
        try
        {
            // This may throw InvalidOperationException when the
            // process was not yet associated with an object
            if (process.HasExited) { return; }

#if NETFRAMEWORK
            process.Kill();
#else
            process.Kill(true); // Kill entire process tree
#endif
        }
        catch
        {
            /* best effort */
        }
    }
}
