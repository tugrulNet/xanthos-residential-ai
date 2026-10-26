"""Buyer-agent tools. Names must stay identical in code and in the UI.

Data rules (AGENTS.md):
- The score/listing data source is the existing live Xanthos data. The live
  score endpoint contract is still an open item, so today these tools read
  the page context that .NET passes in (already labelled as demo cache).
- No figures are invented. Missing data returns None and the graph says so.
- Photos are analysed by the existing Python pipeline over localhost HTTP
  (python/photo-pipeline); triage is never reimplemented here.
"""
import os
from typing import Any

import httpx

PIPELINE_BASE_URL = os.environ.get("PHOTO_PIPELINE_BASE_URL", "http://localhost:8000")
AGENT_MAX_PHOTOS = int(os.environ.get("AGENT_MAX_PHOTOS", "8"))
PIPELINE_TIMEOUT_S = float(os.environ.get("PHOTO_PIPELINE_TIMEOUT_S", "120"))

# Per-request page context, set by the engine before the graph runs.
# (Single-request demo service; a contextvar would be needed for concurrency.)
_CONTEXT: dict[str, Any] = {}


def set_context(context: dict[str, Any] | None) -> None:
    _CONTEXT.clear()
    if context:
        _CONTEXT.update(context)


def get_residential_score(address: str) -> dict[str, Any] | None:
    """Residential score for a Calgary address (livability, investment,
    components, confidence, sources). Returns None when there is no data.

    TODO (open item, owner): when the live Xanthos score service contract
    arrives (URL, method, auth, sample request/response), call it here.
    Until then the score comes from the page context .NET passed in, which
    is labelled as cached demo data."""
    wanted = (address or "").strip().lower()
    score = _CONTEXT.get("score")
    if not score or not wanted:
        return None
    if str(score.get("address", "")).strip().lower() != wanted:
        return None
    return score


def list_listing_photos(address: str) -> list[str] | None:
    """Listing photo URLs for a property address. Returns None when the
    listing has no photo data in this session."""
    wanted = (address or "").strip().lower()
    ctx_address = str(_CONTEXT.get("address", "")).strip().lower()
    if wanted and ctx_address and wanted != ctx_address:
        return None
    urls = _CONTEXT.get("photo_urls") or []
    return list(urls) if urls else None


def analyze_photos(image_urls: list[str]) -> dict[str, Any]:
    """Run the local photo pipeline (triage: blur / duplicate / uninformative)
    on up to AGENT_MAX_PHOTOS listing photos. Returns per-photo results and a
    transparency summary. Errors are returned, never invented away."""
    urls = [u for u in (image_urls or []) if str(u).startswith("http")][:AGENT_MAX_PHOTOS]
    if not urls:
        return {"error": "no valid photo URLs given"}

    results: list[dict[str, Any]] = []
    known_hashes: list[str] = []
    for index, url in enumerate(urls):
        try:
            with httpx.Client(timeout=PIPELINE_TIMEOUT_S) as client:
                response = client.post(
                    f"{PIPELINE_BASE_URL}/analyze-url",
                    json={"image_url": url, "known_hashes": known_hashes, "classify": True},
                )
                response.raise_for_status()
                data = response.json()
        except Exception as exc:  # pipeline down or photo unreachable
            return {
                "error": f"photo pipeline call failed for photo {index}: {exc}"[:300],
                "partial_results": results,
            }
        if data.get("phash"):
            known_hash = data["phash"]
            known_hashes.append(known_hash)
        results.append({
            "photo_index": index,
            "kept": bool(data.get("kept")),
            "discard_reason": data.get("discard_reason"),
            "room_type": data.get("room_type"),
            "room_confidence": data.get("room_confidence"),
            "blur_score": data.get("blur_score"),
        })

    discarded = [r for r in results if not r["kept"]]
    by_reason: dict[str, int] = {}
    for item in discarded:
        reason = str(item.get("discard_reason") or "unknown").split(":")[0]
        by_reason[reason] = by_reason.get(reason, 0) + 1

    return {
        "total": len(results),
        "kept": len(results) - len(discarded),
        "discarded": len(discarded),
        "discarded_by_reason": by_reason,
        "results": results,
    }


def compare_neighbourhoods(address_a: str, address_b: str) -> dict[str, Any]:
    """Compare two Calgary addresses using only available score data.
    Components missing on either side are listed as missing — nothing is
    invented, and no price estimate is ever produced."""
    score_a = get_residential_score(address_a)
    score_b = get_residential_score(address_b)

    comparison: dict[str, Any] = {
        "address_a": address_a,
        "address_b": address_b,
        "score_a": score_a,
        "score_b": score_b,
        "missing": [],
    }
    if score_a is None:
        comparison["missing"].append(f"no score data for {address_a}")
    if score_b is None:
        comparison["missing"].append(f"no score data for {address_b}")
    if score_a is None or score_b is None:
        return comparison

    components_a = {c["name"]: c for c in score_a.get("components", [])}
    components_b = {c["name"]: c for c in score_b.get("components", [])}
    rows = []
    for name in sorted(set(components_a) | set(components_b)):
        if name in components_a and name in components_b:
            rows.append({
                "component": name,
                "a": components_a[name]["value"],
                "b": components_b[name]["value"],
                "winner": address_a if components_a[name]["value"] > components_b[name]["value"]
                          else address_b if components_b[name]["value"] > components_a[name]["value"]
                          else "tie",
            })
        else:
            missing_side = address_b if name in components_a else address_a
            comparison["missing"].append(f"component '{name}' missing for {missing_side}")
    comparison["components"] = rows
    return comparison


# Same names in code and UI.
TOOLS = [get_residential_score, list_listing_photos, analyze_photos, compare_neighbourhoods]
TOOL_BY_NAME = {fn.__name__: fn for fn in TOOLS}
