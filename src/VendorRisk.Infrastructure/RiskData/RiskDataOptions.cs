namespace VendorRisk.Infrastructure.RiskData;

/// <summary>
/// File locations for the seeded datasets. Defaults point at the <c>Data</c> folder copied
/// next to the running app; both can be overridden via the "RiskData" configuration section.
/// </summary>
public sealed class RiskDataOptions
{
    public const string SectionName = "RiskData";

    /// <summary>Path to the Risk Similarity Matrix (RiskFactorMatrix.json).</summary>
    public string MatrixPath { get; set; } =
        Path.Combine(AppContext.BaseDirectory, "Data", "RiskFactorMatrix.json");

    /// <summary>Path to the seed vendor dataset (SampleVendorData.json).</summary>
    public string SeedPath { get; set; } =
        Path.Combine(AppContext.BaseDirectory, "Data", "SampleVendorData.json");
}
