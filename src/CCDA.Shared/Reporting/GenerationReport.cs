namespace CCDA.Shared.Reporting;

/// <summary>
/// A report describing the outcome of a synthetic-data generation + ingestion run.
/// Mirrors the indexing report shape in the solution brief.
/// </summary>
public sealed record GenerationReport
{
    public int CasesGenerated { get; init; }

    public int DocumentsGenerated { get; init; }

    public int PagesGenerated { get; init; }

    public int ChunksCreated { get; init; }

    /// <summary>"Passed", "Failed", or "Not Run".</summary>
    public string RetrievalValidation { get; init; } = "Not Run";

    /// <summary>"Success", "Partial", or "Failed".</summary>
    public string IndexStatus { get; init; } = "Unknown";

    public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;

    public int? Seed { get; init; }

    public IReadOnlyList<string> CaseIds { get; init; } = [];
}

/// <summary>
/// Aggregated statistics surfaced on the Aspire "Synthetic Data Generation" dashboard widget.
/// </summary>
public sealed record SyntheticDataStats
{
    public int CasesGenerated { get; init; }

    public int DocumentsGenerated { get; init; }

    public int PagesGenerated { get; init; }

    public int ChunksIndexed { get; init; }

    /// <summary>Most recent retrieval-validation score (0..1).</summary>
    public double RetrievalScore { get; init; }

    /// <summary>"Healthy", "Degraded", "Empty", or "Unknown".</summary>
    public string IndexHealth { get; init; } = "Unknown";

    public DateTimeOffset? LastGenerationTime { get; init; }
}
