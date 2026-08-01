using System.Text;
using CCDA.Shared.Constants;
using CCDA.Shared.Models;

namespace CCDA.Search.Chunking;

/// <summary>Configuration for splitting page text into overlapping chunks.</summary>
public sealed record ChunkingOptions
{
    /// <summary>Target chunk size in words.</summary>
    public int TargetWords { get; init; } = 180;

    /// <summary>Number of words to overlap between consecutive chunks (preserves context).</summary>
    public int OverlapWords { get; init; } = 30;

    public static ChunkingOptions Default { get; } = new();
}

/// <summary>
/// Splits documents into page-anchored, overlapping chunks. Chunking is deterministic:
/// identical input always yields identical chunks and chunk ids, which keeps demos reproducible.
/// Each chunk preserves the page number so every retrieval result can cite an exact page.
/// </summary>
public static class TextChunker
{
    /// <summary>Chunks every page of a document, preserving page-level citation lineage.</summary>
    public static IReadOnlyList<DocumentChunk> ChunkDocument(CaseDocument document, ChunkingOptions? options = null)
    {
        options ??= ChunkingOptions.Default;
        var chunks = new List<DocumentChunk>();
        var globalIndex = 0;

        foreach (var page in document.Pages.OrderBy(p => p.PageNumber))
        {
            foreach (var (text, indexOnPage) in SplitIntoChunks(page.Text, options))
            {
                chunks.Add(new DocumentChunk
                {
                    ChunkId = $"{document.DocumentId}-p{page.PageNumber}-c{indexOnPage}",
                    DocumentId = document.DocumentId,
                    CaseId = document.CaseId,
                    DocumentType = document.DocumentType,
                    Title = document.Title,
                    SourceFile = document.SourceFile,
                    PageNumber = page.PageNumber,
                    ChunkIndex = globalIndex++,
                    Content = text,
                    CreatedDate = document.CreatedDate,
                    Classification = string.IsNullOrEmpty(document.Classification)
                        ? CcdaConstants.Classification
                        : document.Classification
                });
            }
        }

        return chunks;
    }

    /// <summary>Chunks every document in a case.</summary>
    public static IReadOnlyList<DocumentChunk> ChunkCase(LegalCase legalCase, ChunkingOptions? options = null)
    {
        var chunks = new List<DocumentChunk>();
        foreach (var document in legalCase.Documents)
        {
            chunks.AddRange(ChunkDocument(document, options));
        }

        return chunks;
    }

    private static IEnumerable<(string Text, int Index)> SplitIntoChunks(string text, ChunkingOptions options)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            yield break;
        }

        var step = Math.Max(1, options.TargetWords - options.OverlapWords);
        var index = 0;
        for (var start = 0; start < words.Length; start += step)
        {
            var take = Math.Min(options.TargetWords, words.Length - start);
            var builder = new StringBuilder();
            for (var i = start; i < start + take; i++)
            {
                if (builder.Length > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(words[i]);
            }

            yield return (builder.ToString(), index++);

            if (start + take >= words.Length)
            {
                yield break;
            }
        }
    }
}
