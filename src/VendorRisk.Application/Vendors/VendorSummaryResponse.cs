using VendorRisk.Domain.Entities;
using VendorRisk.Domain.Risk;

namespace VendorRisk.Application.Vendors;

/// <summary>
/// Compact vendor row for the list view: the key inputs plus the computed risk
/// score and level (no full factor breakdown).
/// </summary>
public sealed record VendorSummaryResponse(
    int Id,
    string Name,
    int FinancialHealth,
    int SlaUptime,
    int MajorIncidents,
    IReadOnlyList<string> SecurityCerts,
    double RiskScore,
    string RiskLevel,
    string Reason)
{
    public static VendorSummaryResponse From(VendorProfile vendor, RiskAssessment assessment) =>
        new(
            vendor.Id,
            vendor.Name,
            vendor.FinancialHealth,
            vendor.SlaUptime,
            vendor.MajorIncidents,
            vendor.SecurityCerts,
            assessment.RiskScore,
            assessment.RiskLevel.ToString(),
            assessment.Reason);
}
