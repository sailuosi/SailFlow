using System;
using System.Xml;
using sailflow.engine.Ids;

namespace sailflow.runtime.Ids;

/// <summary>runtime 内置 BPMN codec：Guid → Process_{N}；非法旧 id 时发新设计键并 Rewrite。</summary>
public sealed class DefaultSailBpmnIdCodec : ISailBpmnIdCodec
{
    private const string BpmnPrefix = "Process_";

    private readonly ISailIdSource _ids;

    public static DefaultSailBpmnIdCodec Instance { get; } =
        new(GuidStringIdSource.Instance);

    public DefaultSailBpmnIdCodec(ISailIdSource ids)
    {
        _ids = ids ?? throw new ArgumentNullException(nameof(ids));
    }

    public string ToBpmnElementId(string processId)
    {
        var id = string.IsNullOrWhiteSpace(processId) ? "Process_1" : processId.Trim();
        if (Guid.TryParse(id, out var g))
            return BpmnPrefix + g.ToString("N");

        var c0 = id[0];
        if (c0 is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or '_')
            return id;
        return BpmnPrefix + id;
    }

    public string FromBpmnElementId(string bpmnOrProcessId, out bool generatedNew)
    {
        generatedNew = false;
        var id = string.IsNullOrWhiteSpace(bpmnOrProcessId) ? "" : bpmnOrProcessId.Trim();
        if (id.Length == 0)
        {
            generatedNew = true;
            return _ids.NextProcessDesignId();
        }

        if (Guid.TryParse(id, out var g1))
            return g1.ToString("D");

        if (id.StartsWith(BpmnPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var rest = id.Substring(BpmnPrefix.Length);
            if (Guid.TryParse(rest, out var g2))
                return g2.ToString("D");
            if (rest.Length == 32 && Guid.TryParseExact(rest, "N", out var g3))
                return g3.ToString("D");
        }

        generatedNew = true;
        return _ids.NextProcessDesignId();
    }

    public string FromBpmnElementId(string bpmnOrProcessId)
        => FromBpmnElementId(bpmnOrProcessId, out _);

    public string TruncateCode(string code, int maxLen = 64)
    {
        if (string.IsNullOrEmpty(code)) return code ?? "";
        return code.Length <= maxLen ? code : code.Substring(0, maxLen);
    }

    public string RewriteProcessElementId(string xmlContent, string oldId, string newBpmnId)
    {
        if (string.IsNullOrWhiteSpace(xmlContent) || string.IsNullOrWhiteSpace(newBpmnId))
            return xmlContent;

        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xmlContent);
        var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
        nsmgr.AddNamespace("bpmn", "http://www.omg.org/spec/BPMN/20100524/MODEL");
        nsmgr.AddNamespace("bpmndi", "http://www.omg.org/spec/BPMN/20100524/DI");

        var processNode = xmlDoc.SelectSingleNode("//bpmn:process", nsmgr)
            ?? xmlDoc.SelectSingleNode("//*[local-name()='process']");
        if (processNode?.Attributes == null)
            return xmlContent;

        var currentId = processNode.Attributes["id"]?.Value;
        var fromId = !string.IsNullOrEmpty(oldId) ? oldId : currentId;
        if (processNode.Attributes["id"] != null)
            processNode.Attributes["id"]!.Value = newBpmnId;
        else
        {
            var attr = xmlDoc.CreateAttribute("id");
            attr.Value = newBpmnId;
            processNode.Attributes.Append(attr);
        }

        if (!string.IsNullOrEmpty(fromId) && !string.Equals(fromId, newBpmnId, StringComparison.Ordinal))
        {
            var planes = xmlDoc.SelectNodes("//bpmndi:BPMNPlane", nsmgr)
                ?? xmlDoc.SelectNodes("//*[local-name()='BPMNPlane']");
            if (planes != null)
            {
                foreach (XmlNode plane in planes)
                {
                    var be = plane.Attributes?["bpmnElement"];
                    if (be != null && string.Equals(be.Value, fromId, StringComparison.Ordinal))
                        be.Value = newBpmnId;
                }
            }
        }

        return xmlDoc.OuterXml;
    }
}
