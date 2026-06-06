using VendorRisk.Domain.Entities;
using VendorRisk.Domain.Risk;

namespace VendorRisk.Application.Abstractions;

/// <summary>
/// The rule-based risk scoring engine. Produces a transparent, explainable
/// <see cref="RiskAssessment"/> for a vendor.
/// </summary>
public interface IRuleEngineService
{
    /// <summary>Scores a vendor and returns the full explainable assessment.</summary>
    Task<RiskAssessment> EvaluateAsync(VendorProfile vendor, CancellationToken ct = default);
}
