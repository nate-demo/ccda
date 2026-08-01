namespace CCDA.DataGen.Generators;

/// <summary>
/// A small deterministic pseudo-random helper. Wrapping <see cref="Random"/> keeps every
/// generator reproducible: the same seed always yields byte-for-byte identical synthetic data,
/// which is essential for repeatable demos and tests.
/// </summary>
public sealed class DeterministicRandom
{
    private readonly Random _random;

    public DeterministicRandom(int seed) => _random = new Random(seed);

    /// <summary>Inclusive-exclusive integer in [min, max).</summary>
    public int Next(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);

    /// <summary>Inclusive integer in [min, max].</summary>
    public int Between(int minInclusive, int maxInclusive) => _random.Next(minInclusive, maxInclusive + 1);

    public double NextDouble() => _random.NextDouble();

    public bool Chance(double probability) => _random.NextDouble() < probability;

    public T Pick<T>(IReadOnlyList<T> items) => items[_random.Next(items.Count)];

    /// <summary>Picks <paramref name="count"/> distinct items (or all, if fewer exist).</summary>
    public IReadOnlyList<T> PickDistinct<T>(IReadOnlyList<T> items, int count)
    {
        if (count >= items.Count)
        {
            return Shuffle(items);
        }

        var pool = items.ToList();
        var result = new List<T>(count);
        for (var i = 0; i < count; i++)
        {
            var idx = _random.Next(pool.Count);
            result.Add(pool[idx]);
            pool.RemoveAt(idx);
        }

        return result;
    }

    public IReadOnlyList<T> Shuffle<T>(IReadOnlyList<T> items)
    {
        var copy = items.ToList();
        for (var i = copy.Count - 1; i > 0; i--)
        {
            var j = _random.Next(i + 1);
            (copy[i], copy[j]) = (copy[j], copy[i]);
        }

        return copy;
    }
}
