using ErrorOr;
using Microsoft.Extensions.Logging;
using VendorRisk.Application.Abstractions;
using VendorRisk.Application.BuildingBlocks;
using VendorRisk.Domain.Entities;

namespace VendorRisk.Application.UseCases.CreateVendor;

/// <summary>Validates and persists a new vendor.</summary>
internal sealed class CreateVendorHandler(IVendorRepository repository, ILogger<CreateVendorHandler> logger)
    : RequestHandlerBase<CreateVendorRequest, CreateVendorResult>
{
    /// <inheritdoc />
    protected override IEnumerable<Error> ValidateRequest(CreateVendorRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            yield return Error.Validation("Name.Required", "Vendor name is required.");
        }

        if (request.FinancialHealth is < 0 or > 100)
        {
            yield return Error.Validation("FinancialHealth.Range", "Financial health must be between 0 and 100.");
        }

        if (request.SlaUptime is < 0 or > 100)
        {
            yield return Error.Validation("SlaUptime.Range", "SLA uptime must be between 0 and 100.");
        }

        if (request.MajorIncidents < 0)
        {
            yield return Error.Validation("MajorIncidents.Range", "Major incidents cannot be negative.");
        }
    }

    /// <inheritdoc />
    protected override async Task<ErrorOr<CreateVendorResult>> ExecuteAsync(
        CreateVendorRequest request, CancellationToken ct)
    {
        VendorProfile vendor = new()
        {
            Name = request.Name.Trim(),
            FinancialHealth = request.FinancialHealth,
            SlaUptime = request.SlaUptime,
            MajorIncidents = request.MajorIncidents,
            SecurityCerts = [.. request.SecurityCerts ?? []],
            Documents = new VendorDocuments
            {
                ContractValid = request.Documents?.ContractValid,
                PrivacyPolicyValid = request.Documents?.PrivacyPolicyValid,
                PentestReportValid = request.Documents?.PentestReportValid,
            },
        };

        ErrorOr<VendorProfile> saved = await repository.AddAsync(vendor, ct);
        if (saved.IsError)
        {
            return saved.Errors;
        }

        logger.LogInformation("Vendor created: {VendorId} ({VendorName}).", saved.Value.Id, saved.Value.Name);
        return new CreateVendorResult(saved.Value.Id, saved.Value.Name);
    }
}
