using CCDA.Shared.Enums;

namespace CCDA.DataGen.Options;

/// <summary>Parameters for generating a single synthetic case (mirrors the CLI/API options).</summary>
public sealed record GenerationRequest
{
    /// <summary>When null, a case type is chosen deterministically from the seed.</summary>
    public CaseType? CaseType { get; init; }

    public CaseComplexity Complexity { get; init; } = CaseComplexity.Medium;

    /// <summary>Target total page volume. When null, complexity decides the page count.</summary>
    public int? Pages { get; init; }

    /// <summary>Deterministic seed. When null, a stable default seed is used.</summary>
    public int? Seed { get; init; }

    /// <summary>Adds an investigation package (detective report, interviews, evidence log, timeline).</summary>
    public bool IncludeInvestigationPackage { get; init; }

    /// <summary>Adds an inmate/custody history package (intake, transfers, discipline, programs).</summary>
    public bool IncludeInmateHistory { get; init; }
}

/// <summary>Document-count / page-volume presets that a <see cref="CaseComplexity"/> maps to.</summary>
internal sealed record ComplexityProfile(int MinDocuments, int MaxDocuments, int MinPagesPerDoc, int MaxPagesPerDoc)
{
    public static ComplexityProfile For(CaseComplexity complexity) => complexity switch
    {
        CaseComplexity.Low => new ComplexityProfile(3, 4, 1, 2),
        CaseComplexity.High => new ComplexityProfile(8, 12, 3, 6),
        _ => new ComplexityProfile(5, 7, 2, 4),
    };
}
