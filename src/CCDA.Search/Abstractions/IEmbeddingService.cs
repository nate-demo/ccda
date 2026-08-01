namespace CCDA.Search.Abstractions;

/// <summary>
/// Produces embedding vectors for text. The default local implementation is deterministic
/// and offline; an Azure OpenAI implementation is selected when configured.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>Dimensionality of vectors produced by this service.</summary>
    int Dimensions { get; }

    /// <summary>Name of the active provider (for diagnostics / dashboard).</summary>
    string ProviderName { get; }

    ValueTask<float[]> GenerateAsync(string text, CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<float[]>> GenerateBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default);
}
