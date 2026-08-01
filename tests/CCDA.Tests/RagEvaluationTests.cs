using CCDA.DataGen.Ingestion;
using CCDA.DataGen.Options;
using CCDA.Shared.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace CCDA.Tests;

/// <summary>
/// A RAG smoke test: generate + ingest cases, then confirm the retrieval-quality gate passes.
/// The <see cref="SyntheticDataService"/> builds an evaluation set from the generated cases and
/// checks that each expected case is retrievable, mirroring the CLI's <c>generate-rag-eval</c>.
/// </summary>
public sealed class RagEvaluationTests : IDisposable
{
    private readonly ServiceProvider _services = TestHost.Build();

    [Fact]
    public async Task GenerateAndIngest_PassesRetrievalGate()
    {
        var dataService = _services.GetRequiredService<SyntheticDataService>();

        var report = await dataService.GenerateAndIngestAsync(
            new GenerationRequest { Seed = 2026, Complexity = CaseComplexity.Medium }, count: 2);

        Assert.Equal(2, report.CasesGenerated);
        Assert.True(report.DocumentsGenerated > 0);
        Assert.True(report.ChunksCreated > 0);
        Assert.Equal("Passed", report.RetrievalValidation);
        Assert.Equal("Success", report.IndexStatus);
        Assert.Equal(2, report.CaseIds.Count);
    }

    [Fact]
    public async Task EvaluationSet_TargetsGeneratedCases()
    {
        var dataService = _services.GetRequiredService<SyntheticDataService>();
        var generator = _services.GetRequiredService<CCDA.DataGen.Generators.ICaseGenerator>();

        var cases = generator.GenerateCases(2, new GenerationRequest { Seed = 55 });
        var items = dataService.BuildEvaluationSet(cases);

        Assert.NotEmpty(items);
        var caseIds = cases.Select(c => c.CaseId).ToHashSet();
        Assert.All(items, i =>
        {
            Assert.Contains(i.ExpectedCaseId, caseIds);
            Assert.False(string.IsNullOrWhiteSpace(i.Question));
        });
    }

    public void Dispose() => _services.Dispose();
}
