using System.Security.Cryptography;
using System.Text;
using CCDA.Search.Abstractions;
using CCDA.Search.Options;
using Microsoft.Extensions.Options;

namespace CCDA.Search.Embeddings;

/// <summary>
/// A deterministic, offline embedding service. It projects text into a fixed-dimension
/// vector using feature hashing over word unigrams and bigrams with sub-linear term
/// weighting, then L2-normalizes. It is NOT a semantic model, but it produces stable,
/// reproducible vectors whose cosine similarity meaningfully ranks lexically related
/// passages — enough to demonstrate hybrid retrieval and citations fully offline.
/// </summary>
public sealed class LocalEmbeddingService : IEmbeddingService
{
    private readonly int _dimensions;

    public LocalEmbeddingService(IOptions<EmbeddingOptions> options)
    {
        _dimensions = Math.Max(64, options.Value.Dimensions);
    }

    public int Dimensions => _dimensions;

    public string ProviderName => "Local (deterministic hashing)";

    public ValueTask<float[]> GenerateAsync(string text, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(Embed(text));

    public ValueTask<IReadOnlyList<float[]>> GenerateBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        var result = new float[texts.Count][];
        for (var i = 0; i < texts.Count; i++)
        {
            result[i] = Embed(texts[i]);
        }

        return ValueTask.FromResult<IReadOnlyList<float[]>>(result);
    }

    private float[] Embed(string text)
    {
        var vector = new float[_dimensions];
        if (string.IsNullOrWhiteSpace(text))
        {
            return vector;
        }

        var tokens = Tokenize(text);
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);

        for (var i = 0; i < tokens.Count; i++)
        {
            Accumulate(counts, tokens[i]);
            if (i + 1 < tokens.Count)
            {
                Accumulate(counts, tokens[i] + "_" + tokens[i + 1]);
            }
        }

        foreach (var (term, count) in counts)
        {
            var bucket = Bucket(term);
            var sign = (Bucket(term + "#sign") & 1) == 0 ? 1f : -1f;
            var weight = 1f + (float)Math.Log(count);
            vector[bucket] += sign * weight;
        }

        VectorMath.NormalizeInPlace(vector);
        return vector;
    }

    private static void Accumulate(Dictionary<string, int> counts, string term)
        => counts[term] = counts.TryGetValue(term, out var c) ? c + 1 : 1;

    private static List<string> Tokenize(string text)
    {
        var tokens = new List<string>();
        var sb = new StringBuilder();
        foreach (var ch in text)
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(char.ToLowerInvariant(ch));
            }
            else if (sb.Length > 0)
            {
                tokens.Add(sb.ToString());
                sb.Clear();
            }
        }

        if (sb.Length > 0)
        {
            tokens.Add(sb.ToString());
        }

        return tokens;
    }

    private int Bucket(string term)
    {
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(Encoding.UTF8.GetBytes(term), hash);
        var value = BitConverter.ToUInt32(hash);
        return (int)(value % (uint)_dimensions);
    }
}
