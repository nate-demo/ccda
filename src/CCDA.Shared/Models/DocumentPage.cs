namespace CCDA.Shared.Models;

/// <summary>A single rendered page of a case document.</summary>
public sealed class DocumentPage
{
    public int PageNumber { get; set; }

    /// <summary>
    /// The searchable, citable body text of the page. This is what gets chunked, embedded, and
    /// surfaced in citation snippets — presentation boilerplate (watermark/banner) is kept out of
    /// it via <see cref="Header"/> so it never bleeds into AI snippets or extracted timelines.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Presentation-only page header (fictional-data watermark + page banner). Rendered with the
    /// page for viewing, but excluded from the searchable/citable <see cref="Text"/>.
    /// </summary>
    public string Header { get; set; } = string.Empty;

    /// <summary>The faithful, viewable page: watermark/banner header followed by the body text.</summary>
    public string RenderedText =>
        string.IsNullOrEmpty(Header) ? Text : Header + Environment.NewLine + Environment.NewLine + Text;
}
