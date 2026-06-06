using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VendorRisk.Application.Abstractions;
using VendorRisk.Domain.Entities;
using VendorRisk.Domain.Risk;

namespace VendorRisk.Infrastructure.RiskData;

/// <summary>
/// Builds the <see cref="RiskMatrix"/> from the database correlation rows and caches it in memory
/// (singleton). The scoring engine reads the cache synchronously; <see cref="ReloadAsync"/>
/// refreshes it after the data changes.
/// </summary>
internal sealed class DbRiskMatrixProvider(
    IServiceScopeFactory scopeFactory,
    ILogger<DbRiskMatrixProvider> logger) : IRiskMatrixProvider
{
    private volatile RiskMatrix? _cache;

    /// <inheritdoc />
    public RiskMatrix GetMatrix()
    {
        // Lazily warm the cache if it wasn't pre-loaded at startup.
        if (_cache is null)
        {
            ReloadAsync().GetAwaiter().GetResult();
        }

        return _cache ?? RiskMatrix.Empty;
    }

    /// <inheritdoc />
    public async Task ReloadAsync(CancellationToken ct = default)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        IRiskMatrixRepository repository = scope.ServiceProvider.GetRequiredService<IRiskMatrixRepository>();

        IReadOnlyList<RiskFactorEdge> edges = await repository.GetEdgesAsync(ct);

        // Build the adjacency list: parent code → its outgoing (child, weight) edges.
        Dictionary<string, IReadOnlyList<CorrelatedRisk>> adjacency = edges
            .GroupBy(e => e.Parent!.Code, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<CorrelatedRisk>)
                    [.. group.Select(e => new CorrelatedRisk(e.Child!.Code, e.Weight))],
                StringComparer.OrdinalIgnoreCase);

        _cache = new RiskMatrix(adjacency);
        logger.LogInformation(
            "Loaded risk matrix from database: {Nodes} parent nodes over {Edges} edges.",
            adjacency.Count, edges.Count);
    }
}
