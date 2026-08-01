using CCDA.Shared.Constants;

namespace CCDA.Search.Options;

/// <summary>Selects and configures the embedding provider.</summary>
public sealed class EmbeddingOptions
{
    public const string SectionName = "Ccda:Embeddings";

    /// <summary>"Local" (default, offline) or "AzureOpenAI".</summary>
    public string Provider { get; set; } = "Local";

    public int Dimensions { get; set; } = CcdaConstants.DefaultEmbeddingDimensions;

    /// <summary>Azure OpenAI / Foundry endpoint (only used when Provider = AzureOpenAI).</summary>
    public string? Endpoint { get; set; }

    /// <summary>API key. When empty and an endpoint is set, DefaultAzureCredential is used.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Embedding deployment name, e.g. "text-embedding-3-small".</summary>
    public string Deployment { get; set; } = "text-embedding-3-small";

    public bool UseAzure =>
        string.Equals(Provider, "AzureOpenAI", StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(Endpoint);
}

/// <summary>Selects and configures the search provider.</summary>
public sealed class AzureSearchOptions
{
    public const string SectionName = CcdaConstants.AzureSearchSection;

    /// <summary>Azure AI Search endpoint. When empty, the in-memory provider is used.</summary>
    public string? Endpoint { get; set; }

    /// <summary>Admin/query key. When empty and an endpoint is set, DefaultAzureCredential is used.</summary>
    public string? ApiKey { get; set; }

    public string IndexName { get; set; } = CcdaConstants.SearchIndexName;

    public bool UseAzure => !string.IsNullOrWhiteSpace(Endpoint);
}
