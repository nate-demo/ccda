using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CCDA.Shared.Dtos;
using CCDA.Shared.Enums;
using CCDA.Shared.Workflows;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CCDA.Tests;

/// <summary>
/// Boots the real CCDA.Api in memory (offline providers) and asserts the public HTTP contract:
/// enum-as-string serialization, the generate → list → summarize happy path with citations, and
/// the documented validation / not-found responses.
/// </summary>
public sealed class ApiContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly HttpClient _client;

    public ApiContractTests(WebApplicationFactory<Program> factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task GenerateDemoData_ThenList_ThenSummarize_FlowsWithCitations()
    {
        // 1. Generate a reproducible demo case.
        var genResponse = await _client.PostAsJsonAsync("/api/v1/cases/generate-demo-data",
            new GenerateDemoDataRequest { Complexity = CaseComplexity.Medium, Seed = 4242 }, Json);
        genResponse.EnsureSuccessStatusCode();

        var generated = await genResponse.Content.ReadFromJsonAsync<GenerateDemoDataResponse>(Json);
        Assert.NotNull(generated);
        Assert.False(string.IsNullOrWhiteSpace(generated!.CaseId));
        Assert.True(generated.DocumentsGenerated > 0);

        // 2. The case appears in the list.
        var list = await _client.GetFromJsonAsync<List<CaseDetailResponse>>("/api/v1/cases", Json);
        Assert.NotNull(list);
        Assert.Contains(list!, c => c.CaseId == generated.CaseId);

        // 3. Enum-as-string contract on the detail endpoint.
        var detailRaw = await _client.GetStringAsync($"/api/v1/cases/{generated.CaseId}");
        Assert.Contains("\"complexity\":\"Medium\"", detailRaw, StringComparison.Ordinal);

        // 4. Summarize returns grounded citations.
        var summaryResponse = await _client.PostAsJsonAsync("/api/v1/cases/summarize",
            new SummarizeRequest { CaseId = generated.CaseId }, Json);
        summaryResponse.EnsureSuccessStatusCode();

        var summary = await summaryResponse.Content.ReadFromJsonAsync<CaseSummaryResult>(Json);
        Assert.NotNull(summary);
        Assert.False(string.IsNullOrWhiteSpace(summary!.Summary));
        Assert.NotEmpty(summary.Citations);
        Assert.All(summary.Citations, c => Assert.Equal(generated.CaseId, c.CaseId));
    }

    [Fact]
    public async Task GenerateDemoData_WithInvalidPages_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/cases/generate-demo-data",
            new GenerateDemoDataRequest { Pages = 0 }, Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Summarize_UnknownCase_ReturnsNotFound()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/cases/summarize",
            new SummarizeRequest { CaseId = "CCDA-0000-000000" }, Json);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Root_AdvertisesFictionalDataClassification()
    {
        var root = await _client.GetStringAsync("/");
        Assert.Contains("FICTIONAL CASE DATA", root, StringComparison.Ordinal);
    }
}
