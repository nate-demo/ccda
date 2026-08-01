using System.Text;
using CCDA.AI.Options;
using CCDA.Search.Abstractions;
using CCDA.Shared.Enums;
using CCDA.Shared.Search;
using Microsoft.Extensions.Options;

namespace CCDA.AI.Grounding;

/// <summary>
/// Turns a query into grounded context by retrieving from the search index and formatting the
/// hits into numbered, citable passages. This is the single place where AI grounding is built,
/// guaranteeing every workflow response can be traced back to specific source pages.
/// </summary>
public sealed class RetrievalGrounder
{
    private readonly ISearchService _search;
    private readonly FoundryOptions _options;

    public RetrievalGrounder(ISearchService search, IOptions<FoundryOptions> options)
    {
        _search = search;
        _options = options.Value;
    }

    public async ValueTask<GroundingContext> GroundAsync(
        string query,
        string? caseId,
        DocumentType? documentType = null,
        SearchMode mode = SearchMode.Hybrid,
        int? top = null,
        CancellationToken cancellationToken = default)
    {
        var request = new SearchRequest
        {
            Query = query,
            CaseId = caseId,
            DocumentType = documentType,
            Top = top ?? _options.MaxContextChunks,
            Mode = mode
        };

        var hits = await _search.SearchAsync(request, cancellationToken);
        if (hits.Count == 0)
        {
            return GroundingContext.Empty;
        }

        var builder = new StringBuilder();
        for (var i = 0; i < hits.Count; i++)
        {
            var hit = hits[i];
            var passage = string.IsNullOrWhiteSpace(hit.Highlight) ? hit.Chunk.Content : hit.Highlight;
            builder.Append('[').Append(i + 1).Append("] (")
                .Append(hit.Chunk.SourceFile).Append(" p.").Append(hit.Chunk.PageNumber).Append(") ")
                .AppendLine(passage.Trim());
        }

        var citations = hits.Select(h => h.ToCitation()).ToList();
        var avg = hits.Count == 0 ? 0d : hits.Average(h => h.Score);

        return new GroundingContext
        {
            Text = builder.ToString().TrimEnd(),
            Citations = citations,
            Hits = hits,
            AverageScore = Math.Clamp(avg, 0d, 1d)
        };
    }
}
