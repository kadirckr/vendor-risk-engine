using VendorRisk.Domain.Entities;
using VendorRisk.Domain.Enums;
using VendorRisk.Domain.Risk;

namespace VendorRisk.Tests;

/// <summary>
/// Behavioural tests for the scoring engine — the structural guarantees (override, propagation,
/// graceful no-matrix, explanation, ordering). Exact scores and determinism live in
/// <see cref="RiskScoreRegressionTests"/>.
/// </summary>
public sealed class RiskScoreCalculatorTests
{
    private static readonly RiskMatrix Matrix = TestData.Matrix();

    [Fact]
    public void FailedPenTest_IsTheTopFactor_ButLevelFollowsTheScore()
    {
        // Strong on every other axis, but the penetration test failed.
        VendorProfile vendor = TestData.Vendor(
            financialHealth: 95, slaUptime: 99, majorIncidents: 0,
            certs: ["ISO27001"], contractValid: true, privacyPolicyValid: true, pentestReportValid: false);

        RiskAssessment result = RiskScoreCalculator.Calculate(vendor, Matrix);

        // The failed pentest is the most severe factor and maxes the security dimension…
        RiskFactor pentest = Assert.Single(result.Factors, f => f.Code == "failedPenTest");
        Assert.Equal(1.0, pentest.Severity);
        DimensionScore security = result.Dimensions.Single(d => d.Dimension == RiskDimension.Security);
        Assert.Equal(1.0, security.Score, precision: 4);

        // …but the level is NOT force-overridden — it follows the weighted score (here, Low).
        Assert.True(result.RiskScore < 0.35, $"Score was {result.RiskScore}");
        Assert.Equal(RiskLevel.Low, result.RiskLevel);
    }

    [Fact]
    public void Propagation_RaisesSeverityAboveBase_WhenFactorHasCorrelatedRisks()
    {
        VendorProfile vendor = TestData.Vendor(
            financialHealth: 90, slaUptime: 99, majorIncidents: 0,
            certs: [], contractValid: true, privacyPolicyValid: true, pentestReportValid: true);

        RiskAssessment result = RiskScoreCalculator.Calculate(vendor, Matrix);
        RiskFactor iso = Assert.Single(result.Factors, f => f.Code == "missingISO27001");

        Assert.True(iso.Severity > iso.BaseSeverity,
            $"Propagated severity {iso.Severity} should exceed base {iso.BaseSeverity}.");
        Assert.NotEmpty(iso.CorrelatedRisks);
    }

    [Fact]
    public void WithoutMatrix_SeverityEqualsBase()
    {
        VendorProfile vendor = TestData.Vendor(
            financialHealth: 90, slaUptime: 99, majorIncidents: 0,
            certs: [], contractValid: true, privacyPolicyValid: true, pentestReportValid: true);

        RiskAssessment result = RiskScoreCalculator.Calculate(vendor, RiskMatrix.Empty);
        RiskFactor iso = Assert.Single(result.Factors, f => f.Code == "missingISO27001");

        Assert.Equal(iso.BaseSeverity, iso.Severity);
    }

    [Fact]
    public void Reason_NamesOnlyTheVendorsTriggeredFactors()
    {
        RiskAssessment result = RiskScoreCalculator.Calculate(TestData.TechPlus(), Matrix);

        // Only the vendor's own factors — not the matrix's correlated risks.
        Assert.Contains("SLA below 95%", result.Reason);
        Assert.Contains("Privacy policy expired", result.Reason);
        Assert.DoesNotContain("Correlated", result.Reason);
    }

    [Fact]
    public void LowerRiskVendor_ScoresBelowHigherRiskVendor()
    {
        double securePay = RiskScoreCalculator.Calculate(TestData.SecurePay(), Matrix).RiskScore;
        double trustCom = RiskScoreCalculator.Calculate(TestData.TrustCom(), Matrix).RiskScore;

        Assert.True(securePay < trustCom);
    }
}
