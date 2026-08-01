using CCDA.DataGen.Generators;
using CCDA.DataGen.Options;
using CCDA.Shared.Constants;
using CCDA.Shared.Enums;

namespace CCDA.Tests;

/// <summary>
/// Verifies the synthetic case generator is deterministic (same seed ⇒ identical output) and
/// that the mandatory fictional-data watermark is applied to the presentation header only —
/// never to the searchable/citable page body.
/// </summary>
public sealed class DataGenDeterminismTests
{
    private static readonly ICaseGenerator Generator = new CaseGenerator();

    [Fact]
    public void SameSeed_ProducesIdenticalCase()
    {
        var request = new GenerationRequest { Seed = 42, Complexity = CaseComplexity.Medium };

        var first = Generator.GenerateCase(request);
        var second = Generator.GenerateCase(request);

        Assert.Equal(first.CaseId, second.CaseId);
        Assert.Equal(first.DocumentCount, second.DocumentCount);
        Assert.Equal(first.PageCount, second.PageCount);
        Assert.Equal(first.CaseType, second.CaseType);
        // Full document text must match byte-for-byte for a reproducible demo.
        Assert.Equal(
            first.Documents.Select(d => d.FullText),
            second.Documents.Select(d => d.FullText));
    }

    [Fact]
    public void DifferentSeed_ProducesDifferentCase()
    {
        var a = Generator.GenerateCase(new GenerationRequest { Seed = 1 });
        var b = Generator.GenerateCase(new GenerationRequest { Seed = 2 });

        Assert.NotEqual(a.CaseId, b.CaseId);
    }

    [Fact]
    public void BulkGeneration_IsReproducibleAndDistinct()
    {
        var request = new GenerationRequest { Seed = 99 };

        var run1 = Generator.GenerateCases(3, request);
        var run2 = Generator.GenerateCases(3, request);

        Assert.Equal(3, run1.Count);
        // Reproducible across runs...
        Assert.Equal(run1.Select(c => c.CaseId), run2.Select(c => c.CaseId));
        // ...but each case within a run is distinct.
        Assert.Equal(3, run1.Select(c => c.CaseId).Distinct().Count());
    }

    [Fact]
    public void RequestedCaseType_IsHonored()
    {
        var legalCase = Generator.GenerateCase(new GenerationRequest { Seed = 7, CaseType = CaseType.Homicide });
        Assert.Equal(CaseType.Homicide, legalCase.CaseType);
    }

    [Fact]
    public void Watermark_IsInHeaderButNotInSearchableText()
    {
        var legalCase = Generator.GenerateCase(new GenerationRequest { Seed = 42 });

        var pages = legalCase.Documents.SelectMany(d => d.Pages).ToList();
        Assert.NotEmpty(pages);

        foreach (var page in pages)
        {
            // The watermark banner is present for viewing...
            Assert.Contains(CcdaConstants.DemoWatermarkLine1, page.Header, StringComparison.Ordinal);
            Assert.Contains(CcdaConstants.DemoWatermarkLine2, page.Header, StringComparison.Ordinal);
            // ...but must never bleed into the citable body text.
            Assert.DoesNotContain(CcdaConstants.DemoWatermarkLine1, page.Text, StringComparison.Ordinal);
        }
    }
}
