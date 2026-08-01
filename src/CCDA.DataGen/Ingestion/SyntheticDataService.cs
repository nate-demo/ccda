using CCDA.DataGen.Evaluation;
using CCDA.DataGen.Generators;
using CCDA.DataGen.Options;
using CCDA.Search.Abstractions;
using CCDA.Search.Indexing;
using CCDA.Shared.Dtos;
using CCDA.Shared.Models;
using CCDA.Shared.Reporting;
using CCDA.Shared.Search;
using Microsoft.Extensions.Logging;

namespace CCDA.DataGen.Ingestion;

/// <summary>
/// Orchestrates the full synthetic-data lifecycle: generate cases, ingest them into the search
/// index, validate retrieval quality, and surface aggregate statistics for the Aspire dashboard.
/// Registered as a singleton so its running totals persist for the dashboard widget.
/// </summary>
public sealed class SyntheticDataService
{
    private readonly ICaseGenerator _generator;
    private readonly CaseIndexer _indexer;
    private readonly ISearchService _search;
    private readonly RagEvaluationSetGenerator _evalGenerator;
    private readonly ILogger<SyntheticDataService> _logger;

    private readonly Lock _gate = new();
    private int _cumulativeCases;
    private int _cumulativeDocuments;
    private int _cumulativePages;
    private double _lastRetrievalScore;
    private DateTimeOffset? _lastGenerationTime;

    public SyntheticDataService(
        ICaseGenerator generator,
        CaseIndexer indexer,
        ISearchService search,
        RagEvaluationSetGenerator evalGenerator,
        ILogger<SyntheticDataService> logger)
    {
        _generator = generator;
        _indexer = indexer;
        _search = search;
        _evalGenerator = evalGenerator;
        _logger = logger;
    }

    /// <summary>Generates and ingests one or more cases, then validates retrieval and returns a report.</summary>
    public async Task<GenerationReport> GenerateAndIngestAsync(
        GenerationRequest request, int count = 1, CancellationToken cancellationToken = default)
    {
        var cases = _generator.GenerateCases(count, request);

        var docs = 0;
        var pages = 0;
        var chunks = 0;
        foreach (var legalCase in cases)
        {
            var result = await _indexer.IndexCaseAsync(legalCase, cancellationToken: cancellationToken);
            docs += result.DocumentCount;
            pages += result.PageCount;
            chunks += result.ChunkCount;
        }

        var (validation, score) = await ValidateRetrievalAsync(cases, cancellationToken);
        var stats = await _search.GetStatsAsync(cancellationToken);

        UpdateStats(cases.Count, docs, pages, score);

        _logger.LogInformation(
            "Generated {Cases} case(s): {Docs} docs, {Pages} pages, {Chunks} chunks. Retrieval: {Validation} ({Score:P0}).",
            cases.Count, docs, pages, chunks, validation, score);

        return new GenerationReport
        {
            CasesGenerated = cases.Count,
            DocumentsGenerated = docs,
            PagesGenerated = pages,
            ChunksCreated = chunks,
            RetrievalValidation = validation,
            IndexStatus = stats.Health == "Healthy" ? "Success" : stats.Health == "Empty" ? "Failed" : "Partial",
            Seed = request.Seed,
            CaseIds = cases.Select(c => c.CaseId).ToList(),
        };
    }

    /// <summary>Maps the public API request to a generation run for a single demo case.</summary>
    public async Task<GenerateDemoDataResponse> GenerateDemoCaseAsync(
        GenerateDemoDataRequest request, CancellationToken cancellationToken = default)
    {
        var genRequest = new GenerationRequest
        {
            CaseType = request.CaseType,
            Complexity = request.Complexity,
            Pages = request.Pages,
            Seed = request.Seed,
        };

        var legalCase = _generator.GenerateCase(genRequest);
        var indexing = await _indexer.IndexCaseAsync(legalCase, cancellationToken: cancellationToken);
        var (validation, score) = await ValidateRetrievalAsync([legalCase], cancellationToken);
        var stats = await _search.GetStatsAsync(cancellationToken);

        UpdateStats(1, indexing.DocumentCount, indexing.PageCount, score);

        var report = new GenerationReport
        {
            CasesGenerated = 1,
            DocumentsGenerated = indexing.DocumentCount,
            PagesGenerated = indexing.PageCount,
            ChunksCreated = indexing.ChunkCount,
            RetrievalValidation = validation,
            IndexStatus = stats.Health == "Healthy" ? "Success" : "Partial",
            Seed = request.Seed,
            CaseIds = [legalCase.CaseId],
        };

        return new GenerateDemoDataResponse
        {
            CaseId = legalCase.CaseId,
            DocumentsGenerated = indexing.DocumentCount,
            PagesGenerated = indexing.PageCount,
            ChunksIndexed = indexing.ChunkCount,
            IndexStatus = report.IndexStatus,
            Report = report,
        };
    }

    /// <summary>Builds a RAG evaluation set for the given cases without indexing (for export/tests).</summary>
    public IReadOnlyList<RagEvaluationItem> BuildEvaluationSet(IEnumerable<LegalCase> cases)
        => _evalGenerator.Generate(cases);

    /// <summary>Snapshot of aggregate statistics for the Aspire "Synthetic Data Generation" widget.</summary>
    public async Task<SyntheticDataStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var index = await _search.GetStatsAsync(cancellationToken);
        lock (_gate)
        {
            return new SyntheticDataStats
            {
                CasesGenerated = _cumulativeCases,
                DocumentsGenerated = _cumulativeDocuments,
                PagesGenerated = _cumulativePages,
                ChunksIndexed = index.ChunkCount,
                RetrievalScore = _lastRetrievalScore,
                IndexHealth = index.Health,
                LastGenerationTime = _lastGenerationTime,
            };
        }
    }

    private async Task<(string Validation, double Score)> ValidateRetrievalAsync(
        IReadOnlyList<LegalCase> cases, CancellationToken cancellationToken)
    {
        var items = _evalGenerator.Generate(cases);
        if (items.Count == 0)
        {
            return ("Not Run", 0d);
        }

        var matches = 0;
        foreach (var item in items)
        {
            var hits = await _search.SearchAsync(
                new SearchRequest { Query = item.Question, CaseId = item.ExpectedCaseId, Top = 3 },
                cancellationToken);

            if (hits.Any(h => h.Chunk.CaseId == item.ExpectedCaseId))
            {
                matches++;
            }
        }

        var score = (double)matches / items.Count;
        return (score >= 0.6 ? "Passed" : "Failed", score);
    }

    private void UpdateStats(int cases, int docs, int pages, double score)
    {
        lock (_gate)
        {
            _cumulativeCases += cases;
            _cumulativeDocuments += docs;
            _cumulativePages += pages;
            _lastRetrievalScore = score;
            _lastGenerationTime = DateTimeOffset.UtcNow;
        }
    }
}
