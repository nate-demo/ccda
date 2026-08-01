using CCDA.Shared.Dtos;
using CCDA.Shared.Workflows;

namespace CCDA.AI.Abstractions;

/// <summary>
/// The AI orchestration surface for CCDA. Every method returns a citation-first result:
/// the narrative comes from the configured model, but citations and the confidence
/// assessment are grounded in retrieval so the AI can never cite a page that was not
/// actually retrieved. AI assists attorney review; it does not replace attorney judgment.
/// </summary>
public interface IAiOrchestrator
{
    ValueTask<CaseSummaryResult> SummarizeAsync(string caseId, CancellationToken cancellationToken = default);

    ValueTask<CaseBriefingResult> BriefAsync(string caseId, CancellationToken cancellationToken = default);

    ValueTask<CaseTimelineResult> BuildTimelineAsync(string caseId, CancellationToken cancellationToken = default);

    ValueTask<LegalAnswerResult> AnswerAsync(
        string? caseId,
        string question,
        IReadOnlyList<ChatTurn> history,
        CancellationToken cancellationToken = default);
}
