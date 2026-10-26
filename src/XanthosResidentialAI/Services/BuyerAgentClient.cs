using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using XanthosResidentialAI;

namespace XanthosResidentialAI.Services;

public interface IBuyerAgentClient
{
    /// <summary>Asks the Python buyer agent one question with the current
    /// page data as context. Never hangs: the HttpClient timeout is set, and
    /// a failed call returns an honest "job failed" reply — the page keeps
    /// its cached data instead of blanking out.</summary>
    Task<AgentReply> AskAsync(
        string address,
        string question,
        ScoreResult? score,
        IReadOnlyList<PhotoAnalysisResult> photos,
        PhotoTriageSummary? triage,
        CancellationToken cancellationToken = default);
}

/// <summary>HTTP client for the Python buyer agent (python/buyer-agent,
/// POST /agent/ask on localhost). The agent is LangGraph + Muse Spark;
/// .NET only renders its steps and final answer (AGENTS.md). The Meta API
/// key never touches .NET — it lives in the Python service's environment.
///
/// Contract (verified against python/buyer-agent/app.py in this repo):
///   request  {address, question, context{address, score, photo_urls, triage}}
///   response {answer, sources[], step_log[{step, detail}], mode, error}</summary>
public sealed class BuyerAgentClient : IBuyerAgentClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient http;

    public BuyerAgentClient(HttpClient http) => this.http = http;

    public async Task<AgentReply> AskAsync(
        string address,
        string question,
        ScoreResult? score,
        IReadOnlyList<PhotoAnalysisResult> photos,
        PhotoTriageSummary? triage,
        CancellationToken cancellationToken = default)
    {
        var request = new
        {
            address,
            question,
            context = new
            {
                address,
                score = score is null ? null : new
                {
                    address = score.Score.Address,
                    overall = score.Score.Overall,
                    livability = score.Score.Livability,
                    investment = score.Score.Investment,
                    components = score.Score.Components
                        .Select(c => new { name = c.Name, value = c.Value, confidence = c.Confidence }),
                    sources = score.Score.Sources,
                    source_label = score.SourceLabel,
                    is_demo_cache = score.IsDemoCache,
                },
                photo_urls = photos.Select(p => p.PhotoUrl).ToList(),
                triage = triage is null ? null : new
                {
                    total = triage.TotalPhotos,
                    kept = triage.KeptPhotos,
                    discarded = triage.DiscardedPhotos,
                    by_reason = triage.DiscardedByReason,
                },
            },
        };

        try
        {
            using var response = await http.PostAsJsonAsync(
                "/agent/ask", request, JsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var dto = await response.Content.ReadFromJsonAsync<AgentReplyDto>(JsonOptions, cancellationToken)
                      ?? throw new InvalidOperationException("Agent returned an empty reply.");

            return new AgentReply(
                Answer: dto.answer ?? string.Empty,
                Sources: dto.sources ?? Array.Empty<string>(),
                Steps: (dto.step_log ?? Array.Empty<AgentStepDto>())
                    .Select(s => new AgentStep(s.step ?? string.Empty, s.detail ?? string.Empty))
                    .ToList(),
                Mode: dto.mode ?? "unknown",
                Error: dto.error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Python failure must not hang or blank the page: honest error,
            // page keeps its cached demo data.
            return new AgentReply(
                Answer: "The buyer agent service is not reachable (job marked failed). " +
                        "The page keeps its cached data — nothing is invented here.",
                Sources: Array.Empty<string>(),
                Steps: new[] { new AgentStep("agent", $"connection to python/buyer-agent failed: {Truncate(ex.Message)}") },
                Mode: "unavailable",
                Error: Truncate(ex.Message));
        }
    }

    private static string Truncate(string value) => value.Length <= 200 ? value : value[..200];

    /// <summary>Response shape of POST /agent/ask.</summary>
    private sealed record AgentReplyDto(
        [property: JsonPropertyName("answer")] string? answer,
        [property: JsonPropertyName("sources")] string[]? sources,
        [property: JsonPropertyName("step_log")] AgentStepDto[]? step_log,
        [property: JsonPropertyName("mode")] string? mode,
        [property: JsonPropertyName("error")] string? error);

    private sealed record AgentStepDto(
        [property: JsonPropertyName("step")] string? step,
        [property: JsonPropertyName("detail")] string? detail);
}
