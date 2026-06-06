using ErrorOr;

namespace VendorRisk.Application.BuildingBlocks;

/// <summary>
/// Contract for a use-case handler. There is exactly one handler per request type.
/// </summary>
/// <typeparam name="TRequest">The request (input) DTO type.</typeparam>
/// <typeparam name="TResponse">The response (output) type.</typeparam>
public interface IRequestHandler<in TRequest, TResponse>
{
    /// <summary>Handles the request and returns either a success value or errors (ErrorOr).</summary>
    Task<ErrorOr<TResponse>> HandleAsync(TRequest request, CancellationToken ct = default);
}
