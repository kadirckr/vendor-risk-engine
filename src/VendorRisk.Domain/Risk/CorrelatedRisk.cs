namespace VendorRisk.Domain.Risk;

/// <summary>
/// A risk correlated with a primary risk factor, as defined in the Risk Similarity Matrix.
/// </summary>
/// <param name="Name">The related risk identifier (e.g. "weakAccessControl").</param>
/// <param name="Weight">Similarity weight in [0,1] — how strongly the two risks co-occur.</param>
public sealed record CorrelatedRisk(string Name, double Weight);
