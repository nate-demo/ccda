using CCDA.Search.Abstractions;
using CCDA.Search.Chunking;
using CCDA.Shared.Models;
using Microsoft.Extensions.Logging;

namespace CCDA.Search.Indexing;

/// <summary>Result of ingesting a case into the search index.</summary>
public sealed record IndexingResult(string CaseId, int DocumentCount, int PageCount, int ChunkCount);

/// <summary>
/// Coordinates chunking + indexing of a whole case. This is the ingestion entry point used
/// by both the API (<c>/case/upload</c>) and the synthetic data generator's auto-ingestion.
/// </summary>
public sealed class CaseIndexer
{
    private readonly ISearchService _search;
    private readonly ICaseCatalog _catalog;
    private readonly ILogger<CaseIndexer> _logger;

    public CaseIndexer(ISearchService search, ICaseCatalog catalog, ILogger<CaseIndexer> logger)
    {
        _search = search;
        _catalog = catalog;
        _logger = logger;
    }

    public async ValueTask<IndexingResult> IndexCaseAsync(
        LegalCase legalCase,
        ChunkingOptions? chunkingOptions = null,
        CancellationToken cancellationToken = default)
    {
        await _search.EnsureIndexAsync(cancellationToken);

        var chunks = TextChunker.ChunkCase(legalCase, chunkingOptions);
        var indexed = await _search.IndexAsync(chunks, cancellationToken);

        // Retain case metadata so the API can serve case detail without re-reading pages.
        _catalog.Record(legalCase);

        _logger.LogInformation(
            "Indexed case {CaseId}: {Documents} docs, {Pages} pages, {Chunks} chunks.",
            legalCase.CaseId, legalCase.DocumentCount, legalCase.PageCount, indexed);

        return new IndexingResult(legalCase.CaseId, legalCase.DocumentCount, legalCase.PageCount, indexed);
    }
}
