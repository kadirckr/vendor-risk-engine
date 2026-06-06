namespace VendorRisk.Domain.Risk;

/// <summary>
/// The Risk Similarity Matrix as a directed weighted graph (each node → its outgoing correlated
/// risks and weights). Built from the database and used to collect, for a triggered factor, every
/// correlated risk reachable through the graph.
/// </summary>
public sealed class RiskMatrix
{
    private readonly IReadOnlyDictionary<string, IReadOnlyList<CorrelatedRisk>> _adjacency;

    public RiskMatrix(IReadOnlyDictionary<string, IReadOnlyList<CorrelatedRisk>> adjacency)
        => _adjacency = adjacency;

    /// <summary>An empty matrix — propagation degrades gracefully to base severities.</summary>
    public static RiskMatrix Empty { get; } =
        new(new Dictionary<string, IReadOnlyList<CorrelatedRisk>>(StringComparer.OrdinalIgnoreCase));

    /// <summary>Direct outgoing edges of a node (one hop), or empty when unknown.</summary>
    private IReadOnlyList<CorrelatedRisk> DirectNeighbors(string code) =>
        _adjacency.TryGetValue(code, out IReadOnlyList<CorrelatedRisk>? edges) ? edges : [];

    /// <summary>
    /// Walks the graph from <paramref name="triggerCode"/> and collects every reachable edge.
    /// Each node is expanded once (so cycles terminate), but every edge is counted, so a child
    /// reached from two parents contributes twice. The result is a multiset of correlated risks.
    /// </summary>
    public IReadOnlyList<CorrelatedRisk> CollectEffects(string triggerCode)
    {
        if (!_adjacency.ContainsKey(triggerCode))
        {
            return [];
        }

        List<CorrelatedRisk> collected = [];
        HashSet<string> expanded = new(StringComparer.OrdinalIgnoreCase);
        Queue<string> queue = new();
        queue.Enqueue(triggerCode);

        while (queue.Count > 0)
        {
            string node = queue.Dequeue();
            if (!expanded.Add(node))
            {
                continue; // already expanded → its edges are already collected
            }

            foreach (CorrelatedRisk edge in DirectNeighbors(node))
            {
                collected.Add(edge); // count every edge, even to an already-seen child
                if (!expanded.Contains(edge.Name))
                {
                    queue.Enqueue(edge.Name);
                }
            }
        }

        return collected;
    }
}
