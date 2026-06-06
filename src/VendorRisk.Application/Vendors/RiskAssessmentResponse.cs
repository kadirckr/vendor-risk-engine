using VendorRisk.Domain.Entities;
using VendorRisk.Domain.Risk;

namespace VendorRisk.Application.Vendors;

/// <summary>API-facing risk assessment, with enums rendered as strings and a full breakdown.</summary>
public sealed record RiskAssessmentResponse(
    int VendorId,
    string VendorName,
    double RiskScore,
    string RiskLevel,
    string Reason,
    IReadOnlyList<DimensionScoreResponse> Dimensions)
{
    /// <summary>Maps a domain <see cref="RiskAssessment"/> (plus its vendor) to the API shape.</summary>
    public static RiskAssessmentResponse From(VendorProfile vendor, RiskAssessment assessment) =>
        new(
            assessment.VendorId,
            vendor.Name,
            assessment.RiskScore,
            assessment.RiskLevel.ToString(),
            assessment.Reason,
            [.. assessment.Dimensions.Select(DimensionScoreResponse.From)]);
}

/// <summary>One dimension's score and the factors behind it.</summary>
public sealed record DimensionScoreResponse(
    string Dimension,
    double Score,
    IReadOnlyList<RiskFactorResponse> Factors)
{
    public static DimensionScoreResponse From(DimensionScore dimension) =>
        new(
            dimension.Dimension.ToString(),
            Math.Round(dimension.Score, 2),
            [.. dimension.Factors.Select(RiskFactorResponse.From)]);
}

/// <summary>A single triggered risk factor with its correlated risks.</summary>
public sealed record RiskFactorResponse(
    string Code,
    string Label,
    string Description,
    double BaseSeverity,
    double Severity,
    IReadOnlyList<CorrelatedRiskResponse> CorrelatedRisks)
{
    public static RiskFactorResponse From(RiskFactor factor) =>
        new(
            factor.Code,
            factor.Label,
            factor.Description,
            factor.BaseSeverity,
            factor.Severity,
            [.. factor.CorrelatedRisks.Select(c => new CorrelatedRiskResponse(c.Name, c.Weight))]);
}

/// <summary>A correlated risk pulled from the similarity matrix.</summary>
public sealed record CorrelatedRiskResponse(string Name, double Weight);
