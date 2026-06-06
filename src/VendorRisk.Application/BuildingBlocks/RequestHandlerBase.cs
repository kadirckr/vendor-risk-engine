using ErrorOr;

namespace VendorRisk.Application.BuildingBlocks;

/// <summary>Base handler: runs <see cref="ValidateRequest"/> first; calls <see cref="ExecuteAsync"/> only when valid.</summary>
public abstract class RequestHandlerBase<TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
{
    /// <inheritdoc />
    public async Task<ErrorOr<TResponse>> HandleAsync(TRequest request, CancellationToken ct = default)
    {
        List<Error> errors = [.. ValidateRequest(request)];
        if (errors.Count > 0)
        {
            return errors;
        }

        return await ExecuteAsync(request, ct);
    }

    /// <summary>Business-rule validations. Returns empty when there are no errors (default).</summary>
    protected virtual IEnumerable<Error> ValidateRequest(TRequest request) => [];

    /// <summary>The actual business logic. Runs only when validation passes.</summary>
    protected abstract Task<ErrorOr<TResponse>> ExecuteAsync(TRequest request, CancellationToken ct);
}
