using System;
using saiflow.engine.Common;
using saiflow.engine.Factories;
using saiflow.engine.Manager;

namespace saiflow.engine.Client;

/// <summary>
/// A · 四工厂槽 + RunBuilder 入口（D3 已落地）。
/// </summary>
public partial class SailClient
{
    /// <summary>引擎内部：ProcessInstanceManager（类型暂 internal）。</summary>
    internal ProcessInstanceManager ProcessInstances => SailFactoryAccess.Managers(this).ProcessInstances;

    private IRuntimeFactory _runtimeFactory;
    private IMediatorFactory _mediatorFactory;
    private IProcessModelFactory _processModelFactory;
    private IManagerFactory _managerFactory;

    /// <summary>A · Runtime 工厂（可覆写）。</summary>
    public IRuntimeFactory RuntimeFactory
    {
        get => _runtimeFactory ??= new DefaultRuntimeFactory(this);
        set => _runtimeFactory = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>A · Mediator 工厂（可覆写）。</summary>
    public IMediatorFactory MediatorFactory
    {
        get => _mediatorFactory ??= new DefaultMediatorFactory(this);
        set => _mediatorFactory = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>A · ProcessModel 工厂（可覆写）。</summary>
    public IProcessModelFactory ProcessModelFactory
    {
        get => _processModelFactory ??= new DefaultProcessModelFactory(this);
        set => _processModelFactory = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>A · Manager 工厂（可覆写）；不在 Client 上挂齐 Activities/Tasks 属性。</summary>
    public IManagerFactory ManagerFactory
    {
        get => _managerFactory ??= new DefaultManagerFactory(this);
        set => _managerFactory = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>B · 每次 new <see cref="RunBuilder"/>；入参袋为 <see cref="FlowCommandContext"/>；禁止做成属性缓存。</summary>
    public RunBuilder CreateRunBuilder(string userId, string userName)
    {
        var runner = new FlowCommandContext
        {
            UserId = userId,
            UserName = userName,
            TaskId = null,
        };
        return new RunBuilder(this, runner);
    }

    /// <summary>B · 以已有入参袋开建造会话（仍每次 new Builder，不写回 Client 字段）。</summary>
    public RunBuilder CreateRunBuilder(FlowCommandContext runner)
    {
        ArgumentNullException.ThrowIfNull(runner);
        return new RunBuilder(this, runner);
    }

    /// <summary>B · 自动编排建造会话（每次 new；默认 NextPerformer=Unattended；袋为 <see cref="FlowCommandContext"/>）。</summary>
    public AutoRunBuilder CreateAutoRunBuilder(string userId = null, string userName = null)
    {
        var runner = new FlowCommandContext
        {
            UserId = userId,
            UserName = userName,
            NextPerformerType = NextPerformerIntTypeEnum.Unattented,
        };
        return new AutoRunBuilder(this, runner);
    }

    /// <summary>B · 以已有袋开自动编排建造会话。</summary>
    public AutoRunBuilder CreateAutoRunBuilder(FlowCommandContext runner)
    {
        ArgumentNullException.ThrowIfNull(runner);
        return new AutoRunBuilder(this, runner);
    }
}
