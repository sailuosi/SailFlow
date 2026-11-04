using System;
using mooSQL.data;

namespace saiflow.engine.Client;

/// <summary>
/// 抽象数据库工厂。主 API 返回 <see cref="DBInstance"/>；快捷方法返回 <see cref="SQLBuilder"/>。
/// 具体实现由应用宿主注入（如 <see cref="DelegateSailDBFactory"/>）。
/// </summary>
public abstract class SailDBFactory
{
    /// <summary>主连接位。</summary>
    public abstract int getMainPosition();

    /// <summary>按连接位获取数据库实例。</summary>
    public abstract DBInstance GetDB(int position);

    /// <summary>获取主库实例。</summary>
    public virtual DBInstance GetDB() => GetDB(getMainPosition());

    /// <summary>定义侧 SQLBuilder（初期可与运行库同库）。</summary>
    public virtual SQLBuilder useEnvSQL() => GetDB(getMainPosition()).useSQL();

    /// <summary>运行侧 SQLBuilder。</summary>
    public virtual SQLBuilder useRunSQL() => GetDB(getMainPosition()).useSQL();
}

/// <summary>
/// 基于委托的通用 Factory（非宿主专用）。宿主传入 <c>position =&gt; DBInstance</c> 即可。
/// </summary>
public sealed class DelegateSailDBFactory : SailDBFactory
{
    private readonly Func<int, DBInstance> _getDb;
    private readonly int _mainPosition;

    public DelegateSailDBFactory(Func<int, DBInstance> getDb, int mainPosition = 0)
    {
        _getDb = getDb ?? throw new ArgumentNullException(nameof(getDb));
        _mainPosition = mainPosition;
    }

    public DelegateSailDBFactory(DBInstance db, int mainPosition = 0)
        : this(_ => db ?? throw new ArgumentNullException(nameof(db)), mainPosition)
    {
    }

    public override int getMainPosition() => _mainPosition;

    public override DBInstance GetDB(int position) => _getDb(position)
        ?? throw new InvalidOperationException($"DBInstance for position {position} is null.");
}
