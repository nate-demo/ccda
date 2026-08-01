namespace CCDA.Shared.Constants;

/// <summary>
/// Central constants for the CCDA demonstration platform.
/// All CCDA content is fictional and for demonstration purposes only.
/// </summary>
public static class CcdaConstants
{
    /// <summary>Classification stamped on every document / chunk / metadata record.</summary>
    public const string Classification = "Demo Data";

    /// <summary>Mandatory fictional-data watermark lines applied to every generated document.</summary>
    public const string DemoWatermarkLine1 = "FOR DEMONSTRATION PURPOSES ONLY";

    public const string DemoWatermarkLine2 = "FICTIONAL CASE DATA";

    /// <summary>Combined watermark banner.</summary>
    public static string DemoWatermark { get; } =
        $"{DemoWatermarkLine1}{Environment.NewLine}{DemoWatermarkLine2}";

    /// <summary>Default Azure AI Search index name.</summary>
    public const string SearchIndexName = "ccda-cases";

    /// <summary>Embedding vector dimensions (matches text-embedding-3-small / ada-002 family).</summary>
    public const int DefaultEmbeddingDimensions = 1536;

    /// <summary>Prefix for generated case identifiers, e.g. CCDA-2026-000123.</summary>
    public const string CaseIdPrefix = "CCDA";

    // ---- Configuration section keys (drive local-vs-Azure provider selection) ----
    public const string AzureSearchSection = "Azure:Search";
    public const string AzureFoundrySection = "Azure:Foundry";
}
