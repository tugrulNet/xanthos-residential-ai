# AGENTS.md — Xanthos Residential AI

Meta Global AI Developer Hackathon entry, Startup Track, theme: Build for Your World.
Work in this repo only. Read `README.md` and `docs/architecture.md` before changing code.

## Project in one sentence

Calgary home buyer: enter an address, understand its residential score in plain
language, and see what its listing photos actually show — on one screen.

## Architecture (dual-track, do not change without asking)

- Foreground: .NET Blazor Web App, **net10.0 only** (Server interactive).
  Never retarget to net8.0 / net9.0 — both leave support on 10 Nov 2026.
- Background: Python FastAPI photo pipeline in `python/photo-pipeline/`
  (already tested: OpenCV blur, pHash duplicate, CLIP room type).
- .NET calls Python over localhost HTTP. If Python fails, .NET must not hang;
  mark the job failed and fall back to cached demo data.
- The whole photo pipeline stays in Python (download → triage → SAM 3.1 →
  flags). .NET only consumes its result. Do not reimplement triage in C#.

## The one flow (nothing else)

1. Address → score card (livability + investment + components + confidence + sources).
2. Buyer assistant (Muse Spark, Meta Model API): explains the score, compares
   two neighbourhoods only from the data given. Missing data = say so, never
   invent it. No price predictions.
3. Photo analysis: triage discards blurry / duplicate / uninformative photos,
   SAM 3.1 segments the rest for features and issue flags. Always show the
   transparency note (e.g. how many photos were discarded and why).
4. One-page sourced summary, PDF export (QuestPDF).

## New vs pre-existing (hackathon rule — this decides what the demo may claim)

- NEW (27 Oct – 4 Nov 2026, this repo): Blazor flow page, Muse Spark assistant,
  SAM 3.1 photo analysis, summary PDF.
- PRE-EXISTING, never claimed as new: the Xanthos residential score and the
  Calgary data layer behind it.
- OUT OF SCOPE: Smart Zoning, Business Match, Bylaw Chatbot, Area Analysis,
  auth / tenancy, pricing, admin panels, bulk DDF import.
- Stretch only if the core is done by 31 Oct: Muse Image staged-room image,
  always labelled "AI staging, not a real photo".

## Hard rules

- No API keys, tokens, or secrets in this repo. Meta Model API key comes from
  environment / server secrets only; placeholders in code, nothing else.
- Do not invent Meta API endpoints, model names, URLs, or request/response
  shapes. Verify them against the Meta Model API docs first; if unverified,
  leave the client as a clearly marked TODO and say so in your report.
- Do not invent figures. Numbers in UI, README, or demo text come from the
  database / API response, or are labelled as cached demo data.
- Do not modify existing Xanthos data. Demo results live in their own area;
  never write to `properties` or other live tables.
- No legal (bylaw) answers, no price advice, no medical advice.
- One page, one flow: `/`. No extra modules, no auth pages, no pricing pages.

## Working rules

- Work on a feature branch, never push directly to `main`. The owner merges.
- Small, reviewable commits with clear messages. Do not rewrite history.
- After every change: `dotnet build` must pass. Report the exact build output,
  the files you changed, what is still TODO, and what you need from the owner
  (endpoint contract, demo addresses, API key) — do not silently guess those.
- If a required endpoint or credential is missing, implement against a
  labelled cached/demo fallback and list it as an open item. Never present a
  fallback as a live integration.

## Current state (26 Oct 2026)

- Repo has: README, docs, C# data models (`src/XanthosResidentialAI/Models.cs`),
  Python photo pipeline. The Blazor app itself is NOT yet generated.
- First job: `dotnet new blazor -o src/XanthosResidentialAI --framework net10.0`,
  then replace its home page with the one flow above.
- Open items from the owner: residential score endpoint contract (URL, method,
  auth, sample request/response) or "start with cached demo scores" approval;
  3 demo addresses + photo source; demo hosting address; Meta API billing card status.
