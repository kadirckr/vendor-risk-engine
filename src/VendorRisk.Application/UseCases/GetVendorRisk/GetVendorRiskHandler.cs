using ErrorOr;
using Microsoft.Extensions.Logging;
using VendorRisk.Application.Abstractions;
using VendorRisk.Application.BuildingBlocks;
using VendorRisk.Application.Vendors;
using VendorRisk.Domain.Entities;
using VendorRisk.Domain.Risk;

namespace VendorRisk.Application.UseCases.GetVendorRisk;

/// <summary>Loads a vendor and runs the rule engine to produce an explainable risk assessment.</summary>
internal sealed class GetVendorRiskHandler(
    IVendorRepository repository,
    IRuleEngineService ruleEngine,
    ILogger<GetVendorRiskHandler> logger)
    : RequestHandlerBase<GetVendorRiskRequest, RiskAssessmentResponse>
{
    /// <inheritdoc />
    protected override IEnumerable<Error> ValidateRequest(GetVendorRiskRequest request)
    {
        if (request.VendorId <= 0)
        {
            yield return Error.Validation("VendorId.Invalid", "Vendor id must be a positive integer.");
        }
    }

    /// <inheritdoc />
    protected override async Task<ErrorOr<RiskAssessmentResponse>> ExecuteAsync(
        GetVendorRiskRequest request, CancellationToken ct)
    {
        VendorProfile? vendor = await repository.GetByIdAsync(request.VendorId, ct);
        if (vendor is null)
        {
            logger.LogWarning("Risk requested for unknown vendor {VendorId}.", request.VendorId);
            return Error.NotFound("Vendor.NotFound", $"Vendor {request.VendorId} was not found.");
        }

        RiskAssessment assessment = await ruleEngine.EvaluateAsync(vendor, ct);
        logger.LogInformation(
            "Risk evaluated for vendor {VendorId}: {Score} ({Level}).",
            vendor.Id, assessment.RiskScore, assessment.RiskLevel);
        return RiskAssessmentResponse.From(vendor, assessment);
    }
}
