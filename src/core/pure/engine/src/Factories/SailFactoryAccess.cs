using System;
using sailflow.engine.Client;

namespace sailflow.engine.Factories;

/// <summary>
/// 引擎内取 Manager / ProcessModel 默认工厂上的 internal 附属 API。
/// Runtime / Mediator 已走 <see cref="SailClient.RuntimeFactory"/> / <see cref="SailClient.MediatorFactory"/> 真契约，不再经此强转。
/// </summary>
internal static class SailFactoryAccess
{
    public static DefaultManagerFactory Managers(SailClient sail)
    {
        ArgumentNullException.ThrowIfNull(sail);
        if (sail.ManagerFactory is DefaultManagerFactory d)
            return d;
        throw new InvalidOperationException(
            "SailClient.ManagerFactory must be DefaultManagerFactory (or subclass) for ProcessInstances access.");
    }

    public static DefaultProcessModelFactory ProcessModels(SailClient sail)
    {
        ArgumentNullException.ThrowIfNull(sail);
        if (sail.ProcessModelFactory is DefaultProcessModelFactory d)
            return d;
        throw new InvalidOperationException(
            "SailClient.ProcessModelFactory must be DefaultProcessModelFactory (or subclass) in D2/D3. Use builder.UseProcessModelFactory(...).");
    }
}
