using System;

namespace sailflow.engine.Runtime
{
    /// <summary><see cref="InstanceExecutionLock"/> 空闲销户定时选项。</summary>
    public sealed class InstanceLockCleanerOptions
    {
        /// <summary>扫描间隔（默认 5 分钟）。</summary>
        public TimeSpan Period { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>空闲超过此时长且无人占用则从字典移除（默认 15 分钟）。</summary>
        public TimeSpan IdleTtl { get; set; } = TimeSpan.FromMinutes(15);
    }
}
