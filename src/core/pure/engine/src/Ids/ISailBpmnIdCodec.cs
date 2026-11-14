namespace sailflow.engine.Ids
{
    /// <summary>流程设计键 ProcessId ↔ BPMN process@id / Plane 互转（与发号正交）。</summary>
    public interface ISailBpmnIdCodec
    {
        string ToBpmnElementId(string processId);

        string FromBpmnElementId(string bpmnOrProcessId, out bool generatedNew);

        string FromBpmnElementId(string bpmnOrProcessId);

        string RewriteProcessElementId(string xmlContent, string oldId, string newBpmnId);

        string TruncateCode(string code, int maxLen = 64);
    }
}
