namespace AgentOrange;

sealed record PreconditionCheckResult(bool DotnetAvailable, string? DotnetVersion);

static class PreconditionChecker
{
    public static async Task ValidateOrThrowAsync()
    {
        var result = await CheckAsync();
        if (!result.DotnetAvailable)
            throw new InvalidOperationException("dotnet CLI is required but not found on this system.");
    }

    public static async Task<PreconditionCheckResult> CheckAsync()
    {
        var check = await Core.ProcessHandling.ProcessRunner.LaunchWithAsync("dotnet", "--version");
        var available = check.ExitCode == 0;
        var version = available ? check.GetCombinedOutput().Trim() : null;
        return new(available, version);
    }
}
