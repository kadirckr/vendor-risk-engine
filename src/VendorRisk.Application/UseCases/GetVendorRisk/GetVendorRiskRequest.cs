namespace VendorRisk.Application.UseCases.GetVendorRisk;

/// <summary>Input for scoring a vendor (GET /api/vendor/{id}/risk).</summary>
public sealed record GetVendorRiskRequest(int VendorId);
