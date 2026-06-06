namespace VendorRisk.Domain.Entities;

/// <summary>
/// A directed, weighted edge of the Risk Similarity Matrix (<c>parent → child</c>), Table 3 of 3.
/// Each row has its own id, so the same child reached from two parents is two distinct edges.
/// </summary>
public sealed class RiskFactorEdge
{
    /// <summary>Database identity (auto-generated).</summary>
    public int Id { get; set; }

    /// <summary>FK to the parent (source) factor node.</summary>
    public int ParentFactorId { get; set; }

    /// <summary>The parent (source) node.</summary>
    public RiskFactorNode? Parent { get; set; }

    /// <summary>FK to the child (target) factor node.</summary>
    public int ChildFactorId { get; set; }

    /// <summary>The child (target) node.</summary>
    public RiskFactorNode? Child { get; set; }

    /// <summary>Similarity weight in [0,1].</summary>
    public double Weight { get; set; }
}
