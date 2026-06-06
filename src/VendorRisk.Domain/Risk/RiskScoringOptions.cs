namespace VendorRisk.Domain.Risk;

/// <summary>
/// Tunable constants for the scoring engine. Centralised here so the weighting stays
/// consistent and auditable (and can be overridden from configuration if needed).
/// Defaults are calibrated against the sample vendor dataset.
/// </summary>
public sealed class RiskScoringOptions
{
    /// <summary>Weight of the financial dimension in the final score.</summary>
    public double FinancialWeight { get; init; } = 0.40;

    /// <summary>Weight of the operational dimension in the final score.</summary>
    public double OperationalWeight { get; init; } = 0.30;

    /// <summary>Weight of the security &amp; compliance dimension in the final score.</summary>
    public double SecurityWeight { get; init; } = 0.30;

    /// <summary>
    /// Damping factor (β) controlling how strongly the Risk Similarity Matrix amplifies a
    /// triggered factor. 0 = ignore the matrix; 1 = full correlated-risk influence.
    /// </summary>
    public double PropagationDamping { get; init; } = 0.30;

    /// <summary>SLA uptime percentage below which an SLA-drop risk is triggered.</summary>
    public int SlaThreshold { get; init; } = 95;

    /// <summary>Score at or above which the level is Medium.</summary>
    public double MediumThreshold { get; init; } = 0.35;

    /// <summary>Score at or above which the level is High.</summary>
    public double HighThreshold { get; init; } = 0.60;

    /// <summary>Score at or above which the level is Critical.</summary>
    public double CriticalThreshold { get; init; } = 0.85;

    /// <summary>Minimum base severity for a factor to be named in the human-readable reason.</summary>
    public double ReasonThreshold { get; init; } = 0.40;

    /// <summary>The default, calibrated configuration.</summary>
    public static RiskScoringOptions Default { get; } = new();
}
