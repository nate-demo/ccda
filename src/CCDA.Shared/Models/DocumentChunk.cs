using CCDA.Shared.Constants;
using CCDA.Shared.Enums;

namespace CCDA.Shared.Models;

/// <summary>
/// The unit of retrieval stored in the search index: a chunk of a document page,
/// carrying its embedding and full citation lineage back to the source page.
/// </summary>
public sealed class DocumentChunk
{
    public string ChunkId { get; set; } = string.Empty;

    public string DocumentId { get; set; } = string.Empty;

    public string CaseId { get; set; } = string.Empty;

    public DocumentType DocumentType { get; set; }

    public string Title { get; set; } = string.Empty;

    public string SourceFile { get; set; } = string.Empty;

    public int PageNumber { get; set; }

    public int ChunkIndex { get; set; }

    public string Content { get; set; } = string.Empty;

    public float[] Embedding { get; set; } = [];

    public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;

    public string Classification { get; set; } = CcdaConstants.Classification;

    /// <summary>Projects the chunk's lineage into the canonical metadata record.</summary>
    public DocumentMetadata ToMetadata() => new()
    {
        DocumentId = DocumentId,
        CaseId = CaseId,
        DocumentType = DocumentType.ToString(),
        PageNumber = PageNumber,
        SourceFile = SourceFile,
        CreatedDate = CreatedDate,
        Classification = Classification
    };
}
