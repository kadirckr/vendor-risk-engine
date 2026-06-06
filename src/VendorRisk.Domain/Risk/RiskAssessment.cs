using VendorRisk.Domain.Enums;

namespace VendorRisk.Domain.Risk;

/// <summary>
/// The full, explainable outcome of scoring a vendor: the final score, its qualitative
/// level, a human-readable reason, and the per-dimension / per-factor breakdown behind it.
/// </summary>
/// <param name="VendorId">The scored vendor's id.</param>
/// <param name="RiskScore">Final weighted risk score in [0,1] (rounded to 2 decimals).</param>
/// <param name="RiskLevel">Qualitative band derived from the score.</param>
/// <param name="Reason">Human-readable rationale.</param>
/// <param name="Dimensions">Per-dimension scores and their factors.</param>
/// <param name="Factors">All triggered factors, flattened (highest severity first).</param>
public sealed record RiskAssessment(
    int VendorId,
    double RiskScore,
    RiskLevel RiskLevel,
    string Reason,
    IReadOnlyList<DimensionScore> Dimensions,
    IReadOnlyList<RiskFactor> Factors);
