using Commerce.SmartstoreImport.Domain.Entities;

namespace Commerce.SmartstoreImport.Infrastructure.Reconciliation;

internal sealed class ReconciliationMappingIndex
{
    private readonly Dictionary<(string EntityType, int SourceId), ImportIdMapping> _latest = new();
    private readonly Dictionary<(string EntityType, int SourceId), int> _duplicateCounts = new();

    public static ReconciliationMappingIndex FromMappings(IEnumerable<ImportIdMapping> mappings)
    {
        var index = new ReconciliationMappingIndex();
        foreach (var group in mappings.GroupBy(x => (x.EntityType, x.SourceId)))
        {
            var ordered = group.OrderByDescending(x => x.ImportRunId).ToList();
            index._latest[group.Key] = ordered[0];
            if (ordered.Count > 1)
            {
                index._duplicateCounts[group.Key] = ordered.Count;
            }
        }

        return index;
    }

    public bool TryGetTargetId(string entityType, int sourceId, out int targetId)
    {
        if (_latest.TryGetValue((entityType, sourceId), out var mapping))
        {
            targetId = mapping.TargetId;
            return true;
        }

        targetId = default;
        return false;
    }

    public int Count(string entityType) =>
        _latest.Count(x => string.Equals(x.Key.EntityType, entityType, StringComparison.OrdinalIgnoreCase));

    public bool HasDuplicate(string entityType, int sourceId) =>
        _duplicateCounts.TryGetValue((entityType, sourceId), out var count) && count > 1;

    public int DuplicateCount(string entityType, int sourceId) =>
        _duplicateCounts.TryGetValue((entityType, sourceId), out var count) ? count : 0;
}
