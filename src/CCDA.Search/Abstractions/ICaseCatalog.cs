using CCDA.Shared.Enums;
using CCDA.Shared.Models;

namespace CCDA.Search.Abstractions;

/// <summary>Lightweight per-document metadata retained in the case catalog (no page text).</summary>
public sealed record CaseDocumentRecord(
    string DocumentId,
    DocumentType DocumentType,
    string Title,
    string SourceFile,
    int PageCount);

/// <summary>Case-level metadata retained after ingestion so case detail can be served without re-reading pages.</summary>
public sealed record CaseRecord
{
    public required string CaseId { get; init; }

    public string Title { get; init; } = string.Empty;

    public CaseType CaseType { get; init; }

    public CaseComplexity Complexity { get; init; }

    public CaseStatus Status { get; init; }

    public DateTimeOffset CreatedDate { get; init; }

    public string Classification { get; init; } = string.Empty;

    public IReadOnlyList<CaseDocumentRecord> Documents { get; init; } = [];

    public int DocumentCount => Documents.Count;

    public int PageCount => Documents.Sum(d => d.PageCount);
}

/// <summary>
/// A catalog of ingested case metadata. Populated automatically during indexing so the API can
/// serve case detail (<c>GET /cases/{id}</c>) without persisting full document text separately.
/// </summary>
public interface ICaseCatalog
{
    /// <summary>Records (or replaces) the metadata for an ingested case.</summary>
    void Record(LegalCase legalCase);

    /// <summary>Gets the metadata for a case, or null if it has not been ingested.</summary>
    CaseRecord? Get(string caseId);

    /// <summary>Lists all ingested cases (most recently ingested first).</summary>
    IReadOnlyList<CaseRecord> List();

    /// <summary>Removes a case from the catalog; returns true if it existed.</summary>
    bool Remove(string caseId);
}
