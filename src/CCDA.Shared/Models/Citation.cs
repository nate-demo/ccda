using CCDA.Shared.Enums;

namespace CCDA.Shared.Models;

/// <summary>
/// A traceable reference from an AI response back to a specific source page.
/// Every AI answer in CCDA must carry one or more citations.
/// </summary>
public sealed record Citation
{
    public required string DocumentId { get; init; }

    public required string CaseId { get; init; }

    public required string SourceFile { get; init; }

    public int PageNumber { get; init; }

    public string ChunkId { get; init; } = string.Empty;

    public DocumentType DocumentType { get; init; }

    /// <summary>Short excerpt of the cited passage, for display next to the answer.</summary>
    public string Snippet { get; init; } = string.Empty;

    /// <summary>Relevance score (0..1) of the underlying chunk for this answer.</summary>
    public double Score { get; init; }

    /// <summary>Human-friendly reference label, e.g. "CCDA-2026-000123_ArrestReport.pdf (p.12)".</summary>
    public string Reference => $"{SourceFile} (p.{PageNumber})";
}
