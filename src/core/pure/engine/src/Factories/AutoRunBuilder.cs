using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using sailflow.hosting;
using sailflow.utils;
using sailflow.data;
using sailflow.run;
using sailflow.result;
using sailflow.xml;

namespace sailflow.factory;
/// <summary>
/// B · 自动编排建造会话（每次 <c>CreateAutoRunBuilder</c> new）。建成后交给无状态 <see cref="WorkflowExecutor"/>。
/// </summary>
public sealed class AutoRunBuilder
{
    private readonly SailClient _sail;
    private readonly FlowCommandContext _cmdContext;
    private readonly AutoExecutionContext _auto = new AutoExecutionContext();
    private IProcessModel _processModel;
    private string _notifySessionId;
    private Action<string, object> _notifyCallback;

    internal AutoRunBuilder(SailClient sail, FlowCommandContext context)
    {
        _sail = sail ?? throw new ArgumentNullException(nameof(sail));
        _cmdContext = context ?? throw new ArgumentNullException(nameof(context));
        if (_cmdContext.NextPerformerType == default)
            _cmdContext.NextPerformerType = NextPerformerIntTypeEnum.Unattented;
    }

    public AutoRunBuilder UseBiz(BizDataDto biz)
    {
        _cmdContext.ApplyBizData(biz);
        return this;
    }

    public AutoRunBuilder UseBiz(
        string bizPK,
        string bizChannel,
        string bizCode = null,
        string bizType = null,
        string bizName = null)
    {
        return UseBiz(new BizDataDto
        {
            BizPK = bizPK,
            BizChannel = bizChannel,
            BizCode = bizCode,
            BizType = bizType,
            BizName = bizName
        });
    }

    public AutoRunBuilder UseProcess(string processId, string version = null)
    {
        if (string.IsNullOrEmpty(version)) version = "1";

        if (Guid.TryParse(processId, out _))
        {
            _cmdContext.ProcessId = processId;
            _cmdContext.Version = version;
        }
        else
        {
            var entity = _sail.ManagerFactory.Processes.GetByVersion(processId, version);
            _cmdContext.ProcessId = entity.ProcessId;
            _cmdContext.Version = entity.Version;
        }

        _processModel = _sail.ProcessModelFactory.CreateByProcess(_cmdContext.ProcessId, _cmdContext.Version);
        return this;
    }

    public AutoRunBuilder UseProcess(ProcessEntity processEntity)
    {
        ArgumentNullException.ThrowIfNull(processEntity);
        _cmdContext.ProcessId = processEntity.ProcessId;
        _cmdContext.Version = processEntity.Version;
        _processModel = _sail.ProcessModelFactory.CreateByProcess(processEntity);
        return this;
    }

    public AutoRunBuilder AddVariable(string name, string value)
    {
        _auto.Variables ??= new Dictionary<string, string>();
        _auto.Variables.Add(name, value);
        return this;
    }

    public AutoRunBuilder SetNotifyClient(string sessionId, Action<string, object> callback)
    {
        _notifySessionId = sessionId;
        _notifyCallback = callback;
        return this;
    }

    public AutoRunBuilder WithDelegateRegistry(IServiceTaskDelegateRegistry registry)
    {
        _auto.DelegateRegistry = registry;
        return this;
    }

    public FlowCommandContext BuildRunner() => _cmdContext;

    public Task<WfExecutedResult> Run()
    {
        if (_processModel == null)
            throw new InvalidOperationException("Call UseProcess(...) before Run().");
        return new WorkflowExecutor(_sail).Run(
            _cmdContext,
            _processModel,
            _auto,
            _notifySessionId,
            _notifyCallback);
    }
}
