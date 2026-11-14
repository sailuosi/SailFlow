using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using sailflow.engine.Client;
using sailflow.engine.Common;
using sailflow.engine.Entity;
using sailflow.engine.Executor;
using sailflow.engine.Result;
using sailflow.engine.Xpdl;

namespace sailflow.engine.Factories;

/// <summary>
/// B · 自动编排建造会话（每次 <c>CreateAutoRunBuilder</c> new）。建成后交给无状态 <see cref="WorkflowExecutor"/>。
/// </summary>
public sealed class AutoRunBuilder
{
    private readonly SailClient _sail;
    private readonly FlowCommandContext _runner;
    private readonly AutoExecutionContext _context = new AutoExecutionContext();
    private IProcessModel _processModel;
    private string _notifySessionId;
    private Action<string, object> _notifyCallback;

    internal AutoRunBuilder(SailClient sail, FlowCommandContext runner)
    {
        _sail = sail ?? throw new ArgumentNullException(nameof(sail));
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        if (_runner.NextPerformerType == default)
            _runner.NextPerformerType = NextPerformerIntTypeEnum.Unattented;
    }

    public AutoRunBuilder UseBiz(BizDataDto biz)
    {
        _runner.ApplyBizData(biz);
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
            _runner.ProcessId = processId;
            _runner.Version = version;
        }
        else
        {
            var entity = _sail.ManagerFactory.Processes.GetByVersion(processId, version);
            _runner.ProcessId = entity.ProcessId;
            _runner.Version = entity.Version;
        }

        _processModel = _sail.ProcessModelFactory.CreateByProcess(_runner.ProcessId, _runner.Version);
        return this;
    }

    public AutoRunBuilder UseProcess(ProcessEntity processEntity)
    {
        ArgumentNullException.ThrowIfNull(processEntity);
        _runner.ProcessId = processEntity.ProcessId;
        _runner.Version = processEntity.Version;
        _processModel = _sail.ProcessModelFactory.CreateByProcess(processEntity);
        return this;
    }

    public AutoRunBuilder AddVariable(string name, string value)
    {
        _context.Variables ??= new Dictionary<string, string>();
        _context.Variables.Add(name, value);
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
        _context.DelegateRegistry = registry;
        return this;
    }

    public FlowCommandContext BuildRunner() => _runner;

    public Task<WfExecutedResult> Run()
    {
        if (_processModel == null)
            throw new InvalidOperationException("Call UseProcess(...) before Run().");
        return new WorkflowExecutor(_sail).Run(
            _runner,
            _processModel,
            _context,
            _notifySessionId,
            _notifyCallback);
    }
}
