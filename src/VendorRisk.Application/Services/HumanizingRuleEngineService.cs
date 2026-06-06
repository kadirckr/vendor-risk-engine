using VendorRisk.Application.Abstractions;
using VendorRisk.Domain.Entities;
using VendorRisk.Domain.Risk;

namespace VendorRisk.Application.Services;

/// <summary>
/// Decorator over <see cref="RuleEngineService"/> that replaces the rule-based reason with an
/// AI-generated, human-readable one via <see cref="IReasonHumanizer"/>. It sits inside the caching
/// decorator (Caching → Humanizing → RuleEngine), so the humanized reason is what gets cached.
/// Humanization is best-effort: the humanizer falls back to the original reason on failure.
/// </summary>
internal sealed class HumanizingRuleEngineService(
    RuleEngineService inner,
    IReasonHumanizer humanizer) : IRuleEngineService
{
    /// <inheritdoc />
    public async Task<RiskAssessment> EvaluateAsync(VendorProfile vendor, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(vendor);

        RiskAssessment assessment = await inner.EvaluateAsync(vendor, ct);
        string humanReason = await humanizer.HumanizeAsync(assessment.Reason, ct);

        return assessment with { Reason = humanReason };
    }
}
