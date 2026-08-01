using CCDA.DataGen.Evaluation;
using CCDA.DataGen.Generators;
using CCDA.DataGen.Options;
using CCDA.Search.Abstractions;
using CCDA.Search.Indexing;
using CCDA.Shared.Enums;
using CCDA.Shared.Search;
using Microsoft.Extensions.DependencyInjection;

namespace CCDA.Tests;

/// <summary>
/// Exercises the search pillar end to end against the offline in-memory hybrid index: a
/// generated case is ingested, then retrieval must return scored, case-scoped, page-citable hits.
/// </summary>
public sealed class SearchServiceTests : IDisposable
{
    private readonly ServiceProvider _services = TestHost.Build();

    [Fact]
    public async Task Ingest_ThenSearch_ReturnsCaseScopedCitableHits()
    {
        var (search, caseId, question) = await IngestSeedCaseAsync();

        var hits = await search.SearchAsync(
            new SearchRequest { Query = question, CaseId = caseId, Top = 3 });

        Assert.NotEmpty(hits);
        Assert.All(hits, h => Assert.Equal(caseId, h.Chunk.CaseId));

        var citation = hits[0].ToCitation();
        Assert.Equal(caseId, citation.CaseId);
        Assert.True(citation.PageNumber >= 1, "citation must point at a real page");
        Assert.False(string.IsNullOrWhiteSpace(citation.SourceFile));
        Assert.True(hits[0].Score > 0, "top hit should have a positive fused score");
    }

    [Fact]
    public async Task Search_IsScopedToRequestedCase()
    {
        var indexer = _services.GetRequiredService<CaseIndexer>();
        var generator = _services.GetRequiredService<ICaseGenerator>();
        var search = _services.GetRequiredService<ISearchService>();

        var caseA = generator.GenerateCase(new GenerationRequest { Seed = 100 });
        var caseB = generator.GenerateCase(new GenerationRequest { Seed = 200 });
        await indexer.IndexCaseAsync(caseA);
        await indexer.IndexCaseAsync(caseB);

        var hits = await search.SearchAsync(
            new SearchRequest { Query = caseA.Title, CaseId = caseA.CaseId, Top = 5 });

        Assert.NotEmpty(hits);
        Assert.DoesNotContain(hits, h => h.Chunk.CaseId == caseB.CaseId);
    }

    [Fact]
    public async Task Stats_ReflectIngestedContent()
    {
        var (search, _, _) = await IngestSeedCaseAsync();

        var stats = await search.GetStatsAsync();

        Assert.Equal("In-Memory Hybrid", search.ProviderName);
        Assert.True(stats.ChunkCount > 0);
        Assert.Equal("Healthy", stats.Health);
    }

    [Fact]
    public async Task DocumentTypeFilter_RestrictsResults()
    {
        var (search, caseId, _) = await IngestSeedCaseAsync();

        var chunks = await search.GetCaseChunksAsync(caseId);
        var targetType = chunks[0].DocumentType;

        var hits = await search.SearchAsync(new SearchRequest
        {
            Query = "report",
            CaseId = caseId,
            DocumentType = targetType,
            Top = 10,
        });

        Assert.All(hits, h => Assert.Equal(targetType, h.Chunk.DocumentType));
    }

    private async Task<(ISearchService Search, string CaseId, string Question)> IngestSeedCaseAsync()
    {
        var generator = _services.GetRequiredService<ICaseGenerator>();
        var indexer = _services.GetRequiredService<CaseIndexer>();
        var search = _services.GetRequiredService<ISearchService>();
        var evalGenerator = _services.GetRequiredService<RagEvaluationSetGenerator>();

        var legalCase = generator.GenerateCase(new GenerationRequest { Seed = 42, Complexity = CaseComplexity.Medium });
        await indexer.IndexCaseAsync(legalCase);

        var question = evalGenerator.Generate([legalCase]).First().Question;
        return (search, legalCase.CaseId, question);
    }

    public void Dispose() => _services.Dispose();
}
