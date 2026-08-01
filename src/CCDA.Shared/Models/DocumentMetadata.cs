using System.Text.Json.Serialization;
using CCDA.Shared.Constants;

namespace CCDA.Shared.Models;

/// <summary>
/// The canonical per-page metadata record attached to indexed content.
/// Mirrors the metadata model in the solution brief.
/// </summary>
public sealed record DocumentMetadata
{
    [JsonPropertyName("documentId")]
    public string DocumentId { get; init; } = string.Empty;

    [JsonPropertyName("caseId")]
    public string CaseId { get; init; } = string.Empty;

    [JsonPropertyName("documentType")]
    public string DocumentType { get; init; } = string.Empty;

    [JsonPropertyName("pageNumber")]
    public int PageNumber { get; init; }

    [JsonPropertyName("sourceFile")]
    public string SourceFile { get; init; } = string.Empty;

    [JsonPropertyName("createdDate")]
    public DateTimeOffset CreatedDate { get; init; }

    [JsonPropertyName("classification")]
    public string Classification { get; init; } = CcdaConstants.Classification;
}
