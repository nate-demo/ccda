using System.Text;
using System.Text.RegularExpressions;
using CCDA.AI.Abstractions;

namespace CCDA.AI.Providers;

/// <summary>
/// A deterministic, offline "LLM" used for local demos. It does not call any model; instead it
/// produces a grounded, extractive narrative from the CONTEXT block supplied by the orchestrator.
/// When a QUESTION is present it ranks context sentences by lexical overlap with the question;
/// otherwise it returns the leading sentences as a summary. Identical input yields identical output.
/// </summary>
public sealed partial class LocalChatCompletionService : IChatCompletionService
{
    public string ProviderName => "Local (extractive, offline)";

    public ValueTask<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
    {
        var context = ExtractSection(userPrompt, "CONTEXT:");
        if (string.IsNullOrWhiteSpace(context))
        {
            return ValueTask.FromResult(
                "The available case documents do not contain enough information to answer confidently. " +
                "An attorney should review the source record directly.");
        }

        var question = ExtractSection(userPrompt, "QUESTION:");
        var sentences = ToSentences(context);
        if (sentences.Count == 0)
        {
            return ValueTask.FromResult(context.Trim());
        }

        List<string> selected = string.IsNullOrWhiteSpace(question)
            ? sentences.Take(6).ToList()
            : RankByOverlap(sentences, question).Take(5).ToList();

        var narrative = string.Join(" ", selected).Trim();
        return ValueTask.FromResult(narrative);
    }

    private static string ExtractSection(string text, string marker)
    {
        var idx = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
        {
            return string.Empty;
        }

        var start = idx + marker.Length;
        // A section runs until the next ALL-CAPS "LABEL:" marker or end of text.
        var next = NextMarker().Match(text, start);
        var end = next.Success ? next.Index : text.Length;
        return text[start..end].Trim();
    }

    private static List<string> ToSentences(string context)
    {
        // Drop the "[n] (file p.x)" citation prefixes so the narrative reads cleanly.
        var cleaned = CitationPrefix().Replace(context, " ");
        cleaned = WhitespaceRun().Replace(cleaned, " ");

        var raw = SentenceSplit().Split(cleaned);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sentences = new List<string>();
        foreach (var s in raw)
        {
            var trimmed = s.Trim();
            if (trimmed.Length < 15)
            {
                continue;
            }

            var normalized = trimmed.EndsWith('.') ? trimmed : trimmed + ".";
            if (seen.Add(normalized))
            {
                sentences.Add(normalized);
            }
        }

        return sentences;
    }

    private static IEnumerable<string> RankByOverlap(List<string> sentences, string question)
    {
        var qTerms = Tokenize(question);
        if (qTerms.Count == 0)
        {
            return sentences.Take(5);
        }

        return sentences
            .Select((s, order) => (Sentence: s, Score: Overlap(s, qTerms), Order: order))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Order)
            .Select(x => x.Sentence)
            .DefaultIfEmpty(sentences[0]);
    }

    private static int Overlap(string sentence, HashSet<string> qTerms)
    {
        var terms = Tokenize(sentence);
        return terms.Count(qTerms.Contains);
    }

    private static HashSet<string> Tokenize(string text)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sb = new StringBuilder();
        foreach (var ch in text)
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(char.ToLowerInvariant(ch));
            }
            else if (sb.Length > 0)
            {
                if (sb.Length > 2)
                {
                    set.Add(sb.ToString());
                }

                sb.Clear();
            }
        }

        if (sb.Length > 2)
        {
            set.Add(sb.ToString());
        }

        return set;
    }

    [GeneratedRegex(@"\[\d+\]\s*\([^)]*\)")]
    private static partial Regex CitationPrefix();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRun();

    [GeneratedRegex(@"(?<=[.!?])\s+")]
    private static partial Regex SentenceSplit();

    [GeneratedRegex(@"\n[A-Z][A-Z ]{2,}:")]
    private static partial Regex NextMarker();
}
