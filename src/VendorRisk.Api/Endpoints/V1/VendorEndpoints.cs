using VendorRisk.Api.Common;
using VendorRisk.Application.BuildingBlocks;
using VendorRisk.Application.UseCases.CreateVendor;
using VendorRisk.Application.UseCases.GetVendorRisk;
using VendorRisk.Application.UseCases.ListVendors;

namespace VendorRisk.Api.Endpoints.V1;

/// <summary>
/// Minimal API endpoints for vendors and their risk assessments. Mapped from Program.cs.
/// Endpoints only translate HTTP ↔ dispatcher calls; all logic lives in the handlers.
/// </summary>
public static class VendorEndpoints
{
    public static IEndpointRouteBuilder MapVendorEndpoints(this IEndpointRouteBuilder routes)
    {
        RouteGroupBuilder group = routes
            .MapGroup("/api/vendor")
            .WithTags("Vendors");

        group.MapPost("/", async (
                CreateVendorRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                ErrorOr.ErrorOr<CreateVendorResult> result =
                    await dispatcher.DispatchAsync<CreateVendorRequest, CreateVendorResult>(request, ct);

                return result.ToApiResult(value => Results.Created($"/api/vendor/{value.Id}/risk", value));
            })
            .WithName("CreateVendor")
            .WithSummary("Registers a new vendor and returns its generated id.");

        group.MapGet("/", async (IDispatcher dispatcher, CancellationToken ct) =>
            {
                ErrorOr.ErrorOr<ListVendorsResult> result =
                    await dispatcher.DispatchAsync<ListVendorsRequest, ListVendorsResult>(
                        new ListVendorsRequest(), ct);

                return result.ToApiResult();
            })
            .WithName("ListVendors")
            .WithSummary("Lists all vendors with their computed risk score and level (highest risk first).");

        group.MapGet("/{id:int}/risk", async (
                int id,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                ErrorOr.ErrorOr<Application.Vendors.RiskAssessmentResponse> result =
                    await dispatcher.DispatchAsync<GetVendorRiskRequest, Application.Vendors.RiskAssessmentResponse>(
                        new GetVendorRiskRequest(id), ct);

                return result.ToApiResult();
            })
            .WithName("GetVendorRisk")
            .WithSummary("Returns the explainable risk assessment (score, level, reason, breakdown) for a vendor.");

        return routes;
    }
}
