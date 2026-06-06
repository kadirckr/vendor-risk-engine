using VendorRisk.Application.Vendors;

namespace VendorRisk.Application.UseCases.ListVendors;

/// <summary>All vendors with their risk, highest risk first.</summary>
public sealed record ListVendorsResult(IReadOnlyList<VendorSummaryResponse> Vendors);
