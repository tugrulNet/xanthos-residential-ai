// Xanthos Residential AI — data models (Day 1 scaffold).
// These records describe the contracts between the .NET front end, the
// Xanthos score source, the Muse Spark assistant, and the Python photo
// pipeline. Field names follow the build brief; they must be updated to
// match the live score endpoint contract before the demo is recorded.

namespace XanthosResidentialAI;

/// <summary>Read-only residential score for one address. Pre-existing
/// Xanthos logic produces this; this project does not claim it as new.</summary>
public sealed record ResidentialScore(
    string Address,
    double Overall,
    double Livability,
    double Investment,
    IReadOnlyList<ScoreComponent> Components,
    IReadOnlyList<string> Sources
);

public sealed record ScoreComponent(
    string Name,
    double Value,
    double Confidence
);

/// <summary>One assistant message in the buyer-assistant chat.</summary>
public sealed record AssistantMessage(
    string Role,
    string Text,
    IReadOnlyList<string> CitedSources
);

/// <summary>Result for one listing photo, from the Python pipeline.</summary>
public sealed record PhotoAnalysisResult(
    string PhotoUrl,
    int PhotoIndex,
    double? BlurScore,
    bool IsBlurry,
    string? Phash,
    string? DuplicateOf,
    string? RoomType,
    double? RoomConfidence,
    bool Kept,
    string? DiscardReason,
    IReadOnlyList<string> DetectedFeatures,
    IReadOnlyList<string> IssueFlags
);

/// <summary>Transparency summary shown on the listing screen.</summary>
public sealed record PhotoTriageSummary(
    int TotalPhotos,
    int KeptPhotos,
    int DiscardedPhotos,
    IReadOnlyDictionary<string, int> DiscardedByReason
);

/// <summary>One step of the buyer agent, shown in the UI so a judge can see
/// it is an agent working, not a single chat call.</summary>
public sealed record AgentStep(
    string Step,
    string Detail
);

/// <summary>Reply from the Python buyer agent (LangGraph, python/buyer-agent).
/// Mode is "langgraph" (Muse Spark via Meta Model API), "rules-only"
/// (deterministic fallback over the same tools and state — labelled on
/// screen), or "unavailable" (service unreachable; page keeps cached data).</summary>
public sealed record AgentReply(
    string Answer,
    IReadOnlyList<string> Sources,
    IReadOnlyList<AgentStep> Steps,
    string Mode,
    string? Error
);
