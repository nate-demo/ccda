using System.Text;
using CCDA.Shared.Constants;
using CCDA.Shared.Enums;
using CCDA.Shared.Models;

namespace CCDA.DataGen.Generators;

/// <summary>
/// Renders a <see cref="CaseDocument"/> for a given <see cref="DocumentType"/> from a resolved
/// <see cref="CaseContext"/>. Content is deterministic, watermarked on every page, and seeds dated
/// sentences so the extractive timeline workflow can cite real pages.
/// </summary>
internal sealed class DocumentComposer
{
    private readonly DeterministicRandom _rng;

    public DocumentComposer(DeterministicRandom rng) => _rng = rng;

    public CaseDocument Compose(CaseContext ctx, DocumentType type, int targetPages, int ordinal)
    {
        var paragraphs = BuildParagraphs(ctx, type);

        // Ensure there is enough body to fill the requested pages (2 paragraphs per page minimum).
        var minParagraphs = Math.Max(paragraphs.Count, targetPages * 2);
        while (paragraphs.Count < minParagraphs)
        {
            paragraphs.Add(Filler(ctx, type, paragraphs.Count));
        }

        var suffix = ordinal > 0 ? $"_{ordinal + 1}" : string.Empty;
        var sourceFile = $"{ctx.CaseId}_{type}{suffix}.pdf";
        var pages = Paginate(ctx, type, sourceFile, paragraphs, targetPages);

        return new CaseDocument
        {
            DocumentId = $"{ctx.CaseId}-{type}{suffix}",
            CaseId = ctx.CaseId,
            DocumentType = type,
            Title = $"{Humanize(type)} \u2014 {ctx.DefendantName}",
            SourceFile = sourceFile,
            Classification = CcdaConstants.Classification,
            Pages = pages,
        };
    }

    private List<DocumentPage> Paginate(
        CaseContext ctx, DocumentType type, string sourceFile, List<string> paragraphs, int targetPages)
    {
        targetPages = Math.Max(1, targetPages);
        var perPage = (int)Math.Ceiling(paragraphs.Count / (double)targetPages);
        var pages = new List<DocumentPage>();

        for (var p = 0; p < targetPages; p++)
        {
            var slice = paragraphs.Skip(p * perPage).Take(perPage).ToList();
            if (slice.Count == 0 && p > 0)
            {
                break;
            }

            // Presentation-only header: fictional-data watermark + page banner. Kept out of the
            // searchable body so it never leaks into citation snippets or extracted timelines.
            var header = new StringBuilder();
            header.AppendLine(CcdaConstants.DemoWatermarkLine1);
            header.AppendLine(CcdaConstants.DemoWatermarkLine2);
            header.AppendLine();
            header.AppendLine($"[{Humanize(type)}] Case {ctx.CaseId} \u2014 Page {p + 1} of {targetPages}");
            header.Append($"Source: {sourceFile} | Agency: {ctx.Agency} | Classification: {CcdaConstants.Classification}");

            var body = new StringBuilder();
            foreach (var para in slice)
            {
                body.AppendLine(para);
                body.AppendLine();
            }

            pages.Add(new DocumentPage
            {
                PageNumber = p + 1,
                Header = header.ToString(),
                Text = body.ToString().TrimEnd(),
            });
        }

        return pages;
    }

