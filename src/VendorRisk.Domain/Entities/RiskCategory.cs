namespace VendorRisk.Domain.Entities;

/// <summary>
/// Top-level grouping of the Risk Similarity Matrix
/// (financialRisk / operationalRisk / securityRisk / complianceRisk). Table 1 of 3.
/// </summary>
public sealed class RiskCategory
{
    /// <summary>Database identity (auto-generated).</summary>
    public int Id { get; set; }

    /// <summary>Stable code, e.g. "securityRisk".</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Optional display name, e.g. "Security Risk".</summary>
    public string? Name { get; set; }
}
