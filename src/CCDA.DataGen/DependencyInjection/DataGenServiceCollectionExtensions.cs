using CCDA.DataGen.Evaluation;
using CCDA.DataGen.Generators;
using CCDA.DataGen.Ingestion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CCDA.DataGen.DependencyInjection;

/// <summary>Registers the synthetic data generation pillar.</summary>
public static class DataGenServiceCollectionExtensions
{
    /// <summary>
    /// Adds the case generator, RAG evaluation generator, and the ingestion orchestrator.
    /// Assumes <c>AddCcdaSearch</c> has already registered the search + indexing services.
    /// </summary>
    public static IServiceCollection AddCcdaDataGen(this IServiceCollection services)
    {
        services.TryAddSingleton<ICaseGenerator, CaseGenerator>();
        services.TryAddSingleton<RagEvaluationSetGenerator>();
        services.TryAddSingleton<SyntheticDataService>();
        return services;
    }
}
