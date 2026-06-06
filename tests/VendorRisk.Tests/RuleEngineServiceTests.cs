using Moq;
using VendorRisk.Application.Abstractions;
using VendorRisk.Application.Services;
using VendorRisk.Domain.Enums;
using VendorRisk.Domain.Risk;

namespace VendorRisk.Tests;

/// <summary>
/// Tests the thin orchestration in <see cref="RuleEngineService"/> with a mocked matrix
/// provider — demonstrating the interface-based, testable design.
/// </summary>
public sealed class RuleEngineServiceTests
{
    [Fact]
    public async Task Evaluate_PullsMatrixFromProvider_AndScoresVendor()
    {
        Mock<IRiskMatrixProvider> provider = new();
        provider.Setup(p => p.GetMatrix()).Returns(TestData.Matrix());

        RuleEngineService engine = new(provider.Object);
        RiskAssessment result = await engine.EvaluateAsync(TestData.TrustCom());

        Assert.Equal(RiskLevel.Critical, result.RiskLevel);
        provider.Verify(p => p.GetMatrix(), Times.Once);
    }

    [Fact]
    public async Task Evaluate_WithEmptyMatrix_StillProducesValidAssessment()
    {
        Mock<IRiskMatrixProvider> provider = new();
        provider.Setup(p => p.GetMatrix()).Returns(RiskMatrix.Empty);

        RuleEngineService engine = new(provider.Object);
        RiskAssessment result = await engine.EvaluateAsync(TestData.TechPlus());

        Assert.False(string.IsNullOrWhiteSpace(result.Reason));
        Assert.InRange(result.RiskScore, 0.0, 1.0);
    }
}
