using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using CCDA.AI.Abstractions;
using CCDA.AI.Options;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace CCDA.AI.Providers;

/// <summary>
/// Azure AI Foundry / Azure OpenAI chat-completion provider. Selected when
/// <c>Azure:Foundry:Provider = AzureOpenAI</c> and an endpoint is configured. Uses an API key
/// when supplied, otherwise DefaultAzureCredential (managed identity / developer login).
/// </summary>
public sealed class AzureOpenAIChatCompletionService : IChatCompletionService
{
    private readonly ChatClient _client;

    public AzureOpenAIChatCompletionService(IOptions<FoundryOptions> options)
    {
        var opts = options.Value;
        var endpoint = new Uri(opts.Endpoint!);
        var azureClient = string.IsNullOrWhiteSpace(opts.ApiKey)
            ? new AzureOpenAIClient(endpoint, new DefaultAzureCredential())
            : new AzureOpenAIClient(endpoint, new AzureKeyCredential(opts.ApiKey));

        _client = azureClient.GetChatClient(opts.ChatDeployment);
    }

    public string ProviderName => "Azure AI Foundry";

    public async ValueTask<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        var messages = new ChatMessage[]
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        };

        var options = new ChatCompletionOptions { Temperature = 0.2f };
        var completion = await _client.CompleteChatAsync(messages, options, cancellationToken);

        return completion.Value.Content.Count > 0
            ? completion.Value.Content[0].Text
            : string.Empty;
    }
}
