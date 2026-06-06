using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using VendorRisk.Application.Abstractions;

namespace VendorRisk.Infrastructure.Ai;

/// <summary>
/// <see cref="IReasonHumanizer"/> backed by an <see cref="IChatClient"/> (Microsoft.Extensions.AI),
/// so the provider (OpenAI, Azure OpenAI, Ollama, …) can be swapped without touching this class.
/// Best-effort: any failure is logged and the original rule-based reason is returned unchanged.
/// </summary>
internal sealed class ChatClientReasonHumanizer(
    IChatClient chatClient,
    ILogger<ChatClientReasonHumanizer> logger) : IReasonHumanizer
{
    private const string SystemPrompt =
        "You rewrite vendor risk summaries. Turn the input into a single, concise, " +
        "human-readable explanation sentence. Preserve the overall risk level and every " +
        "risk factor mentioned. Do not use bullet points, lists, or markdown.";

    /// <inheritdoc />
    public async Task<string> HumanizeAsync(string reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return reason;
        }

        try
        {
            ChatResponse response = await chatClient.GetResponseAsync(
                [
                    new ChatMessage(ChatRole.System, SystemPrompt),
                    new ChatMessage(ChatRole.User, reason),
                ],
                new ChatOptions { Temperature = 0.2f },
                ct);

            string text = response.Text;
            return string.IsNullOrWhiteSpace(text) ? reason : text.Trim();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Reason humanization failed; falling back to the rule-based reason.");
            return reason;
        }
    }
}
