using CCDA.Shared.Enums;
using CCDA.Shared.Models;

namespace CCDA.Shared.Search;

/// <summary>A retrieval request against the case index.</summary>
public sealed record SearchRequest
{
    public required string Query { get; init; }

    /// <summary>Optional case scope. When set, retrieval is restricted to a single case.</summary>
    public string? CaseId { get; init; }

    /// <summary>Optional document-type filter.</summary>
    public DocumentType? DocumentType { get; init; }

    public int Top { get; init; } = 8;

    public SearchMode Mode { get; init; } = SearchMode.Hybrid;
}

/// <summary>Health &amp; volume statistics for the search index (used by the dashboard).</summary>
public sealed record SearchIndexStats
{
    public required string Provider { get; init; }

    public int ChunkCount { get; init; }

    public int CaseCount { get; init; }

    public int DocumentCount { get; init; }

    public bool IsHealthy { get; init; }

    /// <summary>"Healthy", "Degraded", "Empty", or "Unknown".</summary>
    public string Health => !IsHealthy ? "Degraded" : ChunkCount == 0 ? "Empty" : "Healthy";
}

/// <summary>A single scored retrieval result.</summary>
public sealed record SearchHit
{
    public required DocumentChunk Chunk { get; init; }

    /// <summary>Fused relevance score (0..1).</summary>
    public double Score { get; init; }

    /// <summary>Highlighted / trimmed snippet suitable for display.</summary>
    public string Highlight { get; init; } = string.Empty;

    /// <summary>Projects this hit into a citation for inclusion in an AI response.</summary>
    public Citation ToCitation() => new()
    {
        DocumentId = Chunk.DocumentId,
        CaseId = Chunk.CaseId,
        SourceFile = Chunk.SourceFile,
        PageNumber = Chunk.PageNumber,
        ChunkId = Chunk.ChunkId,
        DocumentType = Chunk.DocumentType,
        Snippet = string.IsNullOrEmpty(Highlight)
            ? Trim(Chunk.Content, 240)
            : Highlight,
        Score = Score
    };

    private static string Trim(string text, int max) =>
        text.Length <= max ? text : text[..max].TrimEnd() + "\u2026";
}
