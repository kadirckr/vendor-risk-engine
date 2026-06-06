using System.Text.Json;
using VendorRisk.Domain.Entities;

namespace VendorRisk.Infrastructure.RiskData;

/// <summary>The matrix graph parsed from JSON, ready to be persisted into the three tables.</summary>
public sealed record RiskMatrixGraph(
    IReadOnlyList<RiskCategory> Categories,
    IReadOnlyList<RiskFactorNode> Nodes,
    IReadOnlyList<RiskFactorEdge> Edges);

/// <summary>
/// Reads RiskFactorMatrix.json (category → parent → {child: weight}) into the normalized graph:
/// a <see cref="RiskCategory"/> per group, a <see cref="RiskFactorNode"/> per distinct risk name,
/// and a <see cref="RiskFactorEdge"/> per parent → child weight. Navigation properties are wired
/// so EF can insert the whole graph and assign the foreign keys.
/// </summary>
public static class RiskMatrixSeedLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static RiskMatrixGraph Load(string path)
    {
        if (!File.Exists(path))
        {
            return new RiskMatrixGraph([], [], []);
        }

        using FileStream stream = File.OpenRead(path);

        // category -> parentCode -> childCode -> weight
        Dictionary<string, Dictionary<string, Dictionary<string, double>>>? raw =
            JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, Dictionary<string, double>>>>(
                stream, SerializerOptions);

        if (raw is null)
        {
            return new RiskMatrixGraph([], [], []);
        }

        Dictionary<string, RiskCategory> categories = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, RiskFactorNode> nodes = new(StringComparer.OrdinalIgnoreCase);
        List<RiskFactorEdge> edges = [];

        foreach ((string categoryCode, Dictionary<string, Dictionary<string, double>> parents) in raw)
        {
            RiskCategory category = GetOrAddCategory(categories, categoryCode);

            foreach ((string parentCode, Dictionary<string, double> children) in parents)
            {
                RiskFactorNode parent = GetOrAddNode(nodes, parentCode, category);

                foreach ((string childCode, double weight) in children)
                {
                    RiskFactorNode child = GetOrAddNode(nodes, childCode, category);
                    edges.Add(new RiskFactorEdge { Parent = parent, Child = child, Weight = weight });
                }
            }
        }

        return new RiskMatrixGraph([.. categories.Values], [.. nodes.Values], edges);
    }

    private static RiskCategory GetOrAddCategory(Dictionary<string, RiskCategory> categories, string code)
    {
        if (!categories.TryGetValue(code, out RiskCategory? category))
        {
            category = new RiskCategory { Code = code };
            categories[code] = category;
        }

        return category;
    }

    // First-seen category wins (the matrix groups are disjoint, so this is unambiguous here).
    private static RiskFactorNode GetOrAddNode(
        Dictionary<string, RiskFactorNode> nodes, string code, RiskCategory category)
    {
        if (!nodes.TryGetValue(code, out RiskFactorNode? node))
        {
            node = new RiskFactorNode { Code = code, Category = category };
            nodes[code] = node;
        }

        return node;
    }
}
