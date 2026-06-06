namespace VendorRisk.Domain.Entities;

/// <summary>
/// Validity flags for a vendor's compliance documents. Each flag is tri-state:
/// <c>true</c> = valid, <c>false</c> = expired/failed (a risk), <c>null</c> = not assessed
/// (treated as unknown, so it does not trigger a risk factor on its own).
/// </summary>
public sealed class VendorDocuments
{
    /// <summary>Whether the master contract is currently valid.</summary>
    public bool? ContractValid { get; set; }

    /// <summary>Whether the privacy policy is currently valid.</summary>
    public bool? PrivacyPolicyValid { get; set; }

    /// <summary>Whether the latest penetration-test report is valid (passed).</summary>
    public bool? PentestReportValid { get; set; }
}
