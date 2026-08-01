namespace CCDA.DataGen.Content;

/// <summary>
/// Pools of purely fictional names, places, and organizations used to compose synthetic cases.
/// None of these correspond to real people, agencies, or events — all CCDA data is fabricated
/// for demonstration purposes only.
/// </summary>
internal static class SyntheticContent
{
    public static readonly IReadOnlyList<string> FirstNames =
    [
        "Avery", "Jordan", "Riley", "Morgan", "Casey", "Taylor", "Quinn", "Rowan", "Devon", "Harper",
        "Emerson", "Sawyer", "Reese", "Finley", "Marlowe", "Sydney", "Blair", "Corey", "Dana", "Elliot",
        "Frankie", "Gray", "Hollis", "Indigo", "Jules", "Kendall", "Lane", "Micah", "Noel", "Orion"
    ];

    public static readonly IReadOnlyList<string> LastNames =
    [
        "Calderon", "Whitfield", "Ashcombe", "Delacroix", "Sorensen", "Marlowe", "Underhill", "Pennington",
        "Rhodes", "Vance", "Locke", "Beaumont", "Castellan", "Draytonn", "Everhart", "Fairbanks", "Grimsby",
        "Halloway", "Ingram", "Jessup", "Kingsley", "Lindqvist", "Montague", "Norcross", "Ovett", "Prescott",
        "Quimby", "Ravenscroft", "Stanhope", "Thornbury"
    ];

    /// <summary>Fictional municipalities inside the fictional Contoso County.</summary>
    public static readonly IReadOnlyList<string> Cities =
    [
        "Bayside Heights", "Fern Hollow", "Cedar Junction", "Maple Crossing", "Lakemont",
        "Rivertown", "Ashford", "Sterling Park", "Northgate", "Old Mill"
    ];

    public static readonly IReadOnlyList<string> Streets =
    [
        "Sycamore Avenue", "Birchwood Lane", "Harbor Street", "Kestrel Road", "Juniper Way",
        "Meridian Boulevard", "Copperfield Court", "Willowbank Drive", "Sundial Terrace", "Ironwood Place"
    ];

    public static readonly IReadOnlyList<string> Agencies =
    [
        "Contoso County Sheriff's Office", "Bayside Heights Police Department",
        "Rivertown Metro Police", "Contoso County Investigations Bureau"
    ];

    public static readonly IReadOnlyList<string> Judges =
    [
        "Hon. P. Wexler", "Hon. R. Amado", "Hon. L. Fontaine", "Hon. S. Okafor", "Hon. D. Brennan"
    ];

    public static readonly IReadOnlyList<string> DefenseCounsel =
    [
        "M. Ellsworth, Esq.", "T. Nakamura, Esq.", "B. Ostrowski, Esq.", "C. Villalobos, Esq."
    ];

    public static readonly IReadOnlyList<string> Prosecutors =
    [
        "ADA J. Halloran", "ADA K. Speyer", "ADA N. Ibarra", "ADA R. Whitmore"
    ];

    public static readonly IReadOnlyList<string> WitnessRoles =
    [
        "neighbor", "store clerk", "coworker", "bystander", "building superintendent",
        "delivery driver", "security guard", "responding paramedic"
    ];

    public static readonly IReadOnlyList<string> Facilities =
    [
        "Contoso County Detention Center", "Northgate Correctional Facility", "Rivertown Holding Annex"
    ];

    public static readonly IReadOnlyList<string> Programs =
    [
        "Vocational Training", "Substance Recovery", "GED Completion", "Anger Management", "Work Release"
    ];
}
