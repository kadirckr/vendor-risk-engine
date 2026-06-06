using System.Text.Json;
using VendorRisk.Domain.Entities;

namespace VendorRisk.Infrastructure.RiskData;

/// <summary>
/// Reads the seed vendor dataset (SampleVendorData.json) and maps it to domain entities.
/// The JSON <c>id</c> is intentionally ignored so the database assigns identities.
/// </summary>
public static class SampleVendorLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>Loads seed vendors, or an empty list when the file is missing/invalid.</summary>
    public static IReadOnlyList<VendorProfile> Load(string path)
    {
        if (!File.Exists(path))
        {
            return [];
        }

        using FileStream stream = File.OpenRead(path);
        SeedFile? file = JsonSerializer.Deserialize<SeedFile>(stream, SerializerOptions);

        return file?.Vendors is null
            ? []
            : [.. file.Vendors.Select(Map)];
    }

    private static VendorProfile Map(SeedVendor v) => new()
    {
        Name = v.Name ?? string.Empty,
        FinancialHealth = v.FinancialHealth,
        SlaUptime = v.SlaUptime,
        MajorIncidents = v.MajorIncidents,
        SecurityCerts = [.. v.SecurityCerts ?? []],
        Documents = new VendorDocuments
        {
            ContractValid = v.Documents?.ContractValid,
            PrivacyPolicyValid = v.Documents?.PrivacyPolicyValid,
            PentestReportValid = v.Documents?.PentestReportValid,
        },
    };

    private sealed record SeedFile(List<SeedVendor>? Vendors);

    private sealed record SeedVendor(
        string? Name,
        int FinancialHealth,
        int SlaUptime,
        int MajorIncidents,
        List<string>? SecurityCerts,
        SeedDocuments? Documents);

    private sealed record SeedDocuments(
        bool? ContractValid,
        bool? PrivacyPolicyValid,
        bool? PentestReportValid);
}
