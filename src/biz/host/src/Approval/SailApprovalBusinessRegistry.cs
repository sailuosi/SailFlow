using System;
using System.Collections.Generic;

namespace sailflow.biz.Approval;

/// <summary>按 FormCode 路由；同码重复登记失败。</summary>
public sealed class SailApprovalBusinessRegistry
{
    readonly Dictionary<string, ISailApprovalBusiness> _map;

    public SailApprovalBusinessRegistry(IEnumerable<ISailApprovalBusiness> handlers)
    {
        _map = new Dictionary<string, ISailApprovalBusiness>(StringComparer.OrdinalIgnoreCase);
        foreach (var h in handlers)
        {
            if (h == null || string.IsNullOrWhiteSpace(h.FormCode))
                continue;
            var code = h.FormCode.Trim();
            if (_map.ContainsKey(code))
            {
                throw new InvalidOperationException(
                    $"Duplicate approval business FormCode '{code}'. Each FormCode may register only once.");
            }
            _map[code] = h;
        }
    }

    public ISailApprovalBusiness? TryGet(string? formCode)
    {
        if (string.IsNullOrWhiteSpace(formCode)) return null;
        return _map.TryGetValue(formCode.Trim(), out var h) ? h : null;
    }

    public ISailApprovalBusiness GetRequired(string formCode)
    {
        var h = TryGet(formCode);
        if (h == null)
        {
            throw new InvalidOperationException(
                $"No approval business registered for FormCode '{formCode}'.");
        }
        return h;
    }

    public IReadOnlyCollection<string> RegisteredCodes => _map.Keys.ToList();

    public IReadOnlyCollection<ISailApprovalBusiness> Handlers => _map.Values.ToList();
}
