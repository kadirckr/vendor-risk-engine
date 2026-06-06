using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using VendorRisk.Application.Abstractions;
using VendorRisk.Application.Services;
using VendorRisk.Domain.Risk;

namespace VendorRisk.Tests;

/// <summary>
/// Verifies the caching decorator: identical inputs are served from the cache (no recompute),
/// while different inputs are computed separately.
/// </summary>
public sealed class CachingRuleEngineServiceTests
{
    private static IDistributedCache NewCache() =>
        new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

    private static (CachingRuleEngineService Engine, Mock<IRiskMatrixProvider> Provider, Mock<IReasonHumanizer> Humanizer) Build()
    {
        Mock<IRiskMatrixProvider> provider = new();
        provider.Setup(p => p.GetMatrix()).Returns(TestData.Matrix());

        // Passthrough humanizer: returns the rule-based reason unchanged, keeping the test focused
        // on the caching behaviour (and asserting the AI layer is not re-invoked on a cache hit).
        Mock<IReasonHumanizer> humanizer = new();
        humanizer
            .Setup(h => h.HumanizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((string reason, CancellationToken _) => Task.FromResult(reason));

        CachingRuleEngineService engine = new(
            new HumanizingRuleEngineService(new RuleEngineService(provider.Object), humanizer.Object),
            NewCache(),
            NullLogger<CachingRuleEngineService>.Instance);

        return (engine, provider, humanizer);
    }

    [Fact]
    public async Task SecondQuery_WithSameInputs_IsServedFromCache_NotRecomputed()
    {
        (CachingRuleEngineService engine, Mock<IRiskMatrixProvider> provider, Mock<IReasonHumanizer> humanizer) = Build();

        RiskAssessment first = await engine.EvaluateAsync(TestData.TrustCom());
        RiskAssessment second = await engine.EvaluateAsync(TestData.TrustCom());

        Assert.Equal(first.RiskScore, second.RiskScore);
        Assert.Equal(first.RiskLevel, second.RiskLevel);
        Assert.Equal(first.Reason, second.Reason);

        // The inner engine pulls the matrix only on the first (computed) call; the second is a hit.
        provider.Verify(p => p.GetMatrix(), Times.Once);

        // A cache hit must NOT re-invoke the AI humanizer — the humanized reason is already cached.
        humanizer.Verify(h => h.HumanizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DifferentInputs_AreComputedSeparately()
    {
        (CachingRuleEngineService engine, Mock<IRiskMatrixProvider> provider, _) = Build();

        await engine.EvaluateAsync(TestData.TrustCom());
        await engine.EvaluateAsync(TestData.SecurePay());

        // Distinct inputs → distinct keys → two computations.
        provider.Verify(p => p.GetMatrix(), Times.Exactly(2));
    }
}
