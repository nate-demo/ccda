using System.Globalization;
using CCDA.DataGen.Content;
using CCDA.DataGen.Generators;
using CCDA.Shared.Enums;

namespace CCDA.DataGen.Generators;

/// <summary>A dated milestone in a case, rendered into document text so timelines are citable.</summary>
internal sealed record KeyDate(string Label, DateOnly Date)
{
    public string Text => Date.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture);
}

/// <summary>Fully resolved, deterministic facts for one synthetic case.</summary>
internal sealed record CaseContext
{
    public required string CaseId { get; init; }
    public required CaseType CaseType { get; init; }
    public required CaseComplexity Complexity { get; init; }
    public required CaseNarrative Narrative { get; init; }
    public required string Title { get; init; }
    public required string DefendantName { get; init; }
    public required string VictimName { get; init; }
    public required string Location { get; init; }
    public required string Agency { get; init; }
    public required string Detective { get; init; }
    public required string ArrestingOfficer { get; init; }
    public required string Prosecutor { get; init; }
    public required string Judge { get; init; }
    public required string DefenseCounsel { get; init; }
    public required IReadOnlyList<string> Charges { get; init; }
    public required IReadOnlyList<string> EvidenceItems { get; init; }
    public required IReadOnlyList<(string Name, string Role)> Witnesses { get; init; }
    public required IReadOnlyList<KeyDate> Dates { get; init; }

    public KeyDate Milestone(string label) =>
        Dates.FirstOrDefault(d => d.Label == label) ?? Dates[0];
}

/// <summary>Builds a deterministic <see cref="CaseContext"/> from a seed.</summary>
internal static class CaseContextFactory
{
    public static CaseContext Create(DeterministicRandom rng, CaseType caseType, CaseComplexity complexity)
    {
        var narrative = CaseNarratives.For(caseType);

        var defendant = $"{rng.Pick(SyntheticContent.FirstNames)} {rng.Pick(SyntheticContent.LastNames)}";
        var victim = $"{rng.Pick(SyntheticContent.FirstNames)} {rng.Pick(SyntheticContent.LastNames)}";
        var location = $"{rng.Between(100, 9999)} {rng.Pick(SyntheticContent.Streets)}, {rng.Pick(SyntheticContent.Cities)}";

        var year = rng.Between(2024, 2026);
        var caseNumber = rng.Between(1, 999_999);
        var caseId = $"{CCDA.Shared.Constants.CcdaConstants.CaseIdPrefix}-{year}-{caseNumber:000000}";

        var incident = new DateOnly(year, rng.Between(1, 12), rng.Between(1, 28));
        var dates = new List<KeyDate>
        {
            new("Incident", incident),
            new("Report", incident.AddDays(rng.Between(0, 1))),
            new("Arrest", incident.AddDays(rng.Between(1, 10))),
        };
        dates.Add(new KeyDate("Booking", dates[^1].Date.AddDays(0)));
        dates.Add(new KeyDate("Evidence Submission", dates[^1].Date.AddDays(rng.Between(1, 5))));
        dates.Add(new KeyDate("Arraignment", dates[2].Date.AddDays(rng.Between(2, 14))));
        dates.Add(new KeyDate("Grand Jury", dates[^1].Date.AddDays(rng.Between(10, 40))));

        var witnessCount = complexity switch
        {
            CaseComplexity.Low => rng.Between(1, 2),
            CaseComplexity.High => rng.Between(3, 5),
            _ => rng.Between(2, 3),
        };
        var witnesses = Enumerable.Range(0, witnessCount)
            .Select(_ => ($"{rng.Pick(SyntheticContent.FirstNames)} {rng.Pick(SyntheticContent.LastNames)}",
                          rng.Pick(SyntheticContent.WitnessRoles)))
            .ToList();

        var title = rng.Pick(narrative.TitleTemplates)
            .Replace("{Defendant}", defendant, StringComparison.Ordinal)
            .Replace("{Location}", location, StringComparison.Ordinal);

        return new CaseContext
        {
            CaseId = caseId,
            CaseType = caseType,
            Complexity = complexity,
            Narrative = narrative,
            Title = title,
            DefendantName = defendant,
            VictimName = victim,
            Location = location,
            Agency = rng.Pick(SyntheticContent.Agencies),
            Detective = $"Det. {rng.Pick(SyntheticContent.LastNames)}",
            ArrestingOfficer = $"Officer {rng.Pick(SyntheticContent.LastNames)}",
            Prosecutor = rng.Pick(SyntheticContent.Prosecutors),
            Judge = rng.Pick(SyntheticContent.Judges),
            DefenseCounsel = rng.Pick(SyntheticContent.DefenseCounsel),
            Charges = narrative.Charges,
            EvidenceItems = rng.Shuffle(narrative.EvidenceItems),
            Witnesses = witnesses,
            Dates = dates,
        };
    }
}
