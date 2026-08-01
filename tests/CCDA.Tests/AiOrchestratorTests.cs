using CCDA.AI.Abstractions;
using CCDA.DataGen.Generators;
using CCDA.DataGen.Options;
using CCDA.Search.Indexing;
using CCDA.Shared.Enums;
using CCDA.Shared.Models;
using Microsoft.Extensions.DependencyInjection;

namespace CCDA.Tests;

/// <summary>
/// Verifies the citation-first contract of the AI orchestrator: every workflow answer is
/// grounded in retrieved chunks, so it always carries page-level citations scoped to the case
/// and a confidence assessment. AI assists attorney review; it never answers ungrounded.
/// </summary>
public sealed class AiOrchestratorTests : IDisposable
{
    private readonly ServiceProvider _services = TestHost.Build();

    [Fact]
    public async Task Summarize_ReturnsGroundedCitations()
    {
        var (orchestrator, caseId, legalCase) = await IngestAsync(seed: 42);

        var result = await orchestrator.SummarizeAsync(caseId);

        Assert.Equal(caseId, result.CaseId);
        Assert.False(string.IsNullOrWhiteSpace(result.Summary));
        Assert.NotEmpty(result.Citations);
        AssertCitationsGrounded(result.Citations, legalCase);
        Assert.Equal(result.Citations.Count, result.Confidence.CitationCount);
        Assert.Contains(result.Confidence.Level, new[] { ConfidenceLevel.Low, ConfidenceLevel.Medium, ConfidenceLevel.High });
    }

    [Fact]
    public async Task Timeline_ReturnsChronologicalCitedEvents()
    {
        var (orchestrator, caseId, legalCase) = await IngestAsync(seed: 7);

        var result = await orchestrator.BuildTimelineAsync(caseId);

        Assert.NotEmpty(result.Events);
        Assert.All(result.Events, e => Assert.False(string.IsNullOrWhiteSpace(e.Title)));
        Assert.NotEmpty(result.Citations);
        AssertCitationsGrounded(result.Citations, legalCase);
    }

    [Fact]
    public async Task Briefing_ProducesDecisionReadySections()
    {
        var (orchestrator, caseId, legalCase) = await IngestAsync(seed: 11);

        var result = await orchestrator.BriefAsync(caseId);

        Assert.False(string.IsNullOrWhiteSpace(result.ExecutiveSummary));
        Assert.NotEmpty(result.KeyFindings);
        Assert.NotEmpty(result.Citations);
        AssertCitationsGrounded(result.Citations, legalCase);
    }

    [Fact]
    public async Task Answer_IsGroundedInTheCase()
    {
        var (orchestrator, caseId, legalCase) = await IngestAsync(seed: 21);

        var result = await orchestrator.AnswerAsync(caseId, "What are the key facts of this case?", []);

        Assert.Equal(caseId, result.CaseId);
        Assert.False(string.IsNullOrWhiteSpace(result.Answer));
        Assert.NotEmpty(result.Citations);
        AssertCitationsGrounded(result.Citations, legalCase);
    }

    private static void AssertCitationsGrounded(IEnumerable<Citation> citations, LegalCase legalCase)
    {
        var validSourceFiles = legalCase.Documents.Select(d => d.SourceFile).ToHashSet(StringComparer.Ordinal);
        Assert.All(citations, c =>
        {
            Assert.Equal(legalCase.CaseId, c.CaseId);
            Assert.True(c.PageNumber >= 1, "a citation must reference a real page");
            Assert.Contains(c.SourceFile, validSourceFiles);
        });
    }

    private async Task<(IAiOrchestrator Orchestrator, string CaseId, LegalCase Case)> IngestAsync(int seed)
    {
        var generator = _services.GetRequiredService<ICaseGenerator>();
        var indexer = _services.GetRequiredService<CaseIndexer>();
        var orchestrator = _services.GetRequiredService<IAiOrchestrator>();

        var legalCase = generator.GenerateCase(new GenerationRequest { Seed = seed, Complexity = CaseComplexity.Medium });
        await indexer.IndexCaseAsync(legalCase);
        return (orchestrator, legalCase.CaseId, legalCase);
    }

    public void Dispose() => _services.Dispose();
}
