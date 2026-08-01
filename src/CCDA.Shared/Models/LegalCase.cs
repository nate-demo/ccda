using CCDA.Shared.Constants;
using CCDA.Shared.Enums;

namespace CCDA.Shared.Models;

/// <summary>An entire synthetic legal case with all of its documents.</summary>
public sealed class LegalCase
{
    public string CaseId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public CaseType CaseType { get; set; }

    public CaseComplexity Complexity { get; set; }

    public CaseStatus Status { get; set; } = CaseStatus.UnderReview;

    public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;

    public string Classification { get; set; } = CcdaConstants.Classification;

    public List<CaseDocument> Documents { get; set; } = [];

    public int DocumentCount => Documents.Count;

    public int PageCount => Documents.Sum(d => d.PageCount);
}
