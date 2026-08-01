using CCDA.AI.DependencyInjection;
using CCDA.DataGen.DependencyInjection;
using CCDA.Search.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CCDA.Tests;

/// <summary>
/// Builds an in-process service provider wiring the CCDA Search, AI, and DataGen pillars with
/// their default offline providers (in-memory hybrid index + local extractive chat), so the
/// integration tests run with no live Azure subscription.
/// </summary>
internal static class TestHost
{
    public static ServiceProvider Build()
    {
        // Empty configuration => every pillar selects its local/offline provider.
        var configuration = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddCcdaSearch(configuration);
        services.AddCcdaAi(configuration);
        services.AddCcdaDataGen();
        return services.BuildServiceProvider();
    }
}
