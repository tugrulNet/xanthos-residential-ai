using XanthosResidentialAI;

namespace XanthosResidentialAI.Services;

/// <summary>A residential score plus where it came from. The label is always
/// shown on screen so cached demo data is never mistaken for live data.</summary>
public sealed record ScoreResult(
    ResidentialScore Score,
    string SourceLabel,
    bool IsDemoCache);

public interface IXanthosScoreClient
{
    /// <summary>Returns null when the address has no data — callers must show
    /// a "no data" state instead of inventing figures.</summary>
    Task<ScoreResult?> GetScoreAsync(string address, CancellationToken cancellationToken = default);
}

/// <summary>Read-only access to the Xanthos residential score.
///
/// TODO (open item, owner): the live score service contract is not known yet
/// (URL, method, auth, sample request/response). Until it is verified, this
/// client serves ONLY the three owner-approved demo addresses from an
/// in-memory cache, always labelled as cached demo data. When the contract
/// arrives, replace GetScoreAsync with the real HTTP call and update
/// Models.cs to match the live response — no field is invented for the demo.</summary>
public sealed class XanthosScoreClient : IXanthosScoreClient
{
    private const string DemoLabel = "DEMO CACHE — unverified sample data";

    // Owner-approved demo addresses (26 Oct). Values are placeholders; every
    // screen that shows them must also show DemoLabel. These are NOT verified
    // figures and must never be presented as live output.
    private static readonly Dictionary<string, ResidentialScore> DemoCache =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["3625 Shaganappi Trail NW"] = new(
                "3625 Shaganappi Trail NW",
                Overall: 72.0,
                Livability: 74.0,
                Investment: 69.0,
                Components: new[]
                {
                    new ScoreComponent("Schools", 78.0, 0.80),
                    new ScoreComponent("Safety", 70.0, 0.75),
                    new ScoreComponent("Transit", 66.0, 0.70),
                    new ScoreComponent("Parks", 74.0, 0.72),
                    new ScoreComponent("Daily errands", 71.0, 0.68),
                },
                Sources: new[] { "demo cache (placeholder source list)" }),

            ["1134 Kensington Road NW"] = new(
                "1134 Kensington Road NW",
                Overall: 81.0,
                Livability: 84.0,
                Investment: 76.0,
                Components: new[]
                {
                    new ScoreComponent("Schools", 76.0, 0.78),
                    new ScoreComponent("Safety", 74.0, 0.72),
                    new ScoreComponent("Transit", 82.0, 0.84),
                    new ScoreComponent("Parks", 68.0, 0.70),
                    new ScoreComponent("Daily errands", 86.0, 0.81),
                },
                Sources: new[] { "demo cache (placeholder source list)" }),

            ["250 Shawnessy Drive SW"] = new(
                "250 Shawnessy Drive SW",
                Overall: 64.0,
                Livability: 66.0,
                Investment: 61.0,
                Components: new[]
                {
                    new ScoreComponent("Schools", 65.0, 0.71),
                    new ScoreComponent("Safety", 62.0, 0.69),
                    new ScoreComponent("Transit", 58.0, 0.66),
                    new ScoreComponent("Parks", 70.0, 0.73),
                    new ScoreComponent("Daily errands", 64.0, 0.70),
                },
                Sources: new[] { "demo cache (placeholder source list)" }),
        };

    public Task<ScoreResult?> GetScoreAsync(string address, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(address);

        if (DemoCache.TryGetValue(address.Trim(), out var score))
        {
            ScoreResult result = new(score, DemoLabel, IsDemoCache: true);
            return Task.FromResult<ScoreResult?>(result);
        }

        // Live endpoint is not wired yet — unknown addresses have no data.
        return Task.FromResult<ScoreResult?>(null);
    }
}
