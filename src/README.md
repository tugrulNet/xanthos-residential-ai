# src — .NET Blazor front end

Target: **net10.0**. .NET 8 and .NET 9 leave support on 10 November 2026;
do not retarget this project to them.

## Day 1 state

Only the data models are here (`XanthosResidentialAI/Models.cs`). The Blazor
app itself is the 27 Oct scaffold step, to be generated with the .NET 10 SDK:

```bash
dotnet new blazor -o src/XanthosResidentialAI --framework net10.0
```

Then replace its home page with the single flow: address → score card →
assistant chat → photo cards → summary. Keep the app to that one page; do
not add auth, pricing, or the other Xanthos modules.

## Services to add (27–30 Oct)

- `XanthosScoreClient` — read-only residential score, from the live endpoint
  or, if that contract is not yet available, from cached demo scores
  labelled as such.
- `MetaModelClient` — Muse Spark assistant calls. Endpoint, model name and
  request/response shape must be verified against the Meta Model API docs
  before coding; do not invent URLs.
- `PhotoPipelineClient` — HTTP client for `python/photo-pipeline`.
- `SummaryService` — one-page sourced summary, PDF via QuestPDF.

## Secrets

The Meta Model API key is read from environment variables only. Never commit
it, and never put it in `appsettings.json` in this public repository.
