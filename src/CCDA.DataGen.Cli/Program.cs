using System.Text.Json;
using System.Text.Json.Serialization;
using CCDA.DataGen.DependencyInjection;
using CCDA.DataGen.Evaluation;
using CCDA.DataGen.Generators;
using CCDA.DataGen.Ingestion;
using CCDA.DataGen.Options;
using CCDA.Search.DependencyInjection;
using CCDA.Shared.Constants;
using CCDA.Shared.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CCDA.DataGen.Cli;

/// <summary>
/// Command-line front end for the CCDA synthetic case generator. Mirrors the Copilot Studio /
/// GitHub agent slash-commands so demos can generate cases from a terminal. All output is
/// fictional and watermarked FOR DEMONSTRATION PURPOSES ONLY.
/// </summary>
internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || IsHelp(args[0]))
        {
            PrintHelp();
            return 0;
        }

        var command = args[0].TrimStart('/').ToLowerInvariant();
        var options = ParseOptions(args.Skip(1));

        var services = BuildServices();
        var synthetic = services.GetRequiredService<SyntheticDataService>();

        Console.WriteLine(CcdaConstants.DemoWatermark);
        Console.WriteLine();

        try
        {
            return command switch
            {
                "generate-case" or "single" => await GenerateCasesAsync(synthetic, options, count: 1),
                "generate-cases" or "bulk" => await GenerateCasesAsync(synthetic, options, count: GetInt(options, "count", 3)),
                "generate-inmate-history" or "inmate-history" =>
                    await GenerateCasesAsync(synthetic, options, count: 1, inmateHistory: true),
                "generate-investigation" or "investigation" =>
                    await GenerateCasesAsync(synthetic, options, count: 1, investigation: true),
                "generate-rag-eval" or "rag-eval" => await GenerateRagEvalAsync(services, options),
                _ => Unknown(command),
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> GenerateCasesAsync(
        SyntheticDataService synthetic,
        IReadOnlyDictionary<string, string> options,
        int count,
        bool investigation = false,
        bool inmateHistory = false)
    {
        var request = new GenerationRequest
        {
            CaseType = ParseEnum<CaseType>(GetString(options, "type")),
            Complexity = ParseEnum<CaseComplexity>(GetString(options, "complexity")) ?? CaseComplexity.Medium,
            Pages = TryGetInt(options, "pages"),
            Seed = TryGetInt(options, "seed"),
            IncludeInvestigationPackage = investigation,
            IncludeInmateHistory = inmateHistory,
        };

        var report = await synthetic.GenerateAndIngestAsync(request, count);

        Console.WriteLine($"Cases generated : {report.CasesGenerated}");
        Console.WriteLine($"Documents       : {report.DocumentsGenerated}");
        Console.WriteLine($"Pages           : {report.PagesGenerated}");
        Console.WriteLine($"Chunks indexed  : {report.ChunksCreated}");
        Console.WriteLine($"Retrieval check : {report.RetrievalValidation}");
        Console.WriteLine($"Index status    : {report.IndexStatus}");
        Console.WriteLine($"Case IDs        : {string.Join(", ", report.CaseIds)}");

        await WriteOutputAsync(options, report);
        return 0;
    }

    private static async Task<int> GenerateRagEvalAsync(
        IServiceProvider services, IReadOnlyDictionary<string, string> options)
    {
        var generator = services.GetRequiredService<ICaseGenerator>();
        var evalGenerator = services.GetRequiredService<RagEvaluationSetGenerator>();

        var request = new GenerationRequest
        {
            CaseType = ParseEnum<CaseType>(GetString(options, "type")),
            Complexity = ParseEnum<CaseComplexity>(GetString(options, "complexity")) ?? CaseComplexity.Medium,
            Seed = TryGetInt(options, "seed"),
        };

        var cases = generator.GenerateCases(GetInt(options, "count", 3), request);
        var items = evalGenerator.Generate(cases);

        Console.WriteLine($"Cases generated : {cases.Count}");
        Console.WriteLine($"Eval questions  : {items.Count}");
        foreach (var item in items.Take(10))
        {
            Console.WriteLine($"  Q: {item.Question}  ->  {item.ExpectedSourceFile}");
        }

        await WriteOutputAsync(options, items);
        return 0;
    }

    private static async Task WriteOutputAsync(IReadOnlyDictionary<string, string> options, object payload)
    {
        var output = GetString(options, "output");
        if (string.IsNullOrWhiteSpace(output))
        {
            return;
        }

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        await File.WriteAllTextAsync(output, json);
        Console.WriteLine($"Output written  : {output}");
    }

    private static ServiceProvider BuildServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(b => b.AddSimpleConsole(o => o.SingleLine = true).SetMinimumLevel(LogLevel.Warning));
        services.AddCcdaSearch(configuration);
        services.AddCcdaDataGen();
        return services.BuildServiceProvider();
    }

    private static int Unknown(string command)
    {
        Console.Error.WriteLine($"Unknown command '{command}'.");
        PrintHelp();
        return 1;
    }

    private static bool IsHelp(string arg) =>
        arg is "-h" or "--help" or "help" or "/?";

    private static void PrintHelp()
    {
        Console.WriteLine("CCDA Synthetic Case Generator");
        Console.WriteLine("All generated data is fictional \u2014 FOR DEMONSTRATION PURPOSES ONLY.");
        Console.WriteLine();
        Console.WriteLine("Usage: ccda-datagen <command> [options]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  generate-case             Generate and index a single case");
        Console.WriteLine("  generate-cases            Generate and index multiple cases (--count)");
        Console.WriteLine("  generate-investigation    Generate a case with an investigation package");
        Console.WriteLine("  generate-inmate-history   Generate a case with a custody/inmate history package");
        Console.WriteLine("  generate-rag-eval         Generate cases and a RAG evaluation set");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --count <n>          Number of cases (bulk / rag-eval)");
        Console.WriteLine("  --type <CaseType>    Fraud|Burglary|Assault|Theft|DrugOffense|Homicide|Cybercrime|...");
        Console.WriteLine("  --complexity <lvl>   Low|Medium|High");
        Console.WriteLine("  --pages <n>          Target total page volume");
        Console.WriteLine("  --seed <n>           Deterministic seed for reproducible output");
        Console.WriteLine("  --output <path>      Write JSON output to a file");
    }

    private static Dictionary<string, string> ParseOptions(IEnumerable<string> args)
    {
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? key = null;
        foreach (var arg in args)
        {
            if (arg.StartsWith("--", StringComparison.Ordinal))
            {
                key = arg[2..];
                options[key] = "true";
            }
            else if (key is not null)
            {
                options[key] = arg;
                key = null;
            }
        }

        return options;
    }

    private static string? GetString(IReadOnlyDictionary<string, string> o, string key) =>
        o.TryGetValue(key, out var v) && v != "true" ? v : null;

    private static int? TryGetInt(IReadOnlyDictionary<string, string> o, string key) =>
        GetString(o, key) is { } s && int.TryParse(s, out var v) ? v : null;

    private static int GetInt(IReadOnlyDictionary<string, string> o, string key, int fallback) =>
        TryGetInt(o, key) ?? fallback;

    private static TEnum? ParseEnum<TEnum>(string? value) where TEnum : struct, Enum =>
        value is not null && Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed) ? parsed : null;
}
