using System.Globalization;
using System.Text.RegularExpressions;
using CCDA.Shared.Models;

namespace CCDA.AI.Workflows;

/// <summary>
/// Extracts a chronological, cited timeline directly from indexed case chunks. Timeline events
/// are built from dates found in the source text, so every event carries a real page citation.
/// This is deliberately extractive (not model-generated) to guarantee citation fidelity.
/// </summary>
public static partial class TimelineExtractor
{
    public static IReadOnlyList<TimelineEvent> Extract(IReadOnlyList<DocumentChunk> chunks, int max = 25)
    {
        var events = new List<(DateOnly? Date, TimelineEvent Event)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var chunk in chunks)
        {
            foreach (Match match in DatePattern().Matches(chunk.Content))
            {
                var (date, dateText) = Parse(match);
                var title = BuildTitle(chunk.Content, match.Index);
                var dedupeKey = $"{dateText}|{title}";
                if (title.Length == 0 || !seen.Add(dedupeKey))
                {
                    continue;
                }

                var citation = new Citation
                {
                    DocumentId = chunk.DocumentId,
                    CaseId = chunk.CaseId,
                    SourceFile = chunk.SourceFile,
                    PageNumber = chunk.PageNumber,
                    ChunkId = chunk.ChunkId,
                    DocumentType = chunk.DocumentType,
                    Snippet = title,
                    Score = 1d
                };

                events.Add((date, new TimelineEvent
                {
                    Date = date,
                    DateText = dateText,
                    Title = title,
                    Description = string.Empty,
                    Citations = [citation]
                }));
            }
        }

        return events
            .OrderBy(e => e.Date ?? DateOnly.MaxValue)
            .ThenBy(e => e.Event.DateText, StringComparer.Ordinal)
            .Select(e => e.Event)
            .Take(max)
            .ToList();
    }

    private static (DateOnly? Date, string Text) Parse(Match match)
    {
        var text = match.Value.Trim();
        string[] formats =
        [
            "yyyy-MM-dd", "M/d/yyyy", "MM/dd/yyyy",
            "MMMM d, yyyy", "MMM d, yyyy", "MMMM d yyyy"
        ];

        if (DateOnly.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return (date, text);
        }

        return DateOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
            ? (date, text)
            : (null, text);
    }

    private static string BuildTitle(string content, int matchIndex)
    {
        // Use the sentence containing the date as the event title.
        var start = content.LastIndexOfAny(['.', '!', '?', '\n'], Math.Min(matchIndex, content.Length - 1));
        start = start < 0 ? 0 : start + 1;

        var end = content.IndexOfAny(['.', '!', '?', '\n'], Math.Min(matchIndex, content.Length - 1));
        end = end < 0 ? content.Length : end;

        var sentence = content[start..end].Trim();
        if (sentence.Length > 200)
        {
            sentence = sentence[..200].TrimEnd() + "\u2026";
        }

        return sentence;
    }

    // ISO dates, US numeric dates, and long/short month-name dates.
    [GeneratedRegex(
        @"\b(\d{4}-\d{2}-\d{2}|\d{1,2}/\d{1,2}/\d{4}|(January|February|March|April|May|June|July|August|September|October|November|December|Jan|Feb|Mar|Apr|Jun|Jul|Aug|Sep|Sept|Oct|Nov|Dec)\.?\s+\d{1,2},?\s+\d{4})\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex DatePattern();
}
