namespace CCDA.AI.Abstractions;

/// <summary>
/// Minimal chat-completion abstraction. The default local implementation is a deterministic
/// extractive summarizer (fully offline); the Azure implementation calls Azure AI Foundry /
/// Azure OpenAI. The orchestrator always derives citations from retrieval — never from the
/// model — so grounding is provider-independent.
/// </summary>
public interface IChatCompletionService
{
    string ProviderName { get; }

    ValueTask<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);
}
