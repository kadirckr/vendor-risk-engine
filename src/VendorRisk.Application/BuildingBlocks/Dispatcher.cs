using ErrorOr;
using Microsoft.Extensions.DependencyInjection;

namespace VendorRisk.Application.BuildingBlocks;

/// <summary>
/// Dispatcher implementation that resolves the handler from the DI container and runs it.
/// </summary>
internal sealed class Dispatcher(IServiceProvider provider) : IDispatcher
{
    /// <inheritdoc />
    public Task<ErrorOr<TResponse>> DispatchAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken ct = default)
    {
        IRequestHandler<TRequest, TResponse> handler =
            provider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();

        return handler.HandleAsync(request, ct);
    }
}
