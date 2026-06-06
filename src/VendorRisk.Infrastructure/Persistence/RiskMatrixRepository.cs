using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VendorRisk.Application.Abstractions;
using VendorRisk.Domain.Entities;

namespace VendorRisk.Infrastructure.Persistence;

/// <summary>PostgreSQL/EF Core implementation of <see cref="IRiskMatrixRepository"/>.</summary>
internal sealed class RiskMatrixRepository(AppDbContext db, ILogger<RiskMatrixRepository> logger) : IRiskMatrixRepository
{
    /// <inheritdoc />
    public Task<bool> AnyAsync(CancellationToken ct = default) =>
        db.RiskFactorEdges.AnyAsync(ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<RiskFactorEdge>> GetEdgesAsync(CancellationToken ct = default) =>
        await db.RiskFactorEdges
            .AsNoTracking()
            .Include(e => e.Parent)
            .Include(e => e.Child)
            .ToListAsync(ct);

    /// <inheritdoc />
    public async Task<ErrorOr<Success>> SeedAsync(
        IEnumerable<RiskCategory> categories,
        IEnumerable<RiskFactorNode> nodes,
        IEnumerable<RiskFactorEdge> edges,
        CancellationToken ct = default)
    {
        try
        {
            // Navigation properties are wired by the loader; EF inserts in dependency order
            // (categories → nodes → edges) and fills the foreign keys.
            db.RiskCategories.AddRange(categories);
            db.RiskFactorNodes.AddRange(nodes);
            db.RiskFactorEdges.AddRange(edges);
            await db.SaveChangesAsync(ct);

            return Result.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to seed the risk matrix.");
            return Error.Failure("RiskMatrix.SeedFailed", $"Failed to seed the risk matrix: {ex.Message}");
        }
    }
}
