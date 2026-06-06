namespace VendorRisk.Domain.Entities;

/// <summary>
/// A vendor under risk assessment. Holds the raw inputs the rule engine scores:
/// financial health, operational signals (SLA, incidents) and security/compliance posture.
/// </summary>
public sealed class VendorProfile
{
    /// <summary>Database identity (auto-generated).</summary>
    public int Id { get; set; }

    /// <summary>Vendor display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Financial health score on a 0–100 scale (higher is healthier).</summary>
    public int FinancialHealth { get; set; }

    /// <summary>SLA uptime as a percentage (0–100).</summary>
    public int SlaUptime { get; set; }

    /// <summary>Number of major incidents in the last 12 months.</summary>
    public int MajorIncidents { get; set; }

    /// <summary>Held security certifications (e.g. ISO27001, SOC2, PCI-DSS).</summary>
    public List<string> SecurityCerts { get; set; } = [];

    /// <summary>Validity of the vendor's compliance documents.</summary>
    public VendorDocuments Documents { get; set; } = new();
}
