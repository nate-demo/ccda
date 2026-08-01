using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using CCDA.Search.Abstractions;
using CCDA.Search.Options;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;

namespace CCDA.Search.Embeddings;

/// <summary>
/// Azure OpenAI / Azure AI Foundry backed embedding service. Selected when
/// <c>Ccda:Embeddings:Provider = AzureOpenAI</c> and an endpoint is configured.
/// Uses an API key when supplied, otherwise DefaultAzureCredential (managed identity / dev login).
/// </summary>
public sealed class AzureOpenAIEmbeddingService : IEmbeddingService
{
    private readonly EmbeddingClient _client;

    public AzureOpenAIEmbeddingService(IOptions<EmbeddingOptions> options)
    {
        var opts = options.Value;
        Dimensions = opts.Dimensions;

        var endpoint = new Uri(opts.Endpoint!);
        var azureClient = string.IsNullOrWhiteSpace(opts.ApiKey)
            ? new AzureOpenAIClient(endpoint, new DefaultAzureCredential())
            : new AzureOpenAIClient(endpoint, new AzureKeyCredential(opts.ApiKey));

        _client = azureClient.GetEmbeddingClient(opts.Deployment);
    }

    public int Dimensions { get; }

    public string ProviderName => "Azure OpenAI";

    public async ValueTask<float[]> GenerateAsync(string text, CancellationToken cancellationToken = default)
    {
        var response = await _client.GenerateEmbeddingAsync(text, cancellationToken: cancellationToken);
        return response.Value.ToFloats().ToArray();
    }

    public async ValueTask<IReadOnlyList<float[]>> GenerateBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        if (texts.Count == 0)
        {
            return [];
        }

        var response = await _client.GenerateEmbeddingsAsync(texts, cancellationToken: cancellationToken);
        return response.Value.Select(e => e.ToFloats().ToArray()).ToArray();
    }
}
