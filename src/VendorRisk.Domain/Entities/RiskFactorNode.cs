namespace VendorRisk.Domain.Entities;

/// <summary>
/// A node of the Risk Similarity Matrix graph (both primary factors and leaf-only correlated
/// risks), Table 2 of 3; belongs to a <see cref="RiskCategory"/>.
/// </summary>
public sealed class RiskFactorNode
{
    /// <summary>Database identity (auto-generated).</summary>
    public int Id { get; set; }

    /// <summary>FK to the owning category.</summary>
    public int CategoryId { get; set; }

    /// <summary>The owning category.</summary>
    public RiskCategory? Category { get; set; }

    /// <summary>Stable code, e.g. "missingISO27001". Unique across the matrix.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Optional display name.</summary>
    public string? Name { get; set; }
}
