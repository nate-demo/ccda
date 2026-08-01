using CCDA.Shared.Enums;
using CCDA.Shared.Search;

namespace CCDA.AI.Workflows;

/// <summary>
/// Derives grounded bullet insights (key points, findings, risks, recommendations, follow-ups)
/// from retrieval hits. These are extractive/heuristic so they remain consistent regardless of
/// the chat provider and always map to retrieved source pages.
/// </summary>
internal static class InsightExtractor
{
    private static readonly string[] RiskKeywords =
    [
        "inconsistent", "contradict", "unavailable", "missing", "unable", "no record",
        "not located", "uncorroborated", "recanted", "declined", "refused", "unknown",
        "conflicting", "gap", "lost", "unclear", "disputed"
    ];

    public static IReadOnlyList<string> KeyPoints(IReadOnlyList<SearchHit> hits, int max)
        => hits.Take(max).Select(h => FirstSentence(h.Chunk.Content))
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static IReadOnlyList<string> Findings(IReadOnlyList<SearchHit> hits, int max)
        => hits.Take(max)
            .Select(h => $"{FirstSentence(h.Chunk.Content)} ({h.Chunk.SourceFile} p.{h.Chunk.PageNumber})")
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static IReadOnlyList<string> Risks(IReadOnlyList<SearchHit> hits)
    {
        var risks = hits
            .Where(h => RiskKeywords.Any(k => h.Chunk.Content.Contains(k, StringComparison.OrdinalIgnoreCase)))
            .Select(h => $"Potential evidentiary gap noted in {h.Chunk.SourceFile} (p.{h.Chunk.PageNumber}): {FirstSentence(h.Chunk.Content)}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .ToList();

        if (risks.Count == 0)
        {
            risks.Add("No explicit evidentiary gaps surfaced in the retrieved passages; corroborate witness statements against physical evidence before charging.");
        }

        return risks;
    }

    public static IReadOnlyList<string> Recommendations(IReadOnlyList<SearchHit> hits)
    {
        var presentTypes = hits.Select(h => h.Chunk.DocumentType).Distinct().ToList();
        var recs = new List<string>
        {
            "Verify every AI-surfaced fact against the cited source page before relying on it.",
            "Confirm the chain of custody for all physical and digital evidence."
        };

        if (presentTypes.Contains(DocumentType.WitnessStatement) || presentTypes.Contains(DocumentType.WitnessInterview))
        {
            recs.Add("Re-interview key witnesses to resolve any inconsistencies across statements.");
        }

        if (presentTypes.Contains(DocumentType.ArrestReport) || presentTypes.Contains(DocumentType.IncidentReport))
        {
            recs.Add("Confirm that the arrest/incident report supports each element of the contemplated charges.");
        }

        recs.Add("Route the final charging decision to a supervising attorney; AI output is advisory only.");
        return recs.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static IReadOnlyList<string> FollowUps(IReadOnlyList<SearchHit> hits, string caseId)
    {
        var types = hits.Select(h => h.Chunk.DocumentType).Distinct().Take(3).ToList();
        var suggestions = types
            .Select(t => $"What do the {Humanize(t)} documents say about the timeline?")
            .ToList();

        suggestions.Add($"Which pages in case {caseId} best support the primary charge?");
        return suggestions.Distinct(StringComparer.OrdinalIgnoreCase).Take(4).ToList();
    }

    public static string FirstSentence(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var idx = content.IndexOfAny(['.', '!', '?']);
        var sentence = (idx < 0 ? content : content[..(idx + 1)]).Trim();
        return sentence.Length > 200 ? sentence[..200].TrimEnd() + "\u2026" : sentence;
    }

    private static string Humanize(DocumentType type)
    {
        var name = type.ToString();
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]))
            {
                sb.Append(' ');
            }

            sb.Append(char.ToLowerInvariant(name[i]));
        }

        return sb.ToString();
    }
}
