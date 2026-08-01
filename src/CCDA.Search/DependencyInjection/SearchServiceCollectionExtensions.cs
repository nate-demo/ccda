using CCDA.Search.Abstractions;
using CCDA.Search.Embeddings;
using CCDA.Search.Indexing;
using CCDA.Search.Options;
using CCDA.Search.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CCDA.Search.DependencyInjection;

/// <summary>Registers the CCDA search pillar with config-driven provider selection.</summary>
public static class SearchServiceCollectionExtensions
{
    /// <summary>
    /// Adds the embedding + search services. Defaults to fully offline providers
    /// (deterministic embeddings + in-memory hybrid index). Configure
    /// <c>Ccda:Embeddings:Provider = AzureOpenAI</c> and/or <c>Azure:Search:Endpoint</c>
    /// to switch to the Azure-backed implementations.
    /// </summary>
    public static IServiceCollection AddCcdaSearch(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<EmbeddingOptions>()
            .Bind(configuration.GetSection(EmbeddingOptions.SectionName));

        services.AddOptions<AzureSearchOptions>()
            .Bind(configuration.GetSection(AzureSearchOptions.SectionName));

        var embeddingOptions = new EmbeddingOptions();
        configuration.GetSection(EmbeddingOptions.SectionName).Bind(embeddingOptions);

        if (embeddingOptions.UseAzure)
        {
            services.AddSingleton<IEmbeddingService, AzureOpenAIEmbeddingService>();
        }
        else
        {
            services.AddSingleton<IEmbeddingService, LocalEmbeddingService>();
        }

        var searchOptions = new AzureSearchOptions();
        configuration.GetSection(AzureSearchOptions.SectionName).Bind(searchOptions);

        if (searchOptions.UseAzure)
        {
            services.AddSingleton<ISearchService, AzureAiSearchService>();
        }
        else
        {
            services.AddSingleton<ISearchService, InMemorySearchService>();
        }

        services.TryAddSingleton<ICaseCatalog, InMemoryCaseCatalog>();
        services.TryAddSingleton<CaseIndexer>();

        return services;
    }
}
