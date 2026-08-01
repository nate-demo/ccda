using CCDA.Shared.Constants;
using CCDA.Shared.Enums;

namespace CCDA.Shared.Models;

/// <summary>A multi-page legal document belonging to a case.</summary>
public sealed class CaseDocument
{
    public string DocumentId { get; set; } = string.Empty;

    public string CaseId { get; set; } = string.Empty;

    public DocumentType DocumentType { get; set; }

    public string Title { get; set; } = string.Empty;

    /// <summary>Logical source file name, e.g. "CCDA-2026-000123_ArrestReport.pdf".</summary>
    public string SourceFile { get; set; } = string.Empty;

    public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;

    public string Classification { get; set; } = CcdaConstants.Classification;

    public List<DocumentPage> Pages { get; set; } = [];

    public int PageCount => Pages.Count;

    /// <summary>Concatenated plain text of every page (useful for whole-document summarization).</summary>
    public string FullText => string.Join(Environment.NewLine + Environment.NewLine, Pages.Select(p => p.Text));
}
