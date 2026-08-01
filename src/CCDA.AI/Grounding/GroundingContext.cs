using CCDA.Shared.Models;
using CCDA.Shared.Search;

namespace CCDA.AI.Grounding;

/// <summary>
/// The grounded context for a workflow: the numbered source passages the model may use,
/// the citations those passages map to, the raw hits, and an aggregate retrieval score.
/// </summary>
public sealed record GroundingContext
{
    public required string Text { get; init; }

    public IReadOnlyList<Citation> Citations { get; init; } = [];

    public IReadOnlyList<SearchHit> Hits { get; init; } = [];

    /// <summary>Mean relevance across retrieved hits (0..1), used to seed confidence.</summary>
    public double AverageScore { get; init; }

    public bool HasResults => Hits.Count > 0;

    public static GroundingContext Empty { get; } = new() { Text = string.Empty };
}
