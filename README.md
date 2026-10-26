# Xanthos Residential AI

Meta Global AI Developer Hackathon entry — Startup Track, theme: **Build for Your World**.

One flow, for a Calgary home buyer: enter an address, see its residential score,
understand it in plain language, and see what its listing photos actually show.

## The flow

1. **Address → score** — the existing Xanthos residential score (livability +
   investment, with components such as schools, safety and transit), read-only.
2. **Buyer assistant (Muse Spark, Meta Model API)** — explains the score in
   plain language and compares two neighbourhoods, citing only the data it was
   given. If data is missing, it says so; it does not fill the gap. It does
   not give price predictions.
3. **Photo analysis (Python pipeline + SAM 3.1, Meta Model API)** — listing
   photos are first triaged (blurry / duplicate / uninformative), then the
   remaining photos are segmented for features and issue flags. The screen
   shows the transparency note, e.g. how many photos were discarded and why.
4. **Sourced one-page summary** — score + assistant summary + photo findings,
   exportable as PDF.

## What is new here, and what is not

- **Built 27 Oct – 4 Nov 2026 for this hackathon:** this repository's Blazor
  front end, the Muse Spark assistant, the SAM 3.1 photo analysis, and the
  summary output.
- **Pre-existing, not claimed as new:** the Xanthos residential score and the
  Calgary data layer it reads from. Those are inputs to this project, not
  hackathon work.
- **Deliberately out of scope:** Smart Zoning, Business Match, Bylaw Chatbot,
  Area Analysis, auth/tenancy, pricing, admin panels, and bulk DDF import.

## Architecture: dual-track

- **Foreground — .NET Blazor (net10.0):** everything the user sees. See
  `src/README.md`.
- **Background — Python:** everything the user does not see. See
  `python/photo-pipeline/README.md`. The Python pipeline is the tested
  photo-triage code (OpenCV blur + pHash duplicate + CLIP room type), with
  SAM 3.1 segmentation added in the same pipeline. .NET calls it over
  localhost HTTP; it does not reimplement triage in C#.

## Status

- Day 1 scaffold (26 Oct 2026): repository structure, this README, the
  Python photo pipeline, and the C# data models. The Blazor UI, the
  Muse Spark client, and the SAM 3.1 integration are not yet wired up —
  they are the 27–30 Oct work. Nothing here is presented as finished.
- No API keys are in this repository. The Meta Model API key lives in
  environment / server secrets only.
- Figures in any submission text must be re-verified from the database
  before use; do not copy older counts from memory into this repo.

## Running (when the Blazor app lands)

- .NET: `dotnet run --project src/XanthosResidentialAI` (net10.0 SDK required)
- Python pipeline: see `python/photo-pipeline/README.md`
