using System.Text;
using XanthosResidentialAI;

namespace XanthosResidentialAI.Services;

public interface ISummaryService
{
    /// <summary>The exact data the assistant is allowed to see — nothing
    /// beyond what the page shows. Guardrail input for MetaModelClient.</summary>
    string BuildAssistantContext(
        ScoreResult score,
        IReadOnlyList<PhotoAnalysisResult> photos,
        PhotoTriageSummary? triage);

    /// <summary>One-page sourced summary. PDF export via QuestPDF is TODO
    /// (31 Oct step); until then this text is the preview of the PDF.</summary>
    string BuildTextSummary(
        ScoreResult score,
        IReadOnlyList<AssistantMessage> messages,
        IReadOnlyList<PhotoAnalysisResult> photos,
        PhotoTriageSummary? triage);
}

public sealed class SummaryService : ISummaryService
{
    public string BuildAssistantContext(
        ScoreResult score,
        IReadOnlyList<PhotoAnalysisResult> photos,
        PhotoTriageSummary? triage)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Address: {score.Score.Address}");
        sb.AppendLine($"Data source: {score.SourceLabel}");
        sb.AppendLine($"Overall: {score.Score.Overall:F1} | Livability: {score.Score.Livability:F1} | Investment: {score.Score.Investment:F1}");
        sb.AppendLine("Components (value, confidence):");
        foreach (var component in score.Score.Components)
        {
            sb.AppendLine($"- {component.Name}: {component.Value:F1} (confidence {component.Confidence:P0})");
        }
        sb.AppendLine($"Sources: {string.Join("; ", score.Score.Sources)}");

        if (triage is not null)
        {
            sb.AppendLine($"Photos: {triage.KeptPhotos} of {triage.TotalPhotos} kept after triage.");
            foreach (var photo in photos.Where(p => p.Kept))
            {
                sb.AppendLine($"- photo {photo.PhotoIndex}: {photo.RoomType ?? "type n/a"}");
            }
        }
        else
        {
            sb.AppendLine("Photos: no photo analysis data available in this session.");
        }

        sb.AppendLine(
            "Rules: cite only the data above; state missing data as missing; " +
            "never invent figures; no price predictions; no legal (bylaw) advice.");
        return sb.ToString();
    }

    public string BuildTextSummary(
        ScoreResult score,
        IReadOnlyList<AssistantMessage> messages,
        IReadOnlyList<PhotoAnalysisResult> photos,
        PhotoTriageSummary? triage)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Xanthos Residential AI — summary for {score.Score.Address}");
        sb.AppendLine($"Data source: {score.SourceLabel}");
        sb.AppendLine();

        sb.AppendLine("Score");
        sb.AppendLine($"  Overall {score.Score.Overall:F1} | Livability {score.Score.Livability:F1} | Investment {score.Score.Investment:F1}");
        foreach (var component in score.Score.Components)
        {
            sb.AppendLine($"  - {component.Name}: {component.Value:F1} (confidence {component.Confidence:P0})");
        }
        sb.AppendLine($"  Sources: {string.Join("; ", score.Score.Sources)}");
        sb.AppendLine();

        sb.AppendLine("Photos");
        if (triage is not null)
        {
            var reasons = string.Join(", ", triage.DiscardedByReason.Select(kv => $"{kv.Key}: {kv.Value}"));
            sb.AppendLine($"  {triage.TotalPhotos} analyzed, {triage.KeptPhotos} kept, {triage.DiscardedPhotos} discarded ({reasons})");
            foreach (var photo in photos.Where(p => p.Kept))
            {
                sb.AppendLine($"  - photo {photo.PhotoIndex}: {photo.RoomType ?? "type n/a"}, blur {photo.BlurScore?.ToString("F1") ?? "n/a"}");
            }
        }
        else
        {
            sb.AppendLine("  No photo analysis ran in this session.");
        }
        sb.AppendLine();

        if (messages.Count > 0)
        {
            sb.AppendLine("Assistant highlights");
            foreach (var message in messages.Where(m => m.Role == "assistant").TakeLast(3))
            {
                sb.AppendLine($"  - {message.Text}");
            }
        }

        // TODO (31 Oct): render this as a one-page PDF via QuestPDF.
        return sb.ToString();
    }
}
