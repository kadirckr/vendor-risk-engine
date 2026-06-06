namespace VendorRisk.Application.Abstractions;

/// <summary>
/// Rewrites the deterministic, rule-based risk reason into a single human-readable
/// explanation. Implementations are best-effort: any failure (or a disabled AI backend)
/// must fall back to returning the original reason unchanged, so scoring never depends on
/// the AI being available.
/// </summary>
public interface IReasonHumanizer
{
    /// <summary>Returns a human-readable version of <paramref name="reason"/>, or the original on failure.</summary>
    Task<string> HumanizeAsync(string reason, CancellationToken ct = default);
}
