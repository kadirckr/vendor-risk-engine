using VendorRisk.Domain.Enums;

namespace VendorRisk.Domain.Risk;

/// <summary>
/// A single risk factor triggered by a vendor's data, after similarity propagation.
/// Carries everything needed to explain the score.
/// </summary>
/// <param name="Code">Stable factor identifier, matching a key in the Risk Similarity Matrix (e.g. "missingISO27001").</param>
/// <param name="Dimension">Which scoring dimension this factor belongs to.</param>
/// <param name="Label">Short human-readable label, used in the risk reason (e.g. "Missing ISO27001").</param>
/// <param name="Description">Full human-readable explanation.</param>
/// <param name="BaseSeverity">Severity from the base rule alone, in [0,1].</param>
/// <param name="Severity">Severity after similarity propagation, in [0,1].</param>
/// <param name="CorrelatedRisks">Correlated risks pulled from the matrix (drive the propagation and the explanation).</param>
public sealed record RiskFactor(
    string Code,
    RiskDimension Dimension,
    string Label,
    string Description,
    double BaseSeverity,
    double Severity,
    IReadOnlyList<CorrelatedRisk> CorrelatedRisks);
