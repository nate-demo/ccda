using CCDA.DataGen.Options;
using CCDA.Shared.Enums;
using CCDA.Shared.Models;

namespace CCDA.DataGen.Generators;

/// <summary>Generates fully synthetic <see cref="LegalCase"/> instances from deterministic seeds.</summary>
public interface ICaseGenerator
{
    /// <summary>Generates a single synthetic case.</summary>
    LegalCase GenerateCase(GenerationRequest request);

    /// <summary>Generates <paramref name="count"/> distinct synthetic cases from one base request.</summary>
    IReadOnlyList<LegalCase> GenerateCases(int count, GenerationRequest request);
}

/// <inheritdoc />
public sealed class CaseGenerator : ICaseGenerator
{
    private const int DefaultSeed = 20260101;

    private static readonly IReadOnlyList<CaseType> SelectableTypes =
        Enum.GetValues<CaseType>().Where(t => t != CaseType.Other).ToList();

    private static readonly IReadOnlyList<DocumentType> InvestigationPackage =
        [DocumentType.DetectiveReport, DocumentType.InvestigationNotes, DocumentType.WitnessInterview, DocumentType.EvidenceLog, DocumentType.TimelineHistory];

    private static readonly IReadOnlyList<DocumentType> InmateHistoryPackage =
        [DocumentType.IntakeRecord, DocumentType.FacilityTransfer, DocumentType.DisciplinaryAction, DocumentType.ProgramParticipation];

    public LegalCase GenerateCase(GenerationRequest request)
    {
        var seed = request.Seed ?? DefaultSeed;
        return Build(seed, request);
    }

    public IReadOnlyList<LegalCase> GenerateCases(int count, GenerationRequest request)
    {
        count = Math.Max(1, count);
        var baseSeed = request.Seed ?? DefaultSeed;
        var cases = new List<LegalCase>(count);
        for (var i = 0; i < count; i++)
        {
            // Derive a distinct-but-stable seed per case so bulk output is reproducible.
            cases.Add(Build(unchecked(baseSeed + (i * 7919)), request));
        }

        return cases;
    }

    private static LegalCase Build(int seed, GenerationRequest request)
    {
        var rng = new DeterministicRandom(seed);
        var caseType = request.CaseType ?? rng.Pick(SelectableTypes);
        var ctx = CaseContextFactory.Create(rng, caseType, request.Complexity);
        var composer = new DocumentComposer(rng);

        var documentTypes = SelectDocumentTypes(rng, request, ctx.Complexity);
        var pageAllocations = AllocatePages(rng, request, documentTypes.Count);

        var ordinals = new Dictionary<DocumentType, int>();
        var documents = new List<CaseDocument>(documentTypes.Count);
        for (var i = 0; i < documentTypes.Count; i++)
        {
            var type = documentTypes[i];
            var ordinal = ordinals.TryGetValue(type, out var used) ? used : 0;
            ordinals[type] = ordinal + 1;
            documents.Add(composer.Compose(ctx, type, pageAllocations[i], ordinal));
        }

        return new LegalCase
        {
            CaseId = ctx.CaseId,
            Title = ctx.Title,
            CaseType = ctx.CaseType,
            Complexity = ctx.Complexity,
            Status = CaseStatus.UnderReview,
            CreatedDate = new DateTimeOffset(ctx.Milestone("Report").Date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
            Documents = documents,
        };
    }

    private static List<DocumentType> SelectDocumentTypes(
        DeterministicRandom rng, GenerationRequest request, CaseComplexity complexity)
    {
        var profile = ComplexityProfile.For(complexity);

        var candidates = complexity switch
        {
            CaseComplexity.Low => new List<DocumentType>
            {
                DocumentType.CaseSummary, DocumentType.ArrestReport,
                DocumentType.WitnessStatement, DocumentType.EvidenceInventory,
            },
            CaseComplexity.High => new List<DocumentType>
            {
                DocumentType.CaseSummary, DocumentType.DefendantProfile, DocumentType.IncidentReport,
                DocumentType.ArrestReport, DocumentType.WitnessStatement, DocumentType.WitnessInterview,
                DocumentType.InvestigatorNotes, DocumentType.DetectiveReport, DocumentType.EvidenceInventory,
                DocumentType.EvidenceLog, DocumentType.ProsecutorNotes, DocumentType.CourtFiling,
                DocumentType.TimelineHistory,
            },
            _ => new List<DocumentType>
            {
                DocumentType.CaseSummary, DocumentType.DefendantProfile, DocumentType.IncidentReport,
                DocumentType.ArrestReport, DocumentType.WitnessStatement, DocumentType.InvestigatorNotes,
                DocumentType.EvidenceInventory, DocumentType.ProsecutorNotes,
            },
        };

        // Size the core set within the complexity's document-count band.
        var target = rng.Between(profile.MinDocuments, profile.MaxDocuments);
        var selected = candidates.Take(Math.Clamp(target, 1, candidates.Count)).ToList();

        if (request.IncludeInvestigationPackage)
        {
            selected.AddRange(InvestigationPackage.Where(t => !selected.Contains(t)));
        }

        if (request.IncludeInmateHistory)
        {
            selected.AddRange(InmateHistoryPackage);
        }

        return selected;
    }

    private static int[] AllocatePages(DeterministicRandom rng, GenerationRequest request, int documentCount)
    {
        var profile = ComplexityProfile.For(request.Complexity);
        var pages = new int[documentCount];

        if (request.Pages is int target && target > 0)
        {
            var basePages = Math.Max(1, target / documentCount);
            for (var i = 0; i < documentCount; i++)
            {
                pages[i] = basePages;
            }

            var remainder = target - (basePages * documentCount);
            for (var i = 0; i < remainder && i < documentCount; i++)
            {
                pages[i]++;
            }
        }
        else
        {
            for (var i = 0; i < documentCount; i++)
            {
                pages[i] = rng.Between(profile.MinPagesPerDoc, profile.MaxPagesPerDoc);
            }
        }

        return pages;
    }
}
