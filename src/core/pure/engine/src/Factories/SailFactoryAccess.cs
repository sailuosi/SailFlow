using System;
using saiflow.engine.Client;

namespace saiflow.engine.Factories;

/// <summary>
/// 引擎内取具体默认工厂（Create* 为 internal）。D3 调用点经此或直接属性转型。
/// </summary>
internal static class SailFactoryAccess
{
    public static DefaultRuntimeFactory Runtime(SailClient sail)
    {
        ArgumentNullException.ThrowIfNull(sail);
        if (sail.RuntimeFactory is DefaultRuntimeFactory d)
            return d;
        throw new InvalidOperationException(
            "SailClient.RuntimeFactory must be DefaultRuntimeFactory (or subclass) in D2/D3. Use builder.UseRuntimeFactory(...).");
    }

    public static DefaultMediatorFactory Mediator(SailClient sail)
    {
        ArgumentNullException.ThrowIfNull(sail);
        if (sail.MediatorFactory is DefaultMediatorFactory d)
            return d;
        throw new InvalidOperationException(
            "SailClient.MediatorFactory must be DefaultMediatorFactory (or subclass) in D2/D3. Use builder.UseMediatorFactory(...).");
    }

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
