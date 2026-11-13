using System;
using System.Collections.Generic;
using Microsoft.Scripting.Hosting;
using sailflow.engine.NextStep;

namespace sailflow.action.Python;

/// <summary>
/// IronPython 连线/条件求值（<see cref="IConditionEvaluator"/>）。
/// <para>
/// <b>不</b>随 <c>UseIronPython</c> 注册；须显式：
/// <c>UseNextStepRoute(r => r.AddConditionEvaluator&lt;PythonConditionEvaluator&gt;(o => { o.Order = 10; o.Match = NextStepMatch.All; }))</c>
/// 且仍建议挂 LINQ 兜底（管道默认会挂）。
/// </para>
/// <para>
/// 仅认带前缀的表达式（默认 <c>python:</c> / <c>#python</c>），否则返回 <c>null</c> 交下一 Evaluator（通常为 LINQ）。
/// 失败亦返回 <c>null</c>（≠ 条件为 false）。
/// </para>
/// </summary>
public sealed class PythonConditionEvaluator : IConditionEvaluator
{
    public const string PrefixColon = "python:";
    public const string PrefixHash = "#python";

    /// <summary>为 false 时对任意非空表达式尝试 Python（高级；易与 LINQ 争抢）。</summary>
    public bool RequirePythonPrefix { get; set; } = true;

    public bool IsMatch(ConditionEvaluateContext ctx) => true;

    public bool? TryEvaluate(ConditionEvaluateContext ctx)
    {
        if (ctx == null || string.IsNullOrWhiteSpace(ctx.Expression))
            return true;

        if (!TryStripPrefix(ctx.Expression.Trim(), out var script))
            return null;

        if (string.IsNullOrWhiteSpace(script))
            return true;

        try
        {
            var engine = IronPython.Hosting.Python.CreateEngine();
            var scope = engine.CreateScope();
            BindVariables(scope, ctx.Variables);

            var source = engine.CreateScriptSourceFromString(script);
            var raw = source.Execute(scope);
            return CoerceBool(raw);
        }
        catch (Exception)
        {
            // 本器失败 → null，交给 LINQ 等后续；禁止用 false 表示「求值器坏了」
            return null;
        }
    }

    bool TryStripPrefix(string expression, out string script)
    {
        script = expression;
        if (expression.StartsWith(PrefixColon, StringComparison.OrdinalIgnoreCase))
        {
            script = expression.Substring(PrefixColon.Length).TrimStart();
            return true;
        }

        if (expression.StartsWith(PrefixHash, StringComparison.OrdinalIgnoreCase))
        {
            script = expression.Substring(PrefixHash.Length).TrimStart();
            // 允许 "#python\nexpr" 或 "#python expr"
            if (script.StartsWith("\r") || script.StartsWith("\n"))
                script = script.TrimStart('\r', '\n', ' ', '\t');
            return true;
        }

        if (!RequirePythonPrefix)
            return true;

        return false;
    }

    static void BindVariables(ScriptScope scope, IDictionary<string, string> variables)
    {
        if (variables == null || variables.Count == 0)
            return;

        foreach (var kv in variables)
        {
            var name = NormalizeName(kv.Key);
            if (string.IsNullOrEmpty(name))
                continue;
            scope.SetVariable(name, CoerceValue(kv.Value));
        }
    }

    /// <summary>去掉前导 @，便于脚本写 amount 而非 @amount。</summary>
    static string NormalizeName(string key)
    {
        if (string.IsNullOrEmpty(key))
            return key;
        return key[0] == '@' ? key.Substring(1) : key;
    }

    static object CoerceValue(string value)
    {
        if (value == null)
            return null;
        if (bool.TryParse(value, out var b))
            return b;
        if (long.TryParse(value, System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out var l))
            return l;
        if (decimal.TryParse(value, System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out var d))
            return d;
        return value;
    }

    static bool? CoerceBool(object raw)
    {
        if (raw == null)
            return false;
        if (raw is bool b)
            return b;
        if (raw is int i)
            return i != 0;
        if (raw is long l)
            return l != 0;
        if (raw is double db)
            return Math.Abs(db) > double.Epsilon;
        if (raw is float f)
            return Math.Abs(f) > float.Epsilon;
        if (raw is decimal m)
            return m != 0;
        if (raw is string s)
        {
            if (bool.TryParse(s, out var sb))
                return sb;
            if (string.Equals(s, "1", StringComparison.Ordinal))
                return true;
            if (string.Equals(s, "0", StringComparison.Ordinal))
                return false;
            return null;
        }

        // IronPython 可能返回动态类型
        try
        {
            return System.Convert.ToBoolean(raw);
        }
        catch
        {
            return null;
        }
    }
}
