using ErrorOr;
using VendorRisk.Domain.Entities;

namespace VendorRisk.Application.Abstractions;

/// <summary>
/// Persistence for the Risk Similarity Matrix, stored as three tables
/// (categories → factor nodes → weighted edges). Implemented in Infrastructure.
/// </summary>
public interface IRiskMatrixRepository
{
    /// <summary>True when the matrix has already been seeded (used to guard seeding).</summary>
    Task<bool> AnyAsync(CancellationToken ct = default);

    /// <summary>Returns every edge with its parent/child nodes loaded (for building the graph).</summary>
    Task<IReadOnlyList<RiskFactorEdge>> GetEdgesAsync(CancellationToken ct = default);

    /// <summary>
    /// Persists a freshly built matrix graph (categories, nodes and edges, wired via navigation
    /// properties). EF inserts them in dependency order.
    /// </summary>
    Task<ErrorOr<Success>> SeedAsync(
        IEnumerable<RiskCategory> categories,
        IEnumerable<RiskFactorNode> nodes,
        IEnumerable<RiskFactorEdge> edges,
        CancellationToken ct = default);
}
