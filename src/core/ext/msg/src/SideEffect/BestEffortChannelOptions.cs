using System;

namespace saiflow.msg.SideEffect
{
    /// <summary>
    /// BestEffort 进程内 Channel 参数：有界、默认可丢；单消费者串行（避免文件等资源并发写坏）。
    /// </summary>
    public sealed class BestEffortChannelOptions
    {
        public const int DefaultCapacity = 4096;

        /// <summary>有界容量；满时按 <see cref="DropWhenFull"/> 处理。</summary>
        public int Capacity { get; set; } = DefaultCapacity;

        /// <summary>
        /// true（默认）：队列满时 <c>TryWrite</c> 失败并丢弃本次写入（生产端可狂丢，不阻塞主路径）。
        /// false：满时 <c>TryWrite</c> 仍失败（有界 Wait 模式下写端应改用 WriteAsync，本骨架仍以 TryWrite 为主）。
        /// </summary>
        public bool DropWhenFull { get; set; } = true;
    }
}
