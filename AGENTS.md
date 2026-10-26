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
2. Buyer agent (LangGraph, Muse Spark, Meta Model API — see "Agent" below):
   explains the score, compares two neighbourhoods only from the data given.
   Missing data = say so, never invent it. No price predictions.
3. Photo analysis: triage discards blurry / duplicate / uninformative photos,
   SAM 3.1 segments the rest for features and issue flags (see "SAM" below).
   Always show the transparency note (e.g. how many photos were discarded and why).
4. One-page sourced summary, PDF export (QuestPDF).

## Agent — LangGraph (owner decision, 26 Oct 2026)

- The buyer assistant is a LangGraph agent, in Python, not in .NET.
  .NET only renders the agent's steps and final answer.
- Graph: `understand → get_score → list_photos/triage → sam_segment →
  synthesize → verify_sources → END`, with conditional routing: if the score
  or photos are missing, the graph says so instead of inventing them.
- Tools (Python functions, same names in code and UI):
  `get_residential_score`, `list_listing_photos`, `analyze_photos`,
  `compare_neighbourhoods`.
- State carries: address, score, photos, triage result, SAM results, messages,
  sources, and a step log. The step log is streamed/shown in the Blazor UI
  ("score fetched", "8 of 34 photos discarded", "SAM running") so a judge can
  see it is an agent, not a single chat call.
- Before building the LangGraph adapter, verify from the Meta Model API docs
  how Muse Spark handles tool calling. If Muse Spark is not directly usable
  as a LangGraph chat model, build a small adapter; if that takes more than
  one working day, fall back to a direct tool loop with the SAME tools, SAME
  state, and SAME UI. The fallback is still reported honestly as a tool loop,
  never presented as LangGraph.

## SAM — how it is implemented well (owner decision, 26 Oct 2026)

- SAM 3.1 is a promptable segmentation model: a short text prompt returns
  pixel masks for all matching instances in a photo. It is NOT a damage
  diagnosis model on its own.
- Three layers, in order:
  1. Triage first (blur / duplicate / uninformative) — discarded photos never
     reach SAM, saving credit and time.
  2. SAM, at most 5 key photos per listing, with a bounded prompt list
     (e.g. kitchen island, window, door, stain / discoloration, crack).
     Keep a confidence threshold (e.g. 0.5) and a confidence value per mask.
  3. Labelling layer: mask + room context (CLIP) + agent reasoning together
     produce a feature or issue flag. No flag without a mask; no mask without
     a visible overlay.
- UI: mask overlay on the photo with on/off toggle, each flag linked to its
  mask, mask confidence shown. If SAM is unavailable, the UI says "SAM
  unavailable" and shows triage + CLIP only — never a fake mask.
- Local SAM weights on this GPU-less server are too slow for live demo use,
  so SAM runs via the Meta Model API (credit expected ~28 Oct 2026). Before
  the credit lands, the core runs on triage + CLIP only.
- Cache SAM masks by photo hash so the same photo is not re-segmented on
  every demo run.

## Live demo (owner decision, 26 Oct 2026)

- Demo address: **https://xanthos.tugrul.net** (subdomain, to be opened).
  Deploy only after the core works locally — never deploy an empty scaffold.
- Server notes: Hestia, separate web domain for the subdomain, HTTPS, and a
  port that is NOT 5005 (5005 is the Chim app and is why tugrul.net currently
  shows the Chim site). The nginx proxy must pass WebSocket traffic, because
  Blazor Server needs it.
- Demo resilience: cached demo data is the fallback if the live database or
  the Meta API is down at demo time, and the UI labels cached data as cached.
  Secrets live on the server only, never in this repo (see security rule).

## New vs pre-existing (hackathon rule — this decides what the demo may claim)

- NEW (27 Oct – 4 Nov 2026, this repo): Blazor flow page, Muse Spark assistant,
  SAM 3.1 photo analysis, summary PDF.
- PRE-EXISTING, never claimed as new: the Xanthos residential score and the
  Calgary data layer behind it.
- OUT OF SCOPE: Smart Zoning, Business Match, Bylaw Chatbot, Area Analysis,
  auth / tenancy, pricing, admin panels, bulk DDF import.
- Stretch only if the core is done by 31 Oct: Muse Image staged-room image,
  always labelled "AI staging, not a real photo".

## Data source and security rule (owner decision, 26 Oct 2026 — binding)

- **Data source is the existing live database.** Score and listing data come
  from the live Xanthos `zoning` database, through the existing Xanthos score
  service where available. Do NOT add a DDF import, do NOT call the Realtor
  API, and do NOT use or store a Realtor API key in this project at all.
  This rule is about the hackathon project only; it does not change any
  separate Xanthos DDF import work.
- **No secrets in this public repo, ever.** This covers every API key,
  database connection string, database password, token, private key, and
  any other information that could create a security hole — not only the
  Meta Model API key. Such values live in environment / server secrets
  only, never in code, config files, docs, comments, screenshots, or commit
  messages in this repo. If a direct database read is ever needed locally,
  it uses an environment-only, read-only connection string, and it is
  never committed.
- **Secret check after every upload, without exception.** Before every push:
  review the staged diff and confirm no secret, connection string, password,
  `.env` file, or `appsettings.Development.json` is included. After every
  push: re-check the pushed files on GitHub (file list and diff) and confirm
  the same. Report the result of both checks in your end-of-step report
  ("pre-push check: clean / after-push check: clean", with the files checked).
  If a secret is found, stop, tell the owner immediately, and do not push
  again until it is removed and the owner confirms the next step.

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
- Open items from the owner: how the Blazor app reaches the live database
  (existing Xanthos score service contract: URL, method, auth, sample
  request/response) or "start with cached demo scores" approval;
  3 demo addresses + photo source; Meta API billing card status;
  LangGraph adapter verification (Muse Spark tool-calling format).
  Demo address is decided: xanthos.tugrul.net (see "Live demo").
  No DDF / Realtor work is an open item — it is out of scope by rule above.
- Day plan (revised, core first): 27 Oct Blazor + score; 28 Oct LangGraph
  agent; 29 Oct SAM; 30–31 Oct integration + demo deploy to xanthos.tugrul.net;
  1 Nov no work (move); 2 Nov stretch decision; 3 Nov video + submission text;
  4 Nov submit by Calgary noon (deadline 21:00 ET).
