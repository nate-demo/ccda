using CCDA.Shared.Models;

namespace CCDA.Shared.Workflows;

/// <summary>Result of the Summary workflow: a grounded case summary with key points.</summary>
public sealed record CaseSummaryResult
{
    public required string CaseId { get; init; }

    public required string Summary { get; init; }

    public IReadOnlyList<string> KeyPoints { get; init; } = [];

    public IReadOnlyList<Citation> Citations { get; init; } = [];

    public ConfidenceAssessment Confidence { get; init; } = new();
}

/// <summary>Result of the Briefing workflow: an executive briefing structured for attorney review.</summary>
public sealed record CaseBriefingResult
{
    public required string CaseId { get; init; }

    public required string ExecutiveSummary { get; init; }

    public IReadOnlyList<string> KeyFindings { get; init; } = [];

    public IReadOnlyList<string> Risks { get; init; } = [];

    public IReadOnlyList<string> Recommendations { get; init; } = [];

    public IReadOnlyList<Citation> Citations { get; init; } = [];

    public ConfidenceAssessment Confidence { get; init; } = new();
}

/// <summary>Result of the Timeline workflow: chronologically ordered, cited events.</summary>
public sealed record CaseTimelineResult
{
    public required string CaseId { get; init; }

    public IReadOnlyList<TimelineEvent> Events { get; init; } = [];

    public IReadOnlyList<Citation> Citations { get; init; } = [];

    public ConfidenceAssessment Confidence { get; init; } = new();
}

/// <summary>Result of the Legal Q&amp;A workflow: a grounded answer with citations and follow-ups.</summary>
public sealed record LegalAnswerResult
{
    public required string CaseId { get; init; }

    public required string Question { get; init; }

    public required string Answer { get; init; }

    public IReadOnlyList<Citation> Citations { get; init; } = [];

    public ConfidenceAssessment Confidence { get; init; } = new();

    public IReadOnlyList<string> FollowUpSuggestions { get; init; } = [];
}
