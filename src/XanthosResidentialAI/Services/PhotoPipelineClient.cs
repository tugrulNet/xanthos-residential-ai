using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using XanthosResidentialAI;

namespace XanthosResidentialAI.Services;

/// <summary>One batch of photo results plus the transparency summary shown
/// on the screen ("X of Y photos kept, discarded by reason").</summary>
public sealed record PhotoBatchResult(
    IReadOnlyList<PhotoAnalysisResult> Photos,
    PhotoTriageSummary Summary);

public interface IPhotoPipelineClient
{
    /// <summary>Pings GET /health on the Python pipeline.</summary>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);

    /// <summary>Analyses listing photos in order via POST /analyze-url.
    /// Phashes of earlier photos are passed along so duplicates inside one
    /// batch are detected. Throws on failure — callers must show the error
    /// instead of hanging; the demo then falls back to cached data.</summary>
    Task<PhotoBatchResult> AnalyzeUrlsAsync(IReadOnlyList<string> imageUrls, CancellationToken cancellationToken = default);
}

/// <summary>HTTP client for the Python photo pipeline (python/photo-pipeline).
/// The contract is verified against that service's code: GET /health and
/// POST /analyze-url with {"image_url", "known_hashes", "classify"}. Triage
/// stays in Python; this class only consumes results. SAM 3.1 fields
/// (features / issue flags) are added by the pipeline on 29–30 Oct and will
/// map into DetectedFeatures / IssueFlags.</summary>
public sealed class PhotoPipelineClient : IPhotoPipelineClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient http;

    public PhotoPipelineClient(HttpClient http) => this.http = http;

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await http.GetAsync("/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<PhotoBatchResult> AnalyzeUrlsAsync(
        IReadOnlyList<string> imageUrls, CancellationToken cancellationToken = default)
    {
        var results = new List<PhotoAnalysisResult>(imageUrls.Count);
        var knownHashes = new List<string>();

        for (var index = 0; index < imageUrls.Count; index++)
        {
            var url = imageUrls[index];
            var payload = new { image_url = url, known_hashes = knownHashes, classify = true };

            using var response = await http.PostAsJsonAsync(
                "/analyze-url", payload, JsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var dto = await response.Content.ReadFromJsonAsync<PipelinePhotoResult>(JsonOptions, cancellationToken)
                      ?? throw new InvalidOperationException(
                          $"Pipeline returned an empty result for photo {index}.");

            if (dto.Phash is not null)
            {
                knownHashes.Add(dto.Phash);
            }

            results.Add(ToModel(dto, url, index));
        }

        return new PhotoBatchResult(results, BuildSummary(results));
    }

    private static PhotoAnalysisResult ToModel(PipelinePhotoResult dto, string url, int index) => new(
        PhotoUrl: url,
        PhotoIndex: index,
        BlurScore: dto.BlurScore,
        IsBlurry: dto.IsBlurry,
        Phash: dto.Phash,
        DuplicateOf: dto.DuplicateOf,
        RoomType: dto.RoomType,
        RoomConfidence: dto.RoomConfidence,
        Kept: dto.Kept,
        DiscardReason: dto.DiscardReason,
        DetectedFeatures: Array.Empty<string>(), // SAM 3.1 lands 29–30 Oct
        IssueFlags: Array.Empty<string>());      // SAM 3.1 lands 29–30 Oct

    private static PhotoTriageSummary BuildSummary(IReadOnlyList<PhotoAnalysisResult> results)
    {
        var discarded = results.Where(r => !r.Kept).ToList();
        var byReason = discarded
            .GroupBy(r => r.DiscardReason?.Split(':')[0] ?? "unknown")
            .ToDictionary(g => g.Key, g => g.Count());

        return new PhotoTriageSummary(
            TotalPhotos: results.Count,
            KeptPhotos: results.Count - discarded.Count,
            DiscardedPhotos: discarded.Count,
            DiscardedByReason: byReason);
    }

    /// <summary>Response shape of POST /analyze-url (triage.py "analyze").</summary>
    private sealed record PipelinePhotoResult(
        [property: JsonPropertyName("blur_score")] double? BlurScore,
        [property: JsonPropertyName("is_blurry")] bool IsBlurry,
        [property: JsonPropertyName("phash")] string? Phash,
        [property: JsonPropertyName("duplicate_of")] string? DuplicateOf,
        [property: JsonPropertyName("room_type")] string? RoomType,
        [property: JsonPropertyName("room_confidence")] double? RoomConfidence,
        [property: JsonPropertyName("kept")] bool Kept,
        [property: JsonPropertyName("discard_reason")] string? DiscardReason);
}
