using ErrorOr;
using Microsoft.Extensions.Logging;
using VendorRisk.Application.Abstractions;
using VendorRisk.Application.BuildingBlocks;
using VendorRisk.Application.Vendors;
using VendorRisk.Domain.Entities;
using VendorRisk.Domain.Risk;

namespace VendorRisk.Application.UseCases.ListVendors;

/// <summary>Lists every vendor with its computed risk score/level, highest risk first.</summary>
internal sealed class ListVendorsHandler(
    IVendorRepository repository,
    IRuleEngineService ruleEngine,
    ILogger<ListVendorsHandler> logger)
    : RequestHandlerBase<ListVendorsRequest, ListVendorsResult>
{
    /// <inheritdoc />
    protected override async Task<ErrorOr<ListVendorsResult>> ExecuteAsync(
        ListVendorsRequest request, CancellationToken ct)
    {
        IReadOnlyList<VendorProfile> vendors = await repository.GetAllAsync(ct);

        // Evaluated sequentially: each result is cached, so repeat listings stay cheap and the
        // shared AI backend is not hammered with a burst of concurrent humanization calls.
        List<VendorSummaryResponse> summaries = [];
        foreach (VendorProfile vendor in vendors)
        {
            RiskAssessment assessment = await ruleEngine.EvaluateAsync(vendor, ct);
            summaries.Add(VendorSummaryResponse.From(vendor, assessment));
        }

        summaries = [.. summaries.OrderByDescending(s => s.RiskScore).ThenBy(s => s.Name)];

        logger.LogInformation("Listed {Count} vendors.", summaries.Count);
        return new ListVendorsResult(summaries);
    }
}