    private List<string> BuildParagraphs(CaseContext ctx, DocumentType type)
    {
        var charge = ctx.Charges[0];
        var incident = ctx.Milestone("Incident");
        var report = ctx.Milestone("Report");
        var arrest = ctx.Milestone("Arrest");
        var evidence = ctx.Milestone("Evidence Submission");
        var arraignment = ctx.Milestone("Arraignment");
        var grandJury = ctx.Milestone("Grand Jury");
        var lead = ctx.EvidenceItems.Count > 0 ? ctx.EvidenceItems[0] : "physical evidence";

        return type switch
        {
            DocumentType.CaseSummary =>
            [
                $"This case summary concerns the People's investigation of {ctx.DefendantName} for {ctx.Narrative.OffenseNoun} allegedly occurring on {incident.Text} at {ctx.Location}.",
                $"On {incident.Text}, it is alleged that {ctx.DefendantName} {ctx.Narrative.OffenseVerb}. The matter was reported on {report.Text} and assigned to {ctx.Detective} of the {ctx.Agency}.",
                $"The defendant was taken into custody on {arrest.Text}. The primary charge under review is {charge}.",
                $"Assigned prosecutor {ctx.Prosecutor} is evaluating the strength of the evidence, which includes {lead}. Defense counsel of record is {ctx.DefenseCounsel}.",
                $"An arraignment was held on {arraignment.Text} before {ctx.Judge}. The matter was presented to the grand jury on {grandJury.Text}.",
            ],
            DocumentType.DefendantProfile =>
            [
                $"Defendant profile for {ctx.DefendantName}, named in case {ctx.CaseId}.",
                $"{ctx.DefendantName} is identified as the subject of a {ctx.Narrative.OffenseNoun} investigation arising from events at {ctx.Location}.",
                $"No adjudication has occurred; the defendant is presumed innocent. This profile is advisory and must be verified against primary records before any charging decision.",
                $"Counsel of record: {ctx.DefenseCounsel}. Assigned prosecutor: {ctx.Prosecutor}.",
            ],
            DocumentType.ArrestReport =>
            [
                $"Arrest report filed by {ctx.ArrestingOfficer}, {ctx.Agency}. Arrest occurred on {arrest.Text} in connection with events at {ctx.Location}.",
                $"The arresting officer states that {ctx.DefendantName} was detained without incident and advised of rights at the scene.",
                $"Probable cause is premised on the allegation that {ctx.DefendantName} {ctx.Narrative.OffenseVerb} on {incident.Text}.",
                $"Recovered at the time of arrest: {lead}. Items were logged and submitted to the evidence unit on {evidence.Text}.",
            ],
            DocumentType.IncidentReport =>
            [
                $"Incident report documenting the events of {incident.Text} at {ctx.Location}.",
                $"Responding units from the {ctx.Agency} were dispatched following a report of a {ctx.Narrative.OffenseNoun} in progress.",
                $"The complainant, {ctx.VictimName}, provided an initial account consistent with the allegation that {ctx.DefendantName} {ctx.Narrative.OffenseVerb}.",
                $"The scene was secured and {lead} was identified for collection.",
            ],
            DocumentType.WitnessStatement or DocumentType.WitnessInterview =>
                BuildWitnessParagraphs(ctx, incident),
            DocumentType.InvestigatorNotes or DocumentType.InvestigationNotes or DocumentType.DetectiveReport =>
            [
                $"Investigative notes prepared by {ctx.Detective} regarding case {ctx.CaseId}.",
                $"Follow-up on {report.Text}: canvassed the area of {ctx.Location} and identified {ctx.Witnesses.Count} potential witness(es).",
                $"On {evidence.Text}, {lead} was submitted to the laboratory for analysis. Chain of custody was documented.",
                $"Working theory: {ctx.DefendantName} {ctx.Narrative.OffenseVerb}. Corroboration of witness accounts against physical evidence remains outstanding.",
            ],
            DocumentType.EvidenceInventory or DocumentType.EvidenceLog =>
                BuildEvidenceParagraphs(ctx, evidence),
            DocumentType.ProsecutorNotes =>
            [
                $"Prosecutor notes ({ctx.Prosecutor}) for case {ctx.CaseId}.",
                $"Charge assessment: {string.Join("; ", ctx.Charges)}.",
                $"Evidentiary strength depends on {lead} and witness corroboration. Note any inconsistencies between statements before charging.",
                $"Arraignment held {arraignment.Text}; grand jury presentation {grandJury.Text}. Attorney judgment governs the final charging decision.",
            ],
            DocumentType.CourtFiling =>
            [
                $"Court filing in the matter of People v. {ctx.DefendantName}, case {ctx.CaseId}, before {ctx.Judge}.",
                $"The People move on the charge of {charge} arising from conduct on {incident.Text}.",
                $"Arraignment was conducted on {arraignment.Text}. The matter was presented to the grand jury on {grandJury.Text}.",
            ],
            DocumentType.IntakeRecord =>
            [
                $"Custody intake record for {ctx.DefendantName} at {(_rng.Pick(Content.SyntheticContent.Facilities))}.",
                $"Booking followed the arrest of {arrest.Text}. Standard intake screening was completed.",
                $"Housing assignment and property inventory were recorded at intake.",
            ],
            DocumentType.FacilityTransfer =>
            [
                $"Facility transfer record for {ctx.DefendantName}.",
                $"Transfer authorized on {arraignment.Text} between custody facilities pending further proceedings.",
                $"Transport was conducted under standard security protocols.",
            ],
            DocumentType.DisciplinaryAction =>
            [
                $"Disciplinary action record for {ctx.DefendantName}.",
                $"An infraction was recorded during custody following intake on {arrest.Text}. A hearing was scheduled per facility policy.",
            ],
            DocumentType.ProgramParticipation =>
            [
                $"Program participation record for {ctx.DefendantName}.",
                $"Enrolled in {_rng.Pick(Content.SyntheticContent.Programs)} beginning {arraignment.Text}. Attendance recorded as satisfactory.",
            ],
            DocumentType.TimelineHistory =>
                BuildTimelineParagraphs(ctx),
            _ =>
            [
                $"Supplemental record for case {ctx.CaseId} concerning {ctx.DefendantName}.",
                $"This document supports the review of a {ctx.Narrative.OffenseNoun} allegation arising on {incident.Text}.",
            ],
        };
    }

