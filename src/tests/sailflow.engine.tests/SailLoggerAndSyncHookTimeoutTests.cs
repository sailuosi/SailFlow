using System;
using System.Collections.Generic;
using System.Threading;
using sailflow.hosting;
using sailflow.events;
using sailflow.hooks;
using sailflow.log;
using Xunit;

namespace sailflow.engine.tests;

public class SailLoggerAndSyncHookTimeoutTests
{
    [Fact]
    public void ConsoleSailLogger_MinimumLevel_Filters_Debug()
    {
        var options = new SailLogOptions { MinimumLevel = SailLogLevel.Message, IncludeTimestamp = false };
        var logger = new CapturingSailLogger(options);

        logger.Debug(SailLogCategories.SyncHook, "BEGIN should hide");
        logger.Warning(SailLogCategories.SyncHook, "SLOW should show");

        Assert.DoesNotContain(logger.Lines, l => l.Contains("BEGIN", StringComparison.Ordinal));
        Assert.Contains(logger.Lines, l => l.Contains("SLOW", StringComparison.Ordinal));
    }

    [Fact]
    public void ResolveSyncHookTimeoutMs_Prefers_Registration_Then_Base_Then_Global()
    {
        var sail = new SailClient
        {
            CapabilityOptions = new FlowCapabilityOptions { SyncHookTimeoutMs = 1111 },
            Log = NullSailLogger.Instance,
        };
        var dispatcher = new FlowCapabilityDispatcher(sail);

        var fromReg = new HookRegistration
        {
            Name = "r",
            Mode = HookMode.Sync,
            Handler = new TinyHook("r") { TimeoutMs = 2222 },
            TimeoutMs = 3333,
        };
        Assert.Equal(3333, dispatcher.ResolveSyncHookTimeoutMs(fromReg));

        var fromBase = new HookRegistration
        {
            Name = "b",
            Mode = HookMode.Sync,
            Handler = new TinyHook("b") { TimeoutMs = 2222 },
        };
        Assert.Equal(2222, dispatcher.ResolveSyncHookTimeoutMs(fromBase));

        var fromGlobal = new HookRegistration
        {
            Name = "g",
            Mode = HookMode.Sync,
            Handler = new PlainHook("g"),
        };
        Assert.Equal(1111, dispatcher.ResolveSyncHookTimeoutMs(fromGlobal));
    }

    [Fact]
    public void RunSyncHooks_SoftTimeout_Throws_After_Handle_Returns()
    {
        var sail = new SailClient
        {
            CapabilityOptions = new FlowCapabilityOptions
            {
                SyncHookTimeoutMs = 30_000,
                SyncHookWarnMs = 0,
            },
            Log = NullSailLogger.Instance,
        };
        sail.Capabilities.RegisterHook(
            new TinyHook("slow") { TimeoutMs = 20 },
            new HookRegistrationOptions
            {
                Match = HookMatch.All,
                Mode = HookMode.Sync,
                Events = new[] { EventFireTypeEnum.OnProcessStarted },
            });

        // 跳过 EnsureProcessCode 对 DB 的依赖
        var ctx = new FlowHookContext { ProcessInstanceId = "pi-1", ProcessCode = "p" };
        var ex = Assert.Throws<FlowSyncHookTimeoutException>(() =>
            sail.CapabilityDispatcher.RunSyncHooks(kit: null, ctx, EventFireTypeEnum.OnProcessStarted));

        Assert.Equal("slow", ex.HookName);
        Assert.Equal(20, ex.TimeoutMs);
        Assert.True(ex.ElapsedMs >= 20);
    }

    [Fact]
    public void RunSyncHooks_Wraps_Handler_Exception()
    {
        var sail = new SailClient { Log = NullSailLogger.Instance };
        sail.Capabilities.RegisterHook(
            new ThrowingHook("boom"),
            new HookRegistrationOptions
            {
                Match = HookMatch.All,
                Mode = HookMode.Sync,
                Events = new[] { EventFireTypeEnum.OnProcessStarted },
                TimeoutMs = 5_000,
            });

        var wrapped = Assert.Throws<FlowSyncHookException>(() =>
            sail.CapabilityDispatcher.RunSyncHooks(
                null,
                new FlowHookContext { ProcessCode = "p" },
                EventFireTypeEnum.OnProcessStarted));

        Assert.Equal("boom", wrapped.HookName);
        Assert.IsType<InvalidOperationException>(wrapped.InnerException);
    }

    private sealed class TinyHook : FlowHookBase
    {
        public TinyHook(string name) => Name = name;
        public override string Name { get; }
        public override void Handle(FlowHookContext ctx) => Thread.Sleep(50);
    }

    private sealed class PlainHook : IFlowHook
    {
        public PlainHook(string name) => Name = name;
        public string Name { get; }
        public void Handle(FlowHookContext ctx) { }
    }

    private sealed class ThrowingHook : FlowHookBase
    {
        public ThrowingHook(string name) => Name = name;
        public override string Name { get; }
        public override void Handle(FlowHookContext ctx) =>
            throw new InvalidOperationException("hook failed");
    }

    private sealed class CapturingSailLogger : ISailLogger
    {
        private readonly SailLogOptions _options;
        public List<string> Lines { get; } = new();

        public CapturingSailLogger(SailLogOptions options) => _options = options;

        public bool IsEnabled(SailLogLevel level) => level >= _options.MinimumLevel;

        public void Trace(string category, string message) => Write(SailLogLevel.Trace, message);
        public void Debug(string category, string message) => Write(SailLogLevel.Debug, message);
        public void Message(string category, string message) => Write(SailLogLevel.Message, message);
        public void Warning(string category, string message) => Write(SailLogLevel.Warning, message);
        public void Error(string category, string message) => Write(SailLogLevel.Error, message);

        public void Error(string category, string message, Exception ex) =>
            Write(SailLogLevel.Error, message);

        public void Debug(string category, string format, params object[] args) =>
            Debug(category, string.Format(format, args));

        public void Message(string category, string format, params object[] args) =>
            Message(category, string.Format(format, args));

        public void Warning(string category, string format, params object[] args) =>
            Warning(category, string.Format(format, args));

        public void Error(string category, Exception ex, string format, params object[] args) =>
            Error(category, string.Format(format, args), ex);

        private void Write(SailLogLevel level, string message)
        {
            if (!IsEnabled(level))
                return;
            Lines.Add(message);
        }
    }
}
