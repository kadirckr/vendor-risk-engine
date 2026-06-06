using VendorRisk.Application.Vendors;

namespace VendorRisk.Application.UseCases.CreateVendor;

/// <summary>Input for registering a new vendor (POST /api/vendor).</summary>
public sealed record CreateVendorRequest(
    string Name,
    int FinancialHealth,
    int SlaUptime,
    int MajorIncidents,
    IReadOnlyList<string>? SecurityCerts,
    VendorDocumentsDto? Documents);
