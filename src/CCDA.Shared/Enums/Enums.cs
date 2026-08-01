namespace CCDA.Shared.Enums;

/// <summary>Categories of legal documents produced for a case.</summary>
public enum DocumentType
{
    CaseSummary,
    DefendantProfile,
    ArrestReport,
    IncidentReport,
    WitnessStatement,
    InvestigatorNotes,
    ProsecutorNotes,
    CourtFiling,
    EvidenceInventory,
    IntakeRecord,
    FacilityTransfer,
    DisciplinaryAction,
    ProgramParticipation,
    DetectiveReport,
    WitnessInterview,
    EvidenceLog,
    InvestigationNotes,
    TimelineHistory,
    Other
}

/// <summary>High-level category of a synthetic legal case.</summary>
public enum CaseType
{
    Fraud,
    Burglary,
    OrganizedCrime,
    FinancialCrime,
    Assault,
    Theft,
    DrugOffense,
    Homicide,
    Cybercrime,
    Other
}

/// <summary>Relative complexity, which drives document count and page volume during generation.</summary>
public enum CaseComplexity
{
    Low,
    Medium,
    High
}

/// <summary>Lifecycle status of a case within the DA review workflow.</summary>
public enum CaseStatus
{
    Draft,
    UnderReview,
    Charged,
    Trial,
    Closed
}

/// <summary>Retrieval strategy used against the search index.</summary>
public enum SearchMode
{
    Keyword,
    Vector,
    Hybrid,
    Semantic
}

/// <summary>Qualitative confidence bucket derived from a numeric grounding score.</summary>
public enum ConfidenceLevel
{
    Low,
    Medium,
    High
}

/// <summary>AI orchestration workflows exposed by the platform.</summary>
public enum WorkflowType
{
    Summary,
    Briefing,
    Timeline,
    LegalQa
}
