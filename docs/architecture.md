# Architecture

## Dual-track (approved 26 Oct 2026)

- Foreground: .NET Blazor Web App, net10.0, Server interactive. Address form,
  score card, assistant chat UI, photo cards, summary PDF (QuestPDF).
- Background: Python FastAPI photo pipeline. Photo download, OpenCV blur,
  pHash duplicate, CLIP room type, SAM 3.1 segmentation, feature / issue flags.
- Communication: .NET <-> Python over localhost HTTP (`POST /analyze-url`
  and follow-ons). If Python fails, .NET does not hang; the job is marked
  failed and the demo falls back to cached results.
- Persistence for demo: photo analysis results live in their own area;
  existing `properties` records are never modified.

## Data contracts (to be confirmed against the live Xanthos score endpoint)

- Score input: a Calgary address string.
- Score output: overall score, livability score, investment score, components
  (schools, safety, transit, etc.), confidence per component, and sources.
  If the endpoint contract differs, the C# models in
  `src/XanthosResidentialAI/Models.cs` are updated to match it — no field
  is invented for the demo.
- Photo input: a list of listing photo URLs for one property.
- Photo output per photo: blur score, duplicate flag, room type + confidence,
  kept / discard reason, SAM segments, detected features, issue flags.

## Guardrails

- Assistant answers cite score components and sources; missing data is
  stated as missing.
- No price prediction, no legal (bylaw) answers, no medical or other
  out-of-scope advice.
- Demo data, if cached for demo stability, is labelled as cached in the
  README and in the demo video.
- See `docs/build-brief-2026-10-26.md` for the full build brief and day plan.
