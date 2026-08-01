using System.Collections.Concurrent;
using CCDA.Search.Abstractions;
using CCDA.Shared.Models;

namespace CCDA.Search.Providers;

/// <summary>
/// Thread-safe in-memory case catalog. Suitable for the local demo and as a drop-in default;
/// an Azure-backed catalog (e.g. Table Storage / Cosmos) can replace it without touching callers.
/// </summary>
public sealed class InMemoryCaseCatalog : ICaseCatalog
{
    private readonly ConcurrentDictionary<string, (long Seq, CaseRecord Record)> _cases = new(StringComparer.OrdinalIgnoreCase);
    private long _sequence;

    public void Record(LegalCase legalCase)
    {
        ArgumentNullException.ThrowIfNull(legalCase);

        var documents = legalCase.Documents
            .Select(d => new CaseDocumentRecord(d.DocumentId, d.DocumentType, d.Title, d.SourceFile, d.PageCount))
            .ToList();

        var record = new CaseRecord
        {
            CaseId = legalCase.CaseId,
            Title = legalCase.Title,
            CaseType = legalCase.CaseType,
            Complexity = legalCase.Complexity,
            Status = legalCase.Status,
            CreatedDate = legalCase.CreatedDate,
            Classification = legalCase.Classification,
            Documents = documents,
        };

        var seq = Interlocked.Increment(ref _sequence);
        _cases[legalCase.CaseId] = (seq, record);
    }

    public CaseRecord? Get(string caseId) =>
        _cases.TryGetValue(caseId, out var entry) ? entry.Record : null;

    public IReadOnlyList<CaseRecord> List() =>
        _cases.Values
            .OrderByDescending(e => e.Seq)
            .Select(e => e.Record)
            .ToList();

    public bool Remove(string caseId) => _cases.TryRemove(caseId, out _);
}
