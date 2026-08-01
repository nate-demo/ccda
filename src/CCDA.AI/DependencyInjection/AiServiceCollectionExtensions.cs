using CCDA.AI.Abstractions;
using CCDA.AI.Grounding;
using CCDA.AI.Options;
using CCDA.AI.Providers;
using CCDA.AI.Workflows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CCDA.AI.DependencyInjection;

/// <summary>Registers the CCDA AI orchestration pillar with config-driven provider selection.</summary>
public static class AiServiceCollectionExtensions
{
    /// <summary>
    /// Adds the chat provider + AI orchestrator. Defaults to the offline extractive provider.
    /// Configure <c>Azure:Foundry:Provider = AzureOpenAI</c> with an endpoint to use Azure AI Foundry.
    /// Assumes <c>AddCcdaSearch</c> has already registered the search + embedding services.
    /// </summary>
    public static IServiceCollection AddCcdaAi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<FoundryOptions>()
            .Bind(configuration.GetSection(FoundryOptions.SectionName));

        var foundryOptions = new FoundryOptions();
        configuration.GetSection(FoundryOptions.SectionName).Bind(foundryOptions);

        if (foundryOptions.UseAzure)
        {
            services.AddSingleton<IChatCompletionService, AzureOpenAIChatCompletionService>();
        }
        else
        {
            services.AddSingleton<IChatCompletionService, LocalChatCompletionService>();
        }

        services.AddSingleton<RetrievalGrounder>();
        services.AddSingleton<IAiOrchestrator, AiOrchestrator>();

        return services;
    }
}
