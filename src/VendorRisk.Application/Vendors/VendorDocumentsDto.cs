namespace VendorRisk.Application.Vendors;

/// <summary>
/// Document validity flags as sent/received over the API. Tri-state: <c>true</c> valid,
/// <c>false</c> expired/failed, <c>null</c> not assessed.
/// </summary>
public sealed record VendorDocumentsDto(
    bool? ContractValid = null,
    bool? PrivacyPolicyValid = null,
    bool? PentestReportValid = null);
