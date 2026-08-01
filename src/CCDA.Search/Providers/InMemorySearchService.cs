using System.Collections.Concurrent;
using CCDA.Search.Abstractions;
using CCDA.Search.Embeddings;
using CCDA.Shared.Enums;
using CCDA.Shared.Models;
using CCDA.Shared.Search;
using Microsoft.Extensions.Logging;

namespace CCDA.Search.Providers;

/// <summary>
/// An offline hybrid search index held in memory. It fuses a lexical (keyword/BM25-style)
/// score with vector cosine similarity to approximate Azure AI Search hybrid retrieval,
/// so the full citation-first demo runs with no cloud dependency. Chunk vectors are
/// produced by the configured <see cref="IEmbeddingService"/>.
/// </summary>
public sealed class InMemorySearchService : ISearchService
{
    private const double VectorWeight = 0.6;
    private const double KeywordWeight = 0.4;

    private readonly ConcurrentDictionary<string, DocumentChunk> _chunks = new(StringComparer.Ordinal);
    private readonly IEmbeddingService _embeddings;
    private readonly ILogger<InMemorySearchService> _logger;

    public InMemorySearchService(IEmbeddingService embeddings, ILogger<InMemorySearchService> logger)
    {
        _embeddings = embeddings;
        _logger = logger;
    }

    public string ProviderName => "In-Memory Hybrid";

    public ValueTask EnsureIndexAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

    public async ValueTask<int> IndexAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        var count = 0;
        foreach (var chunk in chunks)
        {
            if (chunk.Embedding.Length == 0)
            {
                chunk.Embedding = await _embeddings.GenerateAsync(chunk.Content, cancellationToken);
            }

            _chunks[chunk.ChunkId] = chunk;
            count++;
        }

        _logger.LogInformation("Indexed {Count} chunks (total {Total}).", count, _chunks.Count);
        return count;
    }

    public async ValueTask<IReadOnlyList<SearchHit>> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Query) || _chunks.IsEmpty)
        {
            return [];
        }

        var candidates = _chunks.Values.Where(c =>
            (request.CaseId is null || string.Equals(c.CaseId, request.CaseId, StringComparison.OrdinalIgnoreCase))
            && (request.DocumentType is null || c.DocumentType == request.DocumentType))
            .ToList();

        if (candidates.Count == 0)
        {
            return [];
        }

        var queryTerms = Tokenize(request.Query);
        var queryVector = request.Mode is SearchMode.Keyword
            ? []
            : await _embeddings.GenerateAsync(request.Query, cancellationToken);

        var scored = new List<SearchHit>(candidates.Count);
        foreach (var chunk in candidates)
        {
            var keyword = KeywordScore(chunk.Content, queryTerms);
            var vector = queryVector.Length == 0
                ? 0d
                : Math.Clamp((VectorMath.CosineSimilarity(queryVector, chunk.Embedding) + 1d) / 2d, 0d, 1d);

            var score = request.Mode switch
            {
                SearchMode.Keyword => keyword,
                SearchMode.Vector => vector,
                _ => (VectorWeight * vector) + (KeywordWeight * keyword)
            };

            if (score <= 0d)
            {
                continue;
            }

            scored.Add(new SearchHit
            {
                Chunk = chunk,
                Score = Math.Round(score, 4),
                Highlight = BuildHighlight(chunk.Content, queryTerms)
            });
        }

        return scored
            .OrderByDescending(h => h.Score)
            .Take(Math.Max(1, request.Top))
            .ToList();
    }

    public ValueTask<IReadOnlyList<DocumentChunk>> GetCaseChunksAsync(string caseId, CancellationToken cancellationToken = default)
    {
        var chunks = _chunks.Values
            .Where(c => string.Equals(c.CaseId, caseId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.DocumentId, StringComparer.Ordinal)
            .ThenBy(c => c.PageNumber)
            .ThenBy(c => c.ChunkIndex)
            .ToList();

        return ValueTask.FromResult<IReadOnlyList<DocumentChunk>>(chunks);
    }

    public ValueTask<int> DeleteCaseAsync(string caseId, CancellationToken cancellationToken = default)
    {
        var toRemove = _chunks.Values
            .Where(c => string.Equals(c.CaseId, caseId, StringComparison.OrdinalIgnoreCase))
            .Select(c => c.ChunkId)
            .ToList();

        foreach (var id in toRemove)
        {
            _chunks.TryRemove(id, out _);
        }

        return ValueTask.FromResult(toRemove.Count);
    }

    public ValueTask<SearchIndexStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var stats = new SearchIndexStats
        {
            Provider = ProviderName,
            ChunkCount = _chunks.Count,
            CaseCount = _chunks.Values.Select(c => c.CaseId).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            DocumentCount = _chunks.Values.Select(c => c.DocumentId).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            IsHealthy = true
        };

        return ValueTask.FromResult(stats);
    }

    private static double KeywordScore(string content, IReadOnlyCollection<string> queryTerms)
    {
        if (queryTerms.Count == 0)
        {
            return 0d;
        }

        var contentTerms = Tokenize(content);
        if (contentTerms.Count == 0)
        {
            return 0d;
        }

        var contentSet = new HashSet<string>(contentTerms, StringComparer.Ordinal);
        var matches = queryTerms.Count(contentSet.Contains);
        return (double)matches / queryTerms.Count;
    }

    private static string BuildHighlight(string content, IReadOnlyCollection<string> queryTerms)
    {
        const int window = 240;
        if (content.Length <= window || queryTerms.Count == 0)
        {
            return content.Length <= window ? content : content[..window].TrimEnd() + "\u2026";
        }

        var lower = content.ToLowerInvariant();
        var idx = -1;
        foreach (var term in queryTerms)
        {
            idx = lower.IndexOf(term, StringComparison.Ordinal);
            if (idx >= 0)
            {
                break;
            }
        }

        if (idx < 0)
        {
            return content[..window].TrimEnd() + "\u2026";
        }

        var start = Math.Max(0, idx - (window / 3));
        var length = Math.Min(window, content.Length - start);
        var prefix = start > 0 ? "\u2026" : string.Empty;
        var suffix = start + length < content.Length ? "\u2026" : string.Empty;
        return prefix + content.Substring(start, length).Trim() + suffix;
    }

    private static List<string> Tokenize(string text)
    {
        var tokens = new List<string>();
        var start = -1;
        for (var i = 0; i < text.Length; i++)
        {
            if (char.IsLetterOrDigit(text[i]))
            {
                if (start < 0)
                {
                    start = i;
                }
            }
            else if (start >= 0)
            {
                tokens.Add(text[start..i].ToLowerInvariant());
                start = -1;
            }
        }

        if (start >= 0)
        {
            tokens.Add(text[start..].ToLowerInvariant());
        }

        return tokens;
    }
}
