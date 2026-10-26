"""Agent state — the same fields in code and in the Blazor UI (AGENTS.md).

State carries: address, score, photos, triage result, SAM results, messages,
sources, and a step log shown in the UI so a judge can see this is an agent.
"""
from operator import add as list_add
from typing import Annotated, Any, TypedDict


class AgentState(TypedDict, total=False):
    address: str
    question: str
    context: dict[str, Any]        # page data from .NET (score, photos, triage)
    score: dict[str, Any] | None
    photo_urls: list[str]
    triage: dict[str, Any] | None
    sam_results: list[dict[str, Any]]
    messages: list[dict[str, str]] # {"role", "content"}
    sources: list[str]
    step_log: Annotated[list[str], list_add]
    answer: str
    mode: str                      # "langgraph" | "rules-only"
    blocked: str | None            # guardrail refusal text, if any