    private List<string> BuildWitnessParagraphs(CaseContext ctx, KeyDate incident)
    {
        var paras = new List<string>();
        foreach (var (name, role) in ctx.Witnesses)
        {
            paras.Add($"Witness {name} ({role}) provided a statement regarding the events of {incident.Text} at {ctx.Location}.");
            paras.Add($"{name} reported observing conduct consistent with the allegation against {ctx.DefendantName}. The account is uncorroborated pending further review.");
        }

        if (paras.Count == 0)
        {
            paras.Add($"No witness statements are currently available for case {ctx.CaseId}; witness follow-up is outstanding.");
        }

        return paras;
    }

    private static List<string> BuildEvidenceParagraphs(CaseContext ctx, KeyDate evidence)
    {
        var paras = new List<string>
        {
            $"Evidence inventory for case {ctx.CaseId}. Items were submitted on {evidence.Text} with documented chain of custody.",
        };
        var index = 1;
        foreach (var item in ctx.EvidenceItems)
        {
            paras.Add($"Item {index++:00}: {item}. Collected in connection with the {ctx.Narrative.OffenseNoun} at {ctx.Location} and retained in the evidence unit.");
        }

        return paras;
    }

    private static List<string> BuildTimelineParagraphs(CaseContext ctx)
    {
        var paras = new List<string> { $"Chronological history for case {ctx.CaseId}." };
        paras.AddRange(ctx.Dates.Select(d =>
            $"On {d.Text}, the {d.Label.ToLowerInvariant()} milestone was recorded for the matter involving {ctx.DefendantName}."));
        return paras;
    }

    private static string Filler(CaseContext ctx, DocumentType type, int index)
    {
        var d = ctx.Dates[index % ctx.Dates.Count];
        return $"Continued {Humanize(type).ToLowerInvariant()} for case {ctx.CaseId}: as of {d.Text}, review of the {ctx.Narrative.OffenseNoun} allegation against {ctx.DefendantName} remained ongoing and subject to attorney verification.";
    }

    private static string Humanize(DocumentType type)
    {
        var name = type.ToString();
        var sb = new StringBuilder();
        for (var i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]))
            {
                sb.Append(' ');
            }

            sb.Append(name[i]);
        }

        return sb.ToString();
    }
}
