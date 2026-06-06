using VendorRisk.Domain.Risk;

namespace VendorRisk.Application.Abstractions;

/// <summary>
/// Supplies the Risk Similarity Matrix to the scoring engine. The matrix lives in the database
/// (source of truth); this provider caches it in memory for fast scoring and can be reloaded
/// after the data changes.
/// </summary>
public interface IRiskMatrixProvider
{
    /// <summary>Returns the cached similarity matrix (loads on first use if needed).</summary>
    RiskMatrix GetMatrix();

    /// <summary>Reloads the matrix from the database into the cache.</summary>
    Task ReloadAsync(CancellationToken ct = default);
}
