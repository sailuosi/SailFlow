namespace saiflow.engine.Factories;

/// <summary>
/// 寿命用例标签（见 doc/sail/dev/专题-对象生命周期与复用边界.md）。
/// D2：契约与注释用；不改变运行时行为。
/// </summary>
public static class LifecycleTags
{
    /// <summary>A · 应用级富功能（每 SailClient 一套，默认可并发共享）</summary>
    public const string A = "A";

    /// <summary>B · 请求级上下文 / 链式建造（一般不能并发）</summary>
    public const string B = "B";

    /// <summary>C · 纯函数静态工具</summary>
    public const string C = "C";

    /// <summary>H · Host 壳 / 组合根</summary>
    public const string H = "H";
}
