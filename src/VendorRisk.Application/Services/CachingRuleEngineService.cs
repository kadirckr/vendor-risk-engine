using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using VendorRisk.Application.Abstractions;
using VendorRisk.Domain.Entities;
using VendorRisk.Domain.Risk;

namespace VendorRisk.Application.Services;

/// <summary>
/// Caching decorator over <see cref="HumanizingRuleEngineService"/>, registered only when a
/// distributed cache (Redis) is configured. Identical vendor inputs map to the same cache key,
/// so repeated queries are served from the cache; since it wraps the humanizing engine, the
/// cached assessment already holds the AI reason and a hit never re-hits the AI backend.
/// Caching is best-effort: any cache failure is swallowed and a fresh result is still returned.
/// </summary>
internal sealed class CachingRuleEngineService(
    HumanizingRuleEngineService inner,
    IDistributedCache cache,
    ILogger<CachingRuleEngineService> logger) : IRuleEngineService
{
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
    };

    /// <inheritdoc />
    public async Task<RiskAssessment> EvaluateAsync(VendorProfile vendor, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(vendor);
        string key = BuildKey(vendor);

        // Try the cache; never let a cache problem break scoring.
        try
        {
            string? cached = await cache.GetStringAsync(key, ct);
            if (cached is not null)
            {
                RiskAssessment? hit = JsonSerializer.Deserialize<RiskAssessment>(cached);
                if (hit is not null)
                {
                    // Re-attach the current vendor's id (an identical-input vendor may differ by id).
                    return hit with { VendorId = vendor.Id };
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Risk cache read failed; computing without cache.");
        }

        RiskAssessment result = await inner.EvaluateAsync(vendor, ct);

        try
        {
            await cache.SetStringAsync(key, JsonSerializer.Serialize(result), CacheOptions, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Risk cache write failed.");
        }

        return result;
    }

    /// <summary>Deterministic key over the scored inputs: same values → same key.</summary>
    private static string BuildKey(VendorProfile vendor)
    {
        string raw = string.Join('|',
            vendor.FinancialHealth,
            vendor.SlaUptime,
            vendor.MajorIncidents,
            string.Join(',', vendor.SecurityCerts.OrderBy(c => c, StringComparer.Ordinal)),
            vendor.Documents.ContractValid,
            vendor.Documents.PrivacyPolicyValid,
            vendor.Documents.PentestReportValid);

        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
        return $"risk:{hash}";
    }
}
