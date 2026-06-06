namespace VendorRisk.Domain.Enums;

/// <summary>The three weighted dimensions that make up the final risk score.</summary>
public enum RiskDimension
{
    /// <summary>Financial stability (weight 0.40).</summary>
    Financial = 0,

    /// <summary>Operational reliability — SLA, incidents (weight 0.30).</summary>
    Operational = 1,

    /// <summary>Security &amp; compliance — certifications, document validity (weight 0.30).</summary>
    Security = 2,
}
