namespace AgentOrange.Web.Services;

/// <summary>
/// Central service for managing and disposing IAsyncDisposable resources in Blazor Server.
/// Ensures that registered resources are disposed exactly once, either when explicitly requested
/// or when the application/service is disposed (e.g. on circuit shutdown or app exit).
/// Use this to avoid resource leaks for long-lived objects (e.g. chat sessions, clients)
/// that are not directly managed by the Blazor component lifecycle.
/// </summary>
public sealed class ResourceDisposer(ILogger<ResourceDisposer> logger) : IAsyncDisposable
{
    /******************************************************************************************
     * FIELDS
     * ***************************************************************************************/
    readonly List<IAsyncDisposable> _resources = [];
    readonly SemaphoreSlim _semaphore = new(1, 1);
    readonly ILogger<ResourceDisposer> _logger = logger;

    /******************************************************************************************
     * METHODS
     * ***************************************************************************************/
    public void Register(IAsyncDisposable resource) => _resources.Add(resource);

    public ValueTask DisposeResourceAsync(IAsyncDisposable knownResource) => 
        RunSyncAsync(() => TryRemoveAndDisposeAsync(knownResource));

    public async ValueTask DisposeAsync()
    {
        foreach (var res in _resources.ToArray())
        {
            await DisposeResourceAsync(res);
        }
    }

    async ValueTask RunSyncAsync(Func<ValueTask> func)
    {
        try
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            await func();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    async ValueTask TryRemoveAndDisposeAsync(IAsyncDisposable knownResource)
    {
        if (_resources.Remove(knownResource))
        {
            await knownResource.DisposeAsync();
        }
        else
        {
            _logger.LogWarning(
                "Unable to dispose unregistered resource {ResourceType}",
                knownResource.GetType().Name);
        }
    }
}