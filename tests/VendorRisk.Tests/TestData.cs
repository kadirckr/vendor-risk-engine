using VendorRisk.Domain.Entities;
using VendorRisk.Domain.Risk;

namespace VendorRisk.Tests;

/// <summary>
/// Shared builders for tests: the full Risk Similarity Matrix (identical to
/// RiskFactorMatrix.json, so test scores match production) and the 15 seed vendors
/// (identical to SampleVendorData.json).
/// </summary>
internal static class TestData
{
    /// <summary>The complete similarity graph — every parent → child edge from RiskFactorMatrix.json.</summary>
    public static RiskMatrix Matrix()
    {
        Dictionary<string, IReadOnlyList<CorrelatedRisk>> factors = new(StringComparer.OrdinalIgnoreCase)
        {
            // financialRisk
            ["lowCashFlow"] = [new("highDebtRatio", 0.88), new("latePayments", 0.82), new("weakRevenueStability", 0.79)],
            ["highDebtRatio"] = [new("lowCashFlow", 0.88), new("creditDowngrade", 0.84), new("liquidityIssues", 0.80)],
            ["creditDowngrade"] = [new("liquidityIssues", 0.86), new("latePayments", 0.81), new("weakRevenueStability", 0.77)],

            // operationalRisk
            ["slaDrop"] = [new("downtime", 0.87), new("slowTicketResolution", 0.83), new("serviceInstability", 0.79)],
            ["downtime"] = [new("slaDrop", 0.87), new("infrastructureFailure", 0.82), new("lateDeliveries", 0.76)],
            ["majorIncident"] = [new("recurringIncidents", 0.88), new("serviceOutage", 0.85), new("securityIncident", 0.80)],
            ["slowTicketResolution"] = [new("slaDrop", 0.83), new("lateIssueEscalation", 0.80), new("supportCapacityIssues", 0.78)],

            // securityRisk
            ["missingISO27001"] = [new("weakAccessControl", 0.84), new("noEncryptionPolicy", 0.79), new("failedAudit", 0.76)],
            ["failedPenTest"] = [new("internalVulnerabilities", 0.88), new("weakFirewallRules", 0.83), new("missingPatching", 0.79)],
            ["weakAccessControl"] = [new("missingISO27001", 0.84), new("weakPasswordPolicy", 0.81), new("privilegeMisuseRisk", 0.75)],
            ["noEncryptionPolicy"] = [new("dataLeakRisk", 0.86), new("weakAccessControl", 0.79), new("missingISO27001", 0.77)],

            // complianceRisk
            ["expiredPrivacyPolicy"] = [new("missingNDA", 0.81), new("expiredContract", 0.78), new("gdprConflict", 0.75)],
            ["missingNDA"] = [new("expiredPrivacyPolicy", 0.81), new("missingContract", 0.79), new("weakLegalCoverage", 0.76)],
            ["gdprConflict"] = [new("expiredPrivacyPolicy", 0.75), new("dataRetentionIssues", 0.82), new("insufficientComplianceDocs", 0.80)],
            ["expiredContract"] = [new("missingNDA", 0.78), new("expiredPrivacyPolicy", 0.78), new("contractDisputeRisk", 0.74)],
        };

        return new RiskMatrix(factors);
    }

    public static VendorProfile Vendor(
        int financialHealth,
        int slaUptime,
        int majorIncidents,
        string[] certs,
        bool? contractValid,
        bool? privacyPolicyValid,
        bool? pentestReportValid,
        int id = 1,
        string name = "Test Vendor") =>
        new()
        {
            Id = id,
            Name = name,
            FinancialHealth = financialHealth,
            SlaUptime = slaUptime,
            MajorIncidents = majorIncidents,
            SecurityCerts = [.. certs],
            Documents = new VendorDocuments
            {
                ContractValid = contractValid,
                PrivacyPolicyValid = privacyPolicyValid,
                PentestReportValid = pentestReportValid,
            },
        };

    // ----- The 15 seed vendors (SampleVendorData.json) ---------------------------------

    public static VendorProfile TechPlus() => Vendor(78, 93, 1, ["ISO27001"], true, false, true, 1, "TechPlus Solutions");
    public static VendorProfile NovaLog() => Vendor(55, 88, 3, [], true, true, false, 2, "NovaLog Logistics");
    public static VendorProfile SecurePay() => Vendor(90, 97, 0, ["ISO27001", "PCI-DSS"], true, true, true, 3, "SecurePay Financial");
    public static VendorProfile AlphaCloud() => Vendor(62, 89, 2, ["SOC2"], true, false, false, 4, "AlphaCloud Hosting");
    public static VendorProfile TrustCom() => Vendor(48, 92, 4, [], false, false, false, 5, "TrustCom IT Services");
    public static VendorProfile GlobalTrans() => Vendor(70, 95, 1, [], true, true, false, 6, "GlobalTrans Freight");
    public static VendorProfile Skyline() => Vendor(88, 99, 0, ["ISO27001"], true, true, true, 7, "Skyline Software");
    public static VendorProfile DataBridge() => Vendor(43, 86, 2, [], false, false, false, 8, "DataBridge Analytics");
    public static VendorProfile CargoLine() => Vendor(59, 92, 1, [], true, true, false, 9, "CargoLine Transport");
    public static VendorProfile HexaCloud() => Vendor(75, 96, 0, ["ISO27001", "SOC2"], true, true, true, 10, "HexaCloud DevOps");
    public static VendorProfile VisionTech() => Vendor(52, 90, 3, [], true, false, false, 11, "VisionTech Support");
    public static VendorProfile PrimeNet() => Vendor(83, 94, 0, ["ISO27001"], true, true, false, 12, "PrimeNet Security");
    public static VendorProfile Velocity() => Vendor(61, 91, 2, [], true, false, false, 13, "Velocity Warehousing");
    public static VendorProfile Orion() => Vendor(95, 99, 0, ["ISO27001", "SOC2"], true, true, true, 14, "Orion Network Ops");
    public static VendorProfile BlueWave() => Vendor(67, 87, 1, [], true, true, false, 15, "BlueWave Consulting");

    /// <summary>All 15 seed vendors, keyed by name (for data-driven tests).</summary>
    public static IReadOnlyDictionary<string, VendorProfile> AllSamples { get; } =
        new[]
        {
            TechPlus(), NovaLog(), SecurePay(), AlphaCloud(), TrustCom(),
            GlobalTrans(), Skyline(), DataBridge(), CargoLine(), HexaCloud(),
            VisionTech(), PrimeNet(), Velocity(), Orion(), BlueWave(),
        }.ToDictionary(v => v.Name);

    public static VendorProfile Sample(string name) => AllSamples[name];

    /// <summary>Vendor names as xUnit member data (each row a single name).</summary>
    public static IEnumerable<object[]> AllSampleNames() =>
        AllSamples.Keys.Select(name => new object[] { name });
}
