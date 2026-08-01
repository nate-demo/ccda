using CCDA.Shared.Models;
using CCDA.Shared.Search;

namespace CCDA.Search.Abstractions;

/// <summary>
/// The case retrieval index abstraction. Backed by an in-memory hybrid index by default,
/// or Azure AI Search when configured. All results carry full page-level citation lineage.
/// </summary>
public interface ISearchService
{
    /// <summary>Name of the active provider (for diagnostics / dashboard).</summary>
    string ProviderName { get; }

    /// <summary>Creates the index if it does not already exist.</summary>
    ValueTask EnsureIndexAsync(CancellationToken cancellationToken = default);

    /// <summary>Indexes a batch of chunks; returns the number of chunks written.</summary>
    ValueTask<int> IndexAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default);

    /// <summary>Runs a retrieval query and returns scored, citable hits.</summary>
    ValueTask<IReadOnlyList<SearchHit>> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default);

    /// <summary>Returns every indexed chunk for a case (ordered by document then page).</summary>
    ValueTask<IReadOnlyList<DocumentChunk>> GetCaseChunksAsync(string caseId, CancellationToken cancellationToken = default);

    /// <summary>Removes all chunks for a case; returns the number removed.</summary>
    ValueTask<int> DeleteCaseAsync(string caseId, CancellationToken cancellationToken = default);

    /// <summary>Index health &amp; volume statistics.</summary>
    ValueTask<SearchIndexStats> GetStatsAsync(CancellationToken cancellationToken = default);
}
