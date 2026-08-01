using CCDA.AI.Abstractions;
using CCDA.Search.Abstractions;
using CCDA.Search.Indexing;
using CCDA.DataGen.Ingestion;
using CCDA.Shared.Dtos;
using CCDA.Shared.Models;
using CCDA.Shared.Workflows;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CCDA.Api.Endpoints;

/// <summary>
/// Maps the CCDA case-review API surface. Every AI endpoint is citation-first: the response
/// carries the grounded citations and a confidence score. AI assists attorney review; it does
/// not replace attorney judgment.
/// </summary>
internal static class CaseEndpoints
{
    private const int SnippetLength = 180;

    public static IEndpointRouteBuilder MapCaseEndpoints(this IEndpointRouteBuilder app)
    {
        // Versioned route group. A simple URL-segment version keeps the local demo and the
        // APIM-fronted deployment aligned without extra versioning packages.
        var cases = app.MapGroup("/api/v1/cases").WithTags("Cases");

        cases.MapPost("/upload", UploadAsync)
            .WithName("UploadCase")
            .WithSummary("Ingest a case and its documents into the search index.");

        cases.MapGet("/", ListAsync)
            .WithName("ListCases")
            .WithSummary("List all ingested cases (most recent first).");

        cases.MapGet("/{caseId}", GetAsync)
            .WithName("GetCase")
            .WithSummary("Get case detail (metadata + documents) without full page text.");

        cases.MapGet("/{caseId}/citations", GetCitationsAsync)
            .WithName("GetCaseCitations")
            .WithSummary("List every citable source page reference for a case.");

        cases.MapPost("/summarize", SummarizeAsync)
            .WithName("SummarizeCase")
            .WithSummary("Produce a grounded, citation-backed case summary.");

        cases.MapPost("/timeline", TimelineAsync)
            .WithName("BuildCaseTimeline")
            .WithSummary("Extract a grounded, citation-backed chronological timeline.");

        cases.MapPost("/briefing", BriefingAsync)
            .WithName("BuildCaseBriefing")
            .WithSummary("Produce a grounded prosecutorial briefing with findings, risks, and recommendations.");

        cases.MapPost("/chat", ChatAsync)
            .WithName("AskCase")
            .WithSummary("Answer a grounded legal question with page-level citations.");

        cases.MapPost("/generate-demo-data", GenerateDemoDataAsync)
            .WithName("GenerateDemoData")
            .WithSummary("Generate, index, and validate a synthetic demo case (FICTIONAL DATA).");

        return app;
    }

    private static async Task<Results<Ok<UploadCaseResponse>, ValidationProblem>> UploadAsync(
        UploadCaseRequest request,
        CaseIndexer indexer,
        CancellationToken cancellationToken)
    {
        if (request.Case is null || string.IsNullOrWhiteSpace(request.Case.CaseId))
        {
            return ValidationError("Case", "A case with a non-empty CaseId is required.");
        }

        if (request.Case.Documents.Count == 0)
        {
            return ValidationError("Case.Documents", "The case must contain at least one document.");
        }

        var result = await indexer.IndexCaseAsync(request.Case, cancellationToken: cancellationToken);

        return TypedResults.Ok(new UploadCaseResponse
        {
            CaseId = result.CaseId,
            DocumentsIndexed = result.DocumentCount,
            ChunksIndexed = result.ChunkCount,
            Status = "Success",
        });
    }

    private static Ok<IReadOnlyList<CaseDetailResponse>> ListAsync(ICaseCatalog catalog)
    {
        IReadOnlyList<CaseDetailResponse> list = catalog.List().Select(ToDetail).ToList();
        return TypedResults.Ok(list);
    }

    private static Results<Ok<CaseDetailResponse>, NotFound> GetAsync(string caseId, ICaseCatalog catalog)
    {
        var record = catalog.Get(caseId);
        return record is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(ToDetail(record));
    }

