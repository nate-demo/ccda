using CCDA.Shared.Constants;

namespace CCDA.AI.Options;

/// <summary>Selects and configures the chat/LLM provider (Azure AI Foundry / Azure OpenAI).</summary>
public sealed class FoundryOptions
{
    public const string SectionName = CcdaConstants.AzureFoundrySection;

    /// <summary>"Local" (default, offline extractive) or "AzureOpenAI".</summary>
    public string Provider { get; set; } = "Local";

    /// <summary>Azure AI Foundry / Azure OpenAI endpoint (used when Provider = AzureOpenAI).</summary>
    public string? Endpoint { get; set; }

    /// <summary>API key. When empty and an endpoint is set, DefaultAzureCredential is used.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Chat deployment name, e.g. "gpt-4o-mini".</summary>
    public string ChatDeployment { get; set; } = "gpt-4o-mini";

    /// <summary>Maximum number of retrieved chunks used to ground a response.</summary>
    public int MaxContextChunks { get; set; } = 8;

    public bool UseAzure =>
        string.Equals(Provider, "AzureOpenAI", StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(Endpoint);
}
