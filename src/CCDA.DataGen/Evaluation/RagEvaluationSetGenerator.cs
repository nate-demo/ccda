using CCDA.Shared.Enums;
using CCDA.Shared.Models;

namespace CCDA.DataGen.Evaluation;

/// <summary>A single grounded RAG evaluation item: a question with the source page that answers it.</summary>
public sealed record RagEvaluationItem
{
    public required string Question { get; init; }

    public required string ExpectedCaseId { get; init; }

    public required string ExpectedSourceFile { get; init; }

    public DocumentType ExpectedDocumentType { get; init; }

    /// <summary>A phrase that should appear in a correct, grounded answer.</summary>
    public string ExpectedKeyphrase { get; init; } = string.Empty;
}

/// <summary>Builds a deterministic RAG evaluation set from generated cases.</summary>
public sealed class RagEvaluationSetGenerator
{
    public IReadOnlyList<RagEvaluationItem> Generate(IEnumerable<LegalCase> cases)
    {
        var items = new List<RagEvaluationItem>();
        foreach (var legalCase in cases)
        {
            items.AddRange(ForCase(legalCase));
        }

        return items;
    }

    private static IEnumerable<RagEvaluationItem> ForCase(LegalCase legalCase)
    {
        foreach (var template in Templates)
        {
            var doc = legalCase.Documents.FirstOrDefault(d => template.PreferredTypes.Contains(d.DocumentType));
            if (doc is null)
            {
                continue;
            }

            yield return new RagEvaluationItem
            {
                Question = template.Question(legalCase.CaseId),
                ExpectedCaseId = legalCase.CaseId,
                ExpectedSourceFile = doc.SourceFile,
                ExpectedDocumentType = doc.DocumentType,
                ExpectedKeyphrase = template.Keyphrase,
            };
        }
    }

    private sealed record Template(
        Func<string, string> Question,
        IReadOnlyList<DocumentType> PreferredTypes,
        string Keyphrase);

    private static readonly IReadOnlyList<Template> Templates =
    [
        new(id => $"What is the primary charge in case {id}?",
            [DocumentType.CaseSummary, DocumentType.ProsecutorNotes, DocumentType.CourtFiling], "charge"),
        new(id => $"When was the defendant arrested in case {id}?",
            [DocumentType.ArrestReport, DocumentType.CaseSummary], "arrest"),
        new(id => $"What evidence was collected in case {id}?",
            [DocumentType.EvidenceInventory, DocumentType.EvidenceLog], "evidence"),
        new(id => $"Who is the assigned prosecutor for case {id}?",
            [DocumentType.ProsecutorNotes, DocumentType.CaseSummary], "prosecutor"),
        new(id => $"What did witnesses report in case {id}?",
            [DocumentType.WitnessStatement, DocumentType.WitnessInterview], "witness"),
    ];
}