    private static async Task<Results<Ok<CitationsResponse>, NotFound>> GetCitationsAsync(
        string caseId,
        ICaseCatalog catalog,
        ISearchService search,
        CancellationToken cancellationToken)
    {
        if (catalog.Get(caseId) is null)
        {
            return TypedResults.NotFound();
        }

        var chunks = await search.GetCaseChunksAsync(caseId, cancellationToken);

        // One citation per source page: dedupe chunks by (document, page) so the catalog of
        // citable references maps cleanly to the underlying document pages.
        var citations = chunks
            .GroupBy(c => (c.DocumentId, c.PageNumber))
            .Select(g => g.OrderBy(c => c.ChunkIndex).First())
            .Select(ToCitation)
            .OrderBy(c => c.SourceFile, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.PageNumber)
            .ToList();

        return TypedResults.Ok(new CitationsResponse
        {
            CaseId = caseId,
            Count = citations.Count,
            Citations = citations,
        });
    }

    private static async Task<Results<Ok<CaseSummaryResult>, NotFound, ValidationProblem>> SummarizeAsync(
        SummarizeRequest request,
        ICaseCatalog catalog,
        IAiOrchestrator orchestrator,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CaseId))
        {
            return ValidationError("CaseId", "CaseId is required.");
        }

        if (catalog.Get(request.CaseId) is null)
        {
            return TypedResults.NotFound();
        }

        var result = await orchestrator.SummarizeAsync(request.CaseId, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<CaseTimelineResult>, NotFound, ValidationProblem>> TimelineAsync(
        TimelineRequest request,
        ICaseCatalog catalog,
        IAiOrchestrator orchestrator,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CaseId))
        {
            return ValidationError("CaseId", "CaseId is required.");
        }

        if (catalog.Get(request.CaseId) is null)
        {
            return TypedResults.NotFound();
        }

        var result = await orchestrator.BuildTimelineAsync(request.CaseId, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<CaseBriefingResult>, NotFound, ValidationProblem>> BriefingAsync(
        BriefingRequest request,
        ICaseCatalog catalog,
        IAiOrchestrator orchestrator,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CaseId))
        {
            return ValidationError("CaseId", "CaseId is required.");
        }

        if (catalog.Get(request.CaseId) is null)
        {
            return TypedResults.NotFound();
        }

        var result = await orchestrator.BriefAsync(request.CaseId, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<LegalAnswerResult>, NotFound, ValidationProblem>> ChatAsync(
        ChatRequest request,
        ICaseCatalog catalog,
        IAiOrchestrator orchestrator,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return ValidationError("Question", "A question is required.");
        }

        if (!string.IsNullOrWhiteSpace(request.CaseId) && catalog.Get(request.CaseId) is null)
        {
            return TypedResults.NotFound();
        }

        var result = await orchestrator.AnswerAsync(request.CaseId, request.Question, request.History, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<GenerateDemoDataResponse>, ValidationProblem>> GenerateDemoDataAsync(
        GenerateDemoDataRequest request,
        SyntheticDataService dataService,
        CancellationToken cancellationToken)
    {
        if (request.Pages is < 1)
        {
            return ValidationError("Pages", "Pages, when specified, must be at least 1.");
        }

        var response = await dataService.GenerateDemoCaseAsync(request, cancellationToken);
        return TypedResults.Ok(response);
    }

    private static CaseDetailResponse ToDetail(CaseRecord record) => new()
    {
        CaseId = record.CaseId,
        Title = record.Title,
        CaseType = record.CaseType,
        Complexity = record.Complexity,
        Status = record.Status,
        CreatedDate = record.CreatedDate,
        DocumentCount = record.DocumentCount,
        PageCount = record.PageCount,
        Classification = record.Classification,
        Documents = record.Documents.Select(d => new CaseDocumentSummary
        {
            DocumentId = d.DocumentId,
            DocumentType = d.DocumentType,
            Title = d.Title,
            SourceFile = d.SourceFile,
            PageCount = d.PageCount,
        }).ToList(),
    };

    private static Citation ToCitation(DocumentChunk chunk) => new()
    {
        DocumentId = chunk.DocumentId,
        CaseId = chunk.CaseId,
        SourceFile = chunk.SourceFile,
        PageNumber = chunk.PageNumber,
        ChunkId = chunk.ChunkId,
        DocumentType = chunk.DocumentType,
        Snippet = Snippet(chunk.Content),
        Score = 0d,
    };

    private static string Snippet(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var trimmed = content.Trim();
        return trimmed.Length <= SnippetLength ? trimmed : trimmed[..SnippetLength] + "…";
    }

    private static ValidationProblem ValidationError(string field, string message) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            [field] = [message],
        });
}
