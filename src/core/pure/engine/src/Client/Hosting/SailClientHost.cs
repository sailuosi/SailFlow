using System;

namespace saiflow.engine.Client.Hosting;

/// <summary>
/// 懒构建入口：首次 GetOrCreate 时执行 builder 工厂。
/// 仅缓存实例，不写进程静态 Access。
/// </summary>
public static class SailClientHost
{
    private static readonly object Gate = new();
    private static Func<SailClientBuilder>? _builderFactory;
    private static SailClient? _client;

    /// <summary>注册构建配方（不立即 Build）。</summary>
    public static void Configure(Func<SailClientBuilder> builderFactory)
    {
        ArgumentNullException.ThrowIfNull(builderFactory);
        lock (Gate)
        {
            if (_client != null)
                throw new InvalidOperationException("SailClient already built.");
            _builderFactory = builderFactory;
        }
    }

    /// <summary>积极或懒：若未构建则 Build。</summary>
    public static SailClient GetOrCreate()
    {
        if (_client != null)
            return _client;

        lock (Gate)
        {
            if (_client != null)
                return _client;

            if (_builderFactory == null)
            {
                throw new InvalidOperationException(
                    "Call SailClientHost.Configure(() => { var b = SailClient.CreateBuilder(); ...; return b; }) first.");
            }

            _client = _builderFactory().Build();
            return _client;
        }
    }

    /// <summary>测试用：清空懒构建状态。</summary>
    public static void Reset()
    {
        lock (Gate)
        {
            _client = null;
            _builderFactory = null;
        }
    }
}
