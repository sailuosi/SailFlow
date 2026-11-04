using System.Diagnostics;
using saiflow.engine.Hooks;
using Xunit;
using Xunit.Abstractions;

namespace saiflow.engine.tests;

/// <summary>Soft perf smoke — baseline documented in phase3b progress doc.</summary>
public sealed class FlowCapabilityMatchPerfTests
{
    private readonly ITestOutputHelper _output;

    public FlowCapabilityMatchPerfTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void Whitelist_scan_100_hooks_10k_iterations_under_soft_budget()
    {
        const string targetProcessCode = "PROC_B";
        var hookProcessCodes = Enumerable.Range(0, 100)
            .Select(i => $"PROC_{i}")
            .ToArray();

        const int iterations = 10_000;
        var sw = Stopwatch.StartNew();
        var matches = 0;
        for (var n = 0; n < iterations; n++)
        {
            foreach (var codes in hookProcessCodes)
            {
                if (FlowCapabilityMatch.MatchesProcess(
                        HookMatch.Whitelist,
                        new[] { codes },
                        null,
                        targetProcessCode,
                        null)
                    && FlowCapabilityMatch.MatchesActivity(null, "node-1"))
                    matches++;
            }
        }

        sw.Stop();
        _output.WriteLine($"100 hooks × {iterations} iterations = {iterations * 100} checks in {sw.ElapsedMilliseconds} ms (matches={matches})");

        Assert.Equal(0, matches);
        Assert.True(
            sw.ElapsedMilliseconds < 50,
            $"Expected &lt; 50 ms for {iterations * 100} match checks, got {sw.ElapsedMilliseconds} ms.");
    }
}
