namespace CCDA.Shared.Models;

/// <summary>A dated event extracted for a case timeline, with supporting citations.</summary>
public sealed record TimelineEvent
{
    /// <summary>Parsed event date when available.</summary>
    public DateOnly? Date { get; init; }

    /// <summary>Raw date text as it appeared in the source (e.g. "on or about March 3").</summary>
    public string DateText { get; init; } = string.Empty;

    public required string Title { get; init; }

    public string Description { get; init; } = string.Empty;

    public IReadOnlyList<Citation> Citations { get; init; } = [];
}
