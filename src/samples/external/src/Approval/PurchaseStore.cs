using System;
using System.Collections.Concurrent;

namespace sailflow.external.Approval;

/// <summary>样例内存仓：演示门面回写；宿主请换成真实表 + Kit。</summary>
public sealed class PurchaseStore
{
    readonly ConcurrentDictionary<string, PurchaseRecord> _rows = new(StringComparer.OrdinalIgnoreCase);

    public PurchaseRecord Insert(PurchaseRecord row)
    {
        if (string.IsNullOrWhiteSpace(row.Id))
            row.Id = Guid.NewGuid().ToString("N");
        _rows[row.Id] = row;
        return row;
    }

    public PurchaseRecord? Get(string id)
        => string.IsNullOrWhiteSpace(id) ? null
            : _rows.TryGetValue(id.Trim(), out var row) ? row : null;

    public void Update(PurchaseRecord row)
    {
        if (string.IsNullOrWhiteSpace(row.Id)) return;
        _rows[row.Id] = row;
    }

    public void ApplyStatus(string id, int status, string? progressText = null)
    {
        if (!_rows.TryGetValue(id, out var row)) return;
        row.Status = status;
        if (progressText != null)
            row.ProgressText = progressText;
    }
}
