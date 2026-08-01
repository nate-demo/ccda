using CCDA.Shared.Enums;

namespace CCDA.DataGen.Content;

/// <summary>Per-case-type narrative building blocks (charges, evidence, and offense phrasing).</summary>
internal sealed record CaseNarrative(
    string OffenseNoun,
    string OffenseVerb,
    IReadOnlyList<string> Charges,
    IReadOnlyList<string> EvidenceItems,
    IReadOnlyList<string> TitleTemplates);

internal static class CaseNarratives
{
    public static CaseNarrative For(CaseType type) => Catalog.TryGetValue(type, out var n) ? n : Catalog[CaseType.Other];

    private static readonly IReadOnlyDictionary<CaseType, CaseNarrative> Catalog = new Dictionary<CaseType, CaseNarrative>
    {
        [CaseType.Fraud] = new(
            "fraud scheme", "orchestrated a fraudulent billing scheme",
            ["Grand larceny in the second degree", "Falsifying business records", "Scheme to defraud"],
            ["ledger spreadsheets", "forged invoices", "bank transfer records", "email correspondence", "a signed vendor contract"],
            ["State v. {Defendant} — Vendor Billing Fraud", "Financial Fraud Investigation: {Defendant}"]),

        [CaseType.Burglary] = new(
            "burglary", "unlawfully entered the premises",
            ["Burglary in the second degree", "Criminal mischief", "Possession of burglary tools"],
            ["a pry bar recovered on scene", "surveillance video", "latent fingerprints", "recovered stolen property", "a footwear impression"],
            ["State v. {Defendant} — Residential Burglary", "Burglary Investigation at {Location}"]),

        [CaseType.OrganizedCrime] = new(
            "criminal enterprise", "directed an organized criminal enterprise",
            ["Enterprise corruption", "Conspiracy in the fourth degree", "Money laundering"],
            ["intercepted communications", "financial network diagrams", "seized ledgers", "informant statements", "surveillance logs"],
            ["State v. {Defendant} et al. — Enterprise Corruption", "Organized Crime Task Force File: {Defendant}"]),

        [CaseType.FinancialCrime] = new(
            "financial crime", "diverted client funds for personal use",
            ["Grand larceny in the first degree", "Securities fraud", "Falsifying business records"],
            ["brokerage statements", "wire transfer confirmations", "client account records", "a spreadsheet of diverted funds"],
            ["State v. {Defendant} — Investment Fraud", "Financial Crimes Unit File: {Defendant}"]),

        [CaseType.Assault] = new(
            "assault", "struck the victim during an altercation",
            ["Assault in the second degree", "Menacing", "Criminal possession of a weapon"],
            ["medical records", "a recovered weapon", "photographs of injuries", "a 911 call recording", "witness statements"],
            ["State v. {Defendant} — Aggravated Assault", "Assault Investigation at {Location}"]),

        [CaseType.Theft] = new(
            "theft", "removed merchandise without payment",
            ["Petit larceny", "Grand larceny in the fourth degree", "Criminal possession of stolen property"],
            ["store surveillance video", "a recovered receipt", "an inventory audit", "loss-prevention notes"],
            ["State v. {Defendant} — Retail Theft", "Larceny Investigation: {Defendant}"]),

        [CaseType.DrugOffense] = new(
            "controlled-substance offense", "possessed a controlled substance with intent to sell",
            ["Criminal sale of a controlled substance", "Criminal possession of a controlled substance", "Conspiracy"],
            ["a field-test report", "seized packaging", "recorded buy-money serials", "a laboratory analysis", "surveillance photos"],
            ["State v. {Defendant} — Narcotics Distribution", "Controlled Substance Investigation: {Defendant}"]),

        [CaseType.Homicide] = new(
            "homicide", "caused the death of the victim",
            ["Manslaughter in the first degree", "Criminal possession of a weapon", "Tampering with evidence"],
            ["a medical examiner report", "ballistics analysis", "DNA results", "scene photographs", "a recovered firearm"],
            ["State v. {Defendant} — Homicide", "Major Case Squad File: {Defendant}"]),

        [CaseType.Cybercrime] = new(
            "computer intrusion", "gained unauthorized access to protected systems",
            ["Computer trespass", "Identity theft in the first degree", "Unlawful duplication of computer material"],
            ["server access logs", "recovered malware samples", "a forensic disk image", "phishing email headers", "cryptocurrency transaction records"],
            ["State v. {Defendant} — Computer Intrusion", "Cybercrime Unit File: {Defendant}"]),

        [CaseType.Other] = new(
            "criminal matter", "engaged in the alleged conduct",
            ["Criminal charge pending review"],
            ["investigative notes", "witness statements", "physical evidence inventory"],
            ["State v. {Defendant} — Case Review", "Investigation File: {Defendant}"]),
    };
}
