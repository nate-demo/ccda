using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace CCDA.Search.Indexing;

/// <summary>
/// Canonical CCDA case index definition, shared by the Azure provider (to create the index)
/// and by the /infra tooling (exported to JSON). Field names match <c>DocumentChunk</c>.
/// </summary>
public static class CcdaIndexSchema
{
    public const string VectorProfileName = "ccda-vector-profile";
    public const string VectorAlgorithmName = "ccda-hnsw";
    public const string SemanticConfigName = "ccda-semantic";

    public static readonly IReadOnlyList<string> FieldNames =
    [
        "chunkId", "documentId", "caseId", "documentType", "title",
        "sourceFile", "pageNumber", "chunkIndex", "content", "contentVector",
        "createdDate", "classification"
    ];

    public static SearchIndex Build(string indexName, int dimensions)
    {
        var fields = new List<SearchField>
        {
            new SimpleField("chunkId", SearchFieldDataType.String) { IsKey = true, IsFilterable = true },
            new SimpleField("documentId", SearchFieldDataType.String) { IsFilterable = true },
            new SimpleField("caseId", SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true },
            new SimpleField("documentType", SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true },
            new SearchableField("title") { IsFilterable = true },
            new SimpleField("sourceFile", SearchFieldDataType.String) { IsFilterable = true },
            new SimpleField("pageNumber", SearchFieldDataType.Int32) { IsFilterable = true, IsSortable = true },
            new SimpleField("chunkIndex", SearchFieldDataType.Int32) { IsSortable = true },
            new SearchableField("content"),
            new SearchField("contentVector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
            {
                IsSearchable = true,
                VectorSearchDimensions = dimensions,
                VectorSearchProfileName = VectorProfileName
            },
            new SimpleField("createdDate", SearchFieldDataType.DateTimeOffset) { IsFilterable = true, IsSortable = true },
            new SimpleField("classification", SearchFieldDataType.String) { IsFilterable = true }
        };

        return new SearchIndex(indexName)
        {
            Fields = fields,
            VectorSearch = new VectorSearch
            {
                Profiles = { new VectorSearchProfile(VectorProfileName, VectorAlgorithmName) },
                Algorithms = { new HnswAlgorithmConfiguration(VectorAlgorithmName) }
            },
            SemanticSearch = new SemanticSearch
            {
                Configurations =
                {
                    new SemanticConfiguration(SemanticConfigName, new SemanticPrioritizedFields
                    {
                        TitleField = new SemanticField("title"),
                        ContentFields = { new SemanticField("content") }
                    })
                }
            }
        };
    }
}
