using VendorRisk.Domain.Enums;

namespace VendorRisk.Domain.Risk;

/// <summary>
/// The aggregated risk for one dimension (noisy-OR over its factors) plus the factors
/// that produced it, so the breakdown stays fully explainable.
/// </summary>
/// <param name="Dimension">The dimension this score belongs to.</param>
/// <param name="Score">Aggregated dimension risk in [0,1].</param>
/// <param name="Factors">The factors that contributed to this dimension.</param>
public sealed record DimensionScore(
    RiskDimension Dimension,
    double Score,
    IReadOnlyList<RiskFactor> Factors);
