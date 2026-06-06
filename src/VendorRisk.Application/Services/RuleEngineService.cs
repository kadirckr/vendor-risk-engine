using VendorRisk.Application.Abstractions;
using VendorRisk.Domain.Entities;
using VendorRisk.Domain.Risk;

namespace VendorRisk.Application.Services;

/// <summary>
/// Default <see cref="IRuleEngineService"/>: pulls the similarity matrix from the provider
/// and delegates to the pure <see cref="RiskScoreCalculator"/>. Kept thin so the scoring
/// logic stays in the domain (and trivially testable).
/// </summary>
internal sealed class RuleEngineService(IRiskMatrixProvider matrixProvider) : IRuleEngineService
{
    private readonly RiskScoringOptions _options = RiskScoringOptions.Default;

    /// <inheritdoc />
    public Task<RiskAssessment> EvaluateAsync(VendorProfile vendor, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(vendor);
        return Task.FromResult(
            RiskScoreCalculator.Calculate(vendor, matrixProvider.GetMatrix(), _options));
    }
}
