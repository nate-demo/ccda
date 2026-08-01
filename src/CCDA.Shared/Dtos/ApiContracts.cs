using CCDA.Shared.Enums;
using CCDA.Shared.Models;
using CCDA.Shared.Reporting;

namespace CCDA.Shared.Dtos;

// ---------------------------------------------------------------------------
// Request DTOs
// ---------------------------------------------------------------------------

/// <summary>POST /case/upload — ingest a case (its documents) into the search index.</summary>
public sealed record UploadCaseRequest
{
    public required LegalCase Case { get; init; }
}

/// <summary>POST /case/summarize</summary>
public sealed record SummarizeRequest
{
    public required string CaseId { get; init; }
}

/// <summary>POST /case/timeline</summary>
public sealed record TimelineRequest
{
    public required string CaseId { get; init; }
}

/// <summary>POST /case/briefing</summary>
public sealed record BriefingRequest
{
    public required string CaseId { get; init; }
}

/// <summary>A single conversational turn for the chat workflow.</summary>
public sealed record ChatTurn
{
    /// <summary>"user" or "assistant".</summary>
    public required string Role { get; init; }

    public required string Content { get; init; }
}

/// <summary>POST /case/chat — grounded legal Q&amp;A.</summary>
public sealed record ChatRequest
{
    public string? CaseId { get; init; }

    public required string Question { get; init; }

    public IReadOnlyList<ChatTurn> History { get; init; } = [];
}

/// <summary>
/// POST /case/generate-demo-data — generate, index, and validate a synthetic case.
/// Mirrors the Copilot Studio "Generate Demo Case" action parameters
/// (Case Type, Complexity, Document Size).
/// </summary>
public sealed record GenerateDemoDataRequest
{
    /// <summary>Optional; when null a case type is chosen from the seed.</summary>
    public CaseType? CaseType { get; init; }

    public CaseComplexity Complexity { get; init; } = CaseComplexity.Medium;

    /// <summary>Target page volume ("Document Size"); null lets complexity decide.</summary>
    public int? Pages { get; init; }

    /// <summary>Optional deterministic seed for reproducible demos.</summary>
    public int? Seed { get; init; }
}

// ---------------------------------------------------------------------------
// Response DTOs
// ---------------------------------------------------------------------------

/// <summary>Response for an upload / ingestion request.</summary>
public sealed record UploadCaseResponse
{
    public required string CaseId { get; init; }

    public int DocumentsIndexed { get; init; }

    public int ChunksIndexed { get; init; }

    public string Status { get; init; } = "Success";
}

/// <summary>Lightweight per-document summary (no page text) for case detail responses.</summary>
public sealed record CaseDocumentSummary
{
    public required string DocumentId { get; init; }

    public DocumentType DocumentType { get; init; }

    public string Title { get; init; } = string.Empty;

    public string SourceFile { get; init; } = string.Empty;

    public int PageCount { get; init; }
}

/// <summary>GET /case/{id} — case detail without full document text.</summary>
public sealed record CaseDetailResponse
{
    public required string CaseId { get; init; }

    public string Title { get; init; } = string.Empty;

    public CaseType CaseType { get; init; }

    public CaseComplexity Complexity { get; init; }

    public CaseStatus Status { get; init; }

    public DateTimeOffset CreatedDate { get; init; }

    public int DocumentCount { get; init; }

    public int PageCount { get; init; }

    public string Classification { get; init; } = string.Empty;

    public IReadOnlyList<CaseDocumentSummary> Documents { get; init; } = [];
}

/// <summary>GET /case/{id}/citations — the catalog of citable source references for a case.</summary>
public sealed record CitationsResponse
{
    public required string CaseId { get; init; }

    public int Count { get; init; }

    public IReadOnlyList<Citation> Citations { get; init; } = [];
}

/// <summary>Response for a synthetic-data generation request.</summary>
public sealed record GenerateDemoDataResponse
{
    public required string CaseId { get; init; }

    public int DocumentsGenerated { get; init; }

    public int PagesGenerated { get; init; }

    public int ChunksIndexed { get; init; }

    public string IndexStatus { get; init; } = "Success";

    public GenerationReport Report { get; init; } = new();
}
