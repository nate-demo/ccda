using CCDA.AI.Abstractions;
using CCDA.AI.Grounding;
using CCDA.AI.Prompts;
using CCDA.Search.Abstractions;
using CCDA.Shared.Dtos;
using CCDA.Shared.Enums;
using CCDA.Shared.Models;
using CCDA.Shared.Workflows;
using Microsoft.Extensions.Logging;

namespace CCDA.AI.Workflows;

/// <summary>
/// The citation-first AI orchestrator. Each workflow retrieves grounded context, asks the
/// configured chat provider for a narrative, then attaches citations and a confidence
/// assessment derived from retrieval — never from the model's own claims.
/// </summary>
public sealed class AiOrchestrator : IAiOrchestrator
{
    private const string SummaryQuery = "case overview summary parties charges alleged conduct key evidence current posture";
    private const string BriefingQuery = "key findings strength of evidence charges risks weaknesses recommendations decision";

    private readonly RetrievalGrounder _grounder;
    private readonly IChatCompletionService _chat;
    private readonly ISearchService _search;
    private readonly ILogger<AiOrchestrator> _logger;

    public AiOrchestrator(
        RetrievalGrounder grounder,
        IChatCompletionService chat,
        ISearchService search,
        ILogger<AiOrchestrator> logger)
    {
        _grounder = grounder;
        _chat = chat;
        _search = search;
        _logger = logger;
    }

    public async ValueTask<CaseSummaryResult> SummarizeAsync(string caseId, CancellationToken cancellationToken = default)
    {
        var context = await _grounder.GroundAsync(SummaryQuery, caseId, cancellationToken: cancellationToken);
        if (!context.HasResults)
        {
            return new CaseSummaryResult
            {
                CaseId = caseId,
                Summary = NoData(caseId),
                Confidence = ConfidenceAssessment.FromScore(0d, 0, "No indexed content was retrieved for this case.")
            };
        }

        var narrative = await _chat.CompleteAsync(CcdaPrompts.Summary, $"CONTEXT:\n{context.Text}", cancellationToken);

        return new CaseSummaryResult
        {
            CaseId = caseId,
            Summary = narrative,
            KeyPoints = InsightExtractor.KeyPoints(context.Hits, 5),
            Citations = context.Citations,
            Confidence = ConfidenceAssessment.FromScore(context.AverageScore, context.Citations.Count)
        };
    }

    public async ValueTask<CaseBriefingResult> BriefAsync(string caseId, CancellationToken cancellationToken = default)
    {
        var context = await _grounder.GroundAsync(BriefingQuery, caseId, cancellationToken: cancellationToken);
        if (!context.HasResults)
        {
            return new CaseBriefingResult
            {
                CaseId = caseId,
                ExecutiveSummary = NoData(caseId),
                Confidence = ConfidenceAssessment.FromScore(0d, 0, "No indexed content was retrieved for this case.")
            };
        }

        var narrative = await _chat.CompleteAsync(CcdaPrompts.Briefing, $"CONTEXT:\n{context.Text}", cancellationToken);

        return new CaseBriefingResult
        {
            CaseId = caseId,
            ExecutiveSummary = narrative,
            KeyFindings = InsightExtractor.Findings(context.Hits, 5),
            Risks = InsightExtractor.Risks(context.Hits),
            Recommendations = InsightExtractor.Recommendations(context.Hits),
            Citations = context.Citations,
            Confidence = ConfidenceAssessment.FromScore(context.AverageScore, context.Citations.Count)
        };
    }

    public async ValueTask<CaseTimelineResult> BuildTimelineAsync(string caseId, CancellationToken cancellationToken = default)
    {
        var chunks = await _search.GetCaseChunksAsync(caseId, cancellationToken);
        var events = TimelineExtractor.Extract(chunks);

        var citations = events
            .SelectMany(e => e.Citations)
            .GroupBy(c => c.ChunkId, StringComparer.Ordinal)
            .Select(g => g.First())
            .ToList();

        var score = events.Count == 0 ? 0d : Math.Clamp(0.5 + (0.05 * events.Count), 0d, 1d);
        var rationale = events.Count == 0
            ? "No datable events were found in the indexed pages."
            : $"{events.Count} dated event(s) extracted directly from cited source pages.";

        return new CaseTimelineResult
        {
            CaseId = caseId,
            Events = events,
            Citations = citations,
            Confidence = ConfidenceAssessment.FromScore(score, citations.Count, rationale)
        };
    }

    public async ValueTask<LegalAnswerResult> AnswerAsync(
        string? caseId,
        string question,
        IReadOnlyList<ChatTurn> history,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(question, history);
        var context = await _grounder.GroundAsync(query, caseId, mode: SearchMode.Hybrid, cancellationToken: cancellationToken);

        if (!context.HasResults)
        {
            return new LegalAnswerResult
            {
                CaseId = caseId ?? string.Empty,
                Question = question,
                Answer = "The indexed case record does not contain information to answer this question. " +
                         "Please confirm the case has been ingested, or consult the source documents directly.",
                Confidence = ConfidenceAssessment.FromScore(0d, 0, "No supporting passages were retrieved.")
            };
        }

        var userPrompt = $"QUESTION: {question}\n\nCONTEXT:\n{context.Text}";
        var answer = await _chat.CompleteAsync(CcdaPrompts.LegalQa, userPrompt, cancellationToken);

        return new LegalAnswerResult
        {
            CaseId = caseId ?? string.Empty,
            Question = question,
            Answer = answer,
            Citations = context.Citations,
            Confidence = ConfidenceAssessment.FromScore(context.AverageScore, context.Citations.Count),
            FollowUpSuggestions = InsightExtractor.FollowUps(context.Hits, caseId ?? "the case")
        };
    }

    private static string BuildQuery(string question, IReadOnlyList<ChatTurn> history)
    {
        var lastUser = history.LastOrDefault(t => string.Equals(t.Role, "user", StringComparison.OrdinalIgnoreCase));
        return lastUser is null ? question : $"{lastUser.Content} {question}";
    }

    private static string NoData(string caseId) =>
        $"No indexed documents were found for case {caseId}. Ingest the case before requesting AI review.";
}
