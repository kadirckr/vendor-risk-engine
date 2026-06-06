using VendorRisk.Domain.Risk;

namespace VendorRisk.Tests;

/// <summary>Tests for the graph traversal that collects correlated risks (edge-based, all-values).</summary>
public sealed class RiskMatrixTests
{
    private static RiskMatrix Build(params (string Parent, string Child, double Weight)[] edges)
    {
        Dictionary<string, IReadOnlyList<CorrelatedRisk>> adjacency = edges
            .GroupBy(e => e.Parent, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<CorrelatedRisk>)[.. g.Select(e => new CorrelatedRisk(e.Child, e.Weight))],
                StringComparer.OrdinalIgnoreCase);

        return new RiskMatrix(adjacency);
    }

    [Fact]
    public void CollectEffects_CountsEveryEdge_IncludingAChildReachedTwice()
    {
        // Diamond: a→b, a→c, b→d, c→d. "d" is reached via two different edges.
        RiskMatrix matrix = Build(
            ("a", "b", 0.90),
            ("a", "c", 0.50),
            ("b", "d", 0.80),
            ("c", "d", 0.40));

        IReadOnlyList<CorrelatedRisk> effects = matrix.CollectEffects("a");

        Assert.Equal(4, effects.Count); // all four edges counted
        Assert.Equal(2, effects.Count(e => e.Name == "d")); // d twice
        Assert.Contains(effects, e => e is { Name: "d", Weight: 0.80 });
        Assert.Contains(effects, e => e is { Name: "d", Weight: 0.40 });
    }

    [Fact]
    public void CollectEffects_TerminatesOnCycles()
    {
        // a ↔ b cycle.
        RiskMatrix matrix = Build(("a", "b", 0.9), ("b", "a", 0.9));

        IReadOnlyList<CorrelatedRisk> effects = matrix.CollectEffects("a");

        Assert.Equal(2, effects.Count); // a→b and b→a, each once; no infinite loop
    }

    [Fact]
    public void CollectEffects_UnknownTrigger_ReturnsEmpty()
    {
        RiskMatrix matrix = Build(("a", "b", 0.9));

        Assert.Empty(matrix.CollectEffects("zzz"));
    }

    [Fact]
    public void CollectEffects_AverageIsOverAllEdges_NotDistinctNodes()
    {
        // a→b(0.9), a→c(0.5), b→c(0.3): c appears twice (0.5 and 0.3).
        RiskMatrix matrix = Build(("a", "b", 0.9), ("a", "c", 0.5), ("b", "c", 0.3));

        IReadOnlyList<CorrelatedRisk> effects = matrix.CollectEffects("a");

        // 3 edges → average = (0.9 + 0.5 + 0.3) / 3 = 0.5667 (distinct-node avg would differ).
        Assert.Equal(3, effects.Count);
        Assert.Equal(0.5667, effects.Average(e => e.Weight), precision: 3);
    }
}
