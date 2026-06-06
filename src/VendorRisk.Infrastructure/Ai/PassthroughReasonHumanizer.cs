using VendorRisk.Application.Abstractions;

namespace VendorRisk.Infrastructure.Ai;

/// <summary>
/// No-op <see cref="IReasonHumanizer"/> used when no AI backend is configured: returns the
/// rule-based reason unchanged, so the engine stays fully functional without an API key.
/// </summary>
internal sealed class PassthroughReasonHumanizer : IReasonHumanizer
{
    /// <inheritdoc />
    public Task<string> HumanizeAsync(string reason, CancellationToken ct = default)
        => Task.FromResult(reason);
}
