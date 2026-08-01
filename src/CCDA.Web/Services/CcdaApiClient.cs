using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CCDA.Shared.Dtos;
using CCDA.Shared.Workflows;

namespace CCDA.Web.Services;

/// <summary>
/// Typed HTTP client for the CCDA case-review API. The base address is resolved through
/// Aspire service discovery (logical name <c>ccda-api</c>) when running under the AppHost, or
/// from the <c>ApiBaseUrl</c> configuration value when the web app is run standalone.
/// </summary>
public sealed class CcdaApiClient(HttpClient http)
{
    private const string Root = "api/v1/cases";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Lists ingested cases (most recent first). Returns an empty list on failure.</summary>
    public async Task<IReadOnlyList<CaseDetailResponse>> ListCasesAsync(CancellationToken ct = default)
    {
        var list = await http.GetFromJsonAsync<List<CaseDetailResponse>>($"{Root}/", JsonOptions, ct);
        return list ?? [];
    }

    /// <summary>Gets a single case's detail, or <c>null</c> when the case is not found.</summary>
    public async Task<CaseDetailResponse?> GetCaseAsync(string caseId, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"{Root}/{Uri.EscapeDataString(caseId)}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CaseDetailResponse>(JsonOptions, ct);
    }

    /// <summary>Gets the citable source-page catalog for a case, or <c>null</c> when not found.</summary>
    public async Task<CitationsResponse?> GetCitationsAsync(string caseId, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"{Root}/{Uri.EscapeDataString(caseId)}/citations", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CitationsResponse>(JsonOptions, ct);
    }

    /// <summary>Requests a grounded, citation-backed summary for a case.</summary>
    public Task<CaseSummaryResult?> SummarizeAsync(string caseId, CancellationToken ct = default) =>
        PostAsync<SummarizeRequest, CaseSummaryResult>($"{Root}/summarize", new() { CaseId = caseId }, ct);

    /// <summary>Requests a grounded, citation-backed chronological timeline for a case.</summary>
    public Task<CaseTimelineResult?> TimelineAsync(string caseId, CancellationToken ct = default) =>
        PostAsync<TimelineRequest, CaseTimelineResult>($"{Root}/timeline", new() { CaseId = caseId }, ct);

    /// <summary>Requests a grounded prosecutorial briefing for a case.</summary>
    public Task<CaseBriefingResult?> BriefingAsync(string caseId, CancellationToken ct = default) =>
        PostAsync<BriefingRequest, CaseBriefingResult>($"{Root}/briefing", new() { CaseId = caseId }, ct);

    /// <summary>Asks a grounded legal question, optionally scoped to a single case.</summary>
    public Task<LegalAnswerResult?> AskAsync(ChatRequest request, CancellationToken ct = default) =>
        PostAsync<ChatRequest, LegalAnswerResult>($"{Root}/chat", request, ct);

    /// <summary>Generates, indexes, and validates a synthetic demo case (fictional data).</summary>
    public Task<GenerateDemoDataResponse?> GenerateDemoDataAsync(GenerateDemoDataRequest request, CancellationToken ct = default) =>
        PostAsync<GenerateDemoDataRequest, GenerateDemoDataResponse>($"{Root}/generate-demo-data", request, ct);

    private async Task<TResponse?> PostAsync<TRequest, TResponse>(string uri, TRequest body, CancellationToken ct)
    {
        var response = await http.PostAsJsonAsync(uri, body, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, ct);
    }
}
