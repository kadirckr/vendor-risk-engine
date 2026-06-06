using ErrorOr;

namespace VendorRisk.Application.BuildingBlocks;

/// <summary>
/// A simple dispatcher (mediator) that routes a request to its handler.
/// Endpoints never call handlers directly; they go through this.
/// </summary>
public interface IDispatcher
{
    /// <summary>Resolves the handler for the request type and executes it.</summary>
    Task<ErrorOr<TResponse>> DispatchAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken ct = default);
}
