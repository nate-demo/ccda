namespace CCDA.Search.Embeddings;

/// <summary>Small vector helpers used by the in-memory retrieval provider.</summary>
public static class VectorMath
{
    public static double CosineSimilarity(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        if (a.Length != b.Length || a.Length == 0)
        {
            return 0d;
        }

        double dot = 0d, magA = 0d, magB = 0d;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * (double)a[i];
            magB += b[i] * (double)b[i];
        }

        if (magA == 0d || magB == 0d)
        {
            return 0d;
        }

        return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
    }

    public static void NormalizeInPlace(float[] vector)
    {
        double sum = 0d;
        foreach (var v in vector)
        {
            sum += v * (double)v;
        }

        if (sum <= 0d)
        {
            return;
        }

        var inv = 1d / Math.Sqrt(sum);
        for (var i = 0; i < vector.Length; i++)
        {
            vector[i] = (float)(vector[i] * inv);
        }
    }
}
