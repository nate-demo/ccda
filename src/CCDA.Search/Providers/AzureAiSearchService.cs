using Azure;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using CCDA.Search.Abstractions;
using CCDA.Search.Indexing;
using CCDA.Search.Options;
using CCDA.Shared.Enums;
using CCDA.Shared.Models;
using CCDA.Shared.Search;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SearchMode = CCDA.Shared.Enums.SearchMode;

namespace CCDA.Search.Providers;

/// <summary>
/// Azure AI Search backed retrieval provider. Selected when an <c>Azure:Search:Endpoint</c>
/// is configured. Performs true hybrid (keyword + vector) and semantic retrieval using the
/// index defined by <see cref="CcdaIndexSchema"/>. Query vectors come from the configured
/// <see cref="IEmbeddingService"/> so the same embedding model is used for indexing and search.
/// </summary>
public sealed class AzureAiSearchService : ISearchService
{
    private readonly AzureSearchOptions _options;
    private readonly IEmbeddingService _embeddings;
    private readonly ILogger<AzureAiSearchService> _logger;
    private readonly SearchIndexClient _indexClient;
    private readonly SearchClient _searchClient;

    public AzureAiSearchService(
        IOptions<AzureSearchOptions> options,
        IEmbeddingService embeddings,
        ILogger<AzureAiSearchService> logger)
    {
        _options = options.Value;
        _embeddings = embeddings;
        _logger = logger;

        var endpoint = new Uri(_options.Endpoint!);
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            var credential = new DefaultAzureCredential();
            _indexClient = new SearchIndexClient(endpoint, credential);
            _searchClient = new SearchClient(endpoint, _options.IndexName, credential);
        }
        else
        {
            var credential = new AzureKeyCredential(_options.ApiKey);
            _indexClient = new SearchIndexClient(endpoint, credential);
            _searchClient = new SearchClient(endpoint, _options.IndexName, credential);
        }
    }

    public string ProviderName => "Azure AI Search";

    public async ValueTask EnsureIndexAsync(CancellationToken cancellationToken = default)
    {
        var index = CcdaIndexSchema.Build(_options.IndexName, _embeddings.Dimensions);
        await _indexClient.CreateOrUpdateIndexAsync(index, cancellationToken: cancellationToken);
        _logger.LogInformation("Ensured Azure AI Search index {Index}.", _options.IndexName);
    }

    public async ValueTask<int> IndexAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        var batch = new List<SearchDocument>();
        foreach (var chunk in chunks)
        {
            if (chunk.Embedding.Length == 0)
            {
                chunk.Embedding = await _embeddings.GenerateAsync(chunk.Content, cancellationToken);
            }

            batch.Add(ToSearchDocument(chunk));
        }

        if (batch.Count == 0)
        {
            return 0;
        }

        await _searchClient.MergeOrUploadDocumentsAsync(batch, cancellationToken: cancellationToken);
        _logger.LogInformation("Uploaded {Count} chunks to Azure AI Search.", batch.Count);
        return batch.Count;
    }

    public async ValueTask<IReadOnlyList<SearchHit>> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return [];
        }

        var options = new SearchOptions
        {
            Size = Math.Max(1, request.Top),
            Filter = BuildFilter(request)
        };

        foreach (var field in CcdaIndexSchema.FieldNames)
        {
            if (field != "contentVector")
            {
                options.Select.Add(field);
            }
        }

        if (request.Mode is not SearchMode.Keyword)
        {
            var vector = await _embeddings.GenerateAsync(request.Query, cancellationToken);
            options.VectorSearch = new VectorSearchOptions
            {
                Queries =
                {
                    new VectorizedQuery(vector)
                    {
                        KNearestNeighborsCount = Math.Max(1, request.Top),
                        Fields = { "contentVector" }
                    }
                }
            };
        }

        if (request.Mode is SearchMode.Semantic)
        {
            options.QueryType = SearchQueryType.Semantic;
            options.SemanticSearch = new SemanticSearchOptions
            {
                SemanticConfigurationName = CcdaIndexSchema.SemanticConfigName
            };
        }

        var searchText = request.Mode is SearchMode.Vector ? null : request.Query;
        var response = await _searchClient.SearchAsync<SearchDocument>(searchText, options, cancellationToken);

        var hits = new List<SearchHit>();
        await foreach (var result in response.Value.GetResultsAsync())
        {
            var chunk = FromSearchDocument(result.Document);
            hits.Add(new SearchHit
            {
                Chunk = chunk,
                Score = result.SemanticSearch?.RerankerScore ?? result.Score ?? 0d,
                Highlight = chunk.Content.Length <= 240 ? chunk.Content : chunk.Content[..240].TrimEnd() + "\u2026"
            });
        }

        return hits;
    }

    public async ValueTask<IReadOnlyList<DocumentChunk>> GetCaseChunksAsync(string caseId, CancellationToken cancellationToken = default)
    {
        var options = new SearchOptions
        {
            Filter = $"caseId eq '{Escape(caseId)}'",
            Size = 1000,
            OrderBy = { "documentId", "pageNumber", "chunkIndex" }
        };

        foreach (var field in CcdaIndexSchema.FieldNames.Where(f => f != "contentVector"))
        {
            options.Select.Add(field);
        }

        var response = await _searchClient.SearchAsync<SearchDocument>("*", options, cancellationToken);
        var chunks = new List<DocumentChunk>();
        await foreach (var result in response.Value.GetResultsAsync())
        {
            chunks.Add(FromSearchDocument(result.Document));
        }

        return chunks;
    }

    public async ValueTask<int> DeleteCaseAsync(string caseId, CancellationToken cancellationToken = default)
    {
        var existing = await GetCaseChunksAsync(caseId, cancellationToken);
        if (existing.Count == 0)
        {
            return 0;
        }

        var keys = existing.Select(c => new SearchDocument { ["chunkId"] = c.ChunkId });
        await _searchClient.DeleteDocumentsAsync(keys, cancellationToken: cancellationToken);
        return existing.Count;
    }

    public async ValueTask<SearchIndexStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var count = await _searchClient.GetDocumentCountAsync(cancellationToken);
            return new SearchIndexStats
            {
                Provider = ProviderName,
                ChunkCount = (int)count.Value,
                IsHealthy = true
            };
        }
        catch (RequestFailedException ex)
        {
            _logger.LogWarning(ex, "Failed to read Azure AI Search document count.");
            return new SearchIndexStats { Provider = ProviderName, IsHealthy = false };
        }
    }

    private static SearchDocument ToSearchDocument(DocumentChunk chunk) => new()
    {
        ["chunkId"] = chunk.ChunkId,
        ["documentId"] = chunk.DocumentId,
        ["caseId"] = chunk.CaseId,
        ["documentType"] = chunk.DocumentType.ToString(),
        ["title"] = chunk.Title,
        ["sourceFile"] = chunk.SourceFile,
        ["pageNumber"] = chunk.PageNumber,
        ["chunkIndex"] = chunk.ChunkIndex,
        ["content"] = chunk.Content,
        ["contentVector"] = chunk.Embedding,
        ["createdDate"] = chunk.CreatedDate,
        ["classification"] = chunk.Classification
    };

    private static DocumentChunk FromSearchDocument(SearchDocument doc) => new()
    {
        ChunkId = doc.GetString("chunkId") ?? string.Empty,
        DocumentId = doc.GetString("documentId") ?? string.Empty,
        CaseId = doc.GetString("caseId") ?? string.Empty,
        DocumentType = Enum.TryParse<DocumentType>(doc.GetString("documentType"), out var dt) ? dt : DocumentType.Other,
        Title = doc.GetString("title") ?? string.Empty,
        SourceFile = doc.GetString("sourceFile") ?? string.Empty,
        PageNumber = doc.GetInt32("pageNumber") ?? 0,
        ChunkIndex = doc.GetInt32("chunkIndex") ?? 0,
        Content = doc.GetString("content") ?? string.Empty,
        Classification = doc.GetString("classification") ?? string.Empty
    };

    private static string? BuildFilter(SearchRequest request)
    {
        var clauses = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.CaseId))
        {
            clauses.Add($"caseId eq '{Escape(request.CaseId)}'");
        }

        if (request.DocumentType is { } dt)
        {
            clauses.Add($"documentType eq '{dt}'");
        }

        return clauses.Count == 0 ? null : string.Join(" and ", clauses);
    }

    private static string Escape(string value) => value.Replace("'", "''");
}
