namespace VendorRisk.Application.UseCases.CreateVendor;

/// <summary>Result of creating a vendor: the generated id and name.</summary>
public sealed record CreateVendorResult(int Id, string Name);
