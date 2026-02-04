using Microsoft.Extensions.AI;

namespace AgentOrange.Core.Embedding;

public sealed class AgentOrangeEmbeddingGenerator
    : IEmbeddingGenerator<string, Embedding<float>>, IDisposable
{
    public async Task<Embedding<float>> GenerateEmbeddingAsync(string input, CancellationToken cancellationToken = default)
    {
        // TODO: Hier je nach Provider (config.Provider) die passende API/Logik aufrufen
        // Aktuell: Dummy-Embedding (nur als Platzhalter)
        await Task.Yield();
        var vector = Enumerable.Repeat(0.0f, 384).ToArray();
        return new Embedding<float>(vector);
    }

    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(IEnumerable<string> values, EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = new CancellationToken())
    {
        // TODO: Hier je nach Provider (config.Provider) die passende API/Logik aufrufen
        // Aktuell: Dummy-Embedding (nur als Platzhalter)
        await Task.Yield();
        var embedding = await GenerateEmbeddingAsync(values.First(), cancellationToken);
        return new([embedding]);
    }

    public void Dispose() { }
    public object? GetService(Type serviceType, object? serviceKey = null) => throw new NotImplementedException();
}
