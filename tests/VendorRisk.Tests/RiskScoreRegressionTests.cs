using VendorRisk.Domain.Entities;
using VendorRisk.Domain.Risk;

namespace VendorRisk.Tests;

/// <summary>
/// Regression + determinism guarantees for the scoring engine — the most important property of
/// this project: the same vendor must always produce the exact same risk result.
///
/// * <see cref="Score_MatchesGoldenValue"/> pins the exact score and level for every seed vendor,
///   so any accidental change to a weight, band, or constant is caught immediately.
/// * <see cref="Scoring_IsFullyDeterministic"/> proves repeated evaluations are byte-for-byte equal
///   across the whole assessment (score, level, reason, dimensions and factor severities).
/// </summary>
public sealed class RiskScoreRegressionTests
{
    private static readonly RiskMatrix Matrix = TestData.Matrix();

    // Golden values captured from the calibrated engine (matches the running API).
    [Theory]
    [InlineData("TechPlus Solutions", 0.51, "Medium")]
    [InlineData("NovaLog Logistics", 0.79, "High")]
    [InlineData("SecurePay Financial", 0.04, "Low")]
    [InlineData("AlphaCloud Hosting", 0.77, "High")]
    [InlineData("TrustCom IT Services", 0.95, "Critical")]
    [InlineData("GlobalTrans Freight", 0.56, "Medium")]
    [InlineData("Skyline Software", 0.04, "Low")]
    [InlineData("DataBridge Analytics", 0.94, "Critical")]
    [InlineData("CargoLine Transport", 0.73, "High")]
    [InlineData("HexaCloud DevOps", 0.12, "Low")]
    [InlineData("VisionTech Support", 0.78, "High")]
    [InlineData("PrimeNet Security", 0.49, "Medium")]
    [InlineData("Velocity Warehousing", 0.76, "High")]
    [InlineData("Orion Network Ops", 0.04, "Low")]
    [InlineData("BlueWave Consulting", 0.68, "High")]
    public void Score_MatchesGoldenValue(string vendorName, double expectedScore, string expectedLevel)
    {
        RiskAssessment result = RiskScoreCalculator.Calculate(TestData.Sample(vendorName), Matrix);

        Assert.Equal(expectedScore, result.RiskScore, precision: 2);
        Assert.Equal(expectedLevel, result.RiskLevel.ToString());
    }

    [Theory]
    [MemberData(nameof(TestData.AllSampleNames), MemberType = typeof(TestData))]
    public void Scoring_IsFullyDeterministic(string vendorName)
    {
        VendorProfile vendor = TestData.Sample(vendorName);
        RiskAssessment baseline = RiskScoreCalculator.Calculate(vendor, Matrix);

        for (int run = 0; run < 5; run++)
        {
            RiskAssessment again = RiskScoreCalculator.Calculate(vendor, Matrix);

            Assert.Equal(baseline.RiskScore, again.RiskScore);
            Assert.Equal(baseline.RiskLevel, again.RiskLevel);
            Assert.Equal(baseline.Reason, again.Reason);

            // Full breakdown must be identical too — dimensions and every factor's severity.
            Assert.Equal(
                baseline.Dimensions.Select(d => (d.Dimension, d.Score)),
                again.Dimensions.Select(d => (d.Dimension, d.Score)));
            Assert.Equal(
                baseline.Factors.Select(f => (f.Code, f.Severity)),
                again.Factors.Select(f => (f.Code, f.Severity)));
        }
    }

    [Theory]
    [MemberData(nameof(TestData.AllSampleNames), MemberType = typeof(TestData))]
    public void Score_StaysWithinUnitInterval(string vendorName)
    {
        RiskAssessment result = RiskScoreCalculator.Calculate(TestData.Sample(vendorName), Matrix);

        Assert.InRange(result.RiskScore, 0.0, 1.0);
    }
}
