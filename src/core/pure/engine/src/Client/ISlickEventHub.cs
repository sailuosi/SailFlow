using System;

namespace sailflow.engine.Client;

/// <summary>
/// 流程/节点事件响应插槽（对齐 BeeFlow EventHub 角色；起步可为 stub）。
/// </summary>
public interface ISailEventHub
{
    /// <summary>发出节点/操作事件（payload 形状后续任务细化）。</summary>
    void Emit(string eventName, object payload = null);
}

/// <summary>默认空实现：记录调试输出，不抛异常。</summary>
public sealed class SailEventHubStub : ISailEventHub
{
    private readonly SailClient _client;

    public SailEventHubStub(SailClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public SailClient Client => _client;

    public void Emit(string eventName, object payload = null)
    {
        // stub：任务 0 仅占位；后续替换为真实事件分发
        System.Diagnostics.Debug.WriteLine($"[SailEvent] {eventName}");
    }
}
