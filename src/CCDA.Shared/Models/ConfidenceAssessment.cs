using CCDA.Shared.Enums;

namespace CCDA.Shared.Models;

/// <summary>
/// A confidence assessment attached to every AI response. In CCDA, confidence is
/// grounded primarily in retrieval quality and citation coverage — never a claim
/// that the AI's legal judgment is correct. AI assists; attorneys decide.
/// </summary>
public sealed record ConfidenceAssessment
{
    /// <summary>Numeric grounding score in the range 0..1.</summary>
    public double Score { get; init; }

    public ConfidenceLevel Level { get; init; }

    public string Rationale { get; init; } = string.Empty;

    public int CitationCount { get; init; }

    /// <summary>Builds an assessment from a raw grounding score and the number of supporting citations.</summary>
    public static ConfidenceAssessment FromScore(double score, int citationCount, string? rationale = null)
    {
        var clamped = Math.Clamp(score, 0d, 1d);
        var level = clamped switch
        {
            >= 0.75 => ConfidenceLevel.High,
            >= 0.50 => ConfidenceLevel.Medium,
            _ => ConfidenceLevel.Low
        };

        return new ConfidenceAssessment
        {
            Score = Math.Round(clamped, 3),
            Level = level,
            CitationCount = citationCount,
            Rationale = rationale ?? DefaultRationale(level, citationCount)
        };
    }

    private static string DefaultRationale(ConfidenceLevel level, int citations) => level switch
    {
        ConfidenceLevel.High =>
            $"Answer is grounded in {citations} well-matched source passage(s). Attorney verification still required.",
        ConfidenceLevel.Medium =>
            $"Answer is supported by {citations} source passage(s) of moderate relevance. Attorney review recommended.",
        _ =>
            $"Limited or weak grounding ({citations} source passage(s)). Treat as a lead only and verify against the record."
    };
}
