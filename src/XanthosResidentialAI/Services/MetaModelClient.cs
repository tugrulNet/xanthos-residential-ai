using XanthosResidentialAI;

namespace XanthosResidentialAI.Services;

public interface IMetaModelClient
{
    /// <summary>False until the Meta Model API endpoint, model name and
    /// request/response schema are verified against the official docs.</summary>
    bool IsConfigured { get; }

    /// <summary>Asks the buyer assistant (Muse Spark) for a reply.
    /// contextData contains exactly the score/photo data shown on the page —
    /// the assistant must not go beyond it. No price predictions, no legal
    /// (bylaw) answers; missing data is stated as missing.</summary>
    Task<AssistantMessage> ReplyAsync(
        IReadOnlyList<AssistantMessage> history,
        string contextData,
        CancellationToken cancellationToken = default);
}

/// <summary>Muse Spark assistant client (Meta Model API).
///
/// TODO (28 Oct step): implement the real HTTP call.
/// BLOCKED until verified against the Meta Model API documentation:
///   - endpoint URL
///   - model name (Muse Spark)
///   - request/response schema
/// Rule (AGENTS.md): do NOT invent URLs, model names or schemas. The API key
/// is read from the environment only (METAMODEL_API_KEY) and never enters
/// this repository.</summary>
public sealed class MetaModelClient : IMetaModelClient
{
    // Flips to true only after the endpoint and schema are verified in docs.
    private const bool EndpointVerified = false;

    private static readonly string? ApiKey = Environment.GetEnvironmentVariable("METAMODEL_API_KEY");

    public bool IsConfigured => EndpointVerified && !string.IsNullOrWhiteSpace(ApiKey);

    public Task<AssistantMessage> ReplyAsync(
        IReadOnlyList<AssistantMessage> history,
        string contextData,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            AssistantMessage placeholder = new(
                "assistant",
                "The assistant is not connected yet: the Meta Model API endpoint and " +
                "request/response schema still need to be verified against the official " +
                "documentation (28 Oct step). Nothing is invented in the meantime.",
                Array.Empty<string>());
            return Task.FromResult(placeholder);
        }

        // TODO: real call — HttpClient to the verified endpoint with the API
        // key from the environment; map the response into AssistantMessage.
        throw new NotImplementedException(
            "Meta Model API call is not implemented: endpoint/schema not verified yet.");
    }
}
