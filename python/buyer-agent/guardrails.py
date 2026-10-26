"""Guardrails for the buyer agent (AGENTS.md):
- no price predictions or price advice
- no legal (bylaw) answers, no medical advice
- missing data is stated as missing; nothing is invented
Input side: blocked topics get a fixed refusal. Output side: verify_sources
appends the sources actually used and lists data gaps.
"""
import re

PRICE_RE = re.compile(
    r"\b(price|pricing|priced|valuation|appraisal|market\s+value|asking\s+price|"
    r"will\s+it\s+appreciate|invest\s+return|roi|rent(al)?\s+rate|how\s+much\s+(is|are)\s+it\s+worth)\b",
    re.IGNORECASE)
LEGAL_RE = re.compile(
    r"\b(bylaw|by-laws?|zoning\s+(regulation|law|legal)|legal\s+advice|lawsuit|sue|"
    r"tenant\s+law|landlord|lease\s+dispute|permit\s+legal|building\s+code\s+violation)\b",
    re.IGNORECASE)
MEDICAL_RE = re.compile(r"\b(medical|health\s+advice|mould?\s+(health|advice)|toxic)\b", re.IGNORECASE)

REFUSAL = (
    "I can't give price predictions, price estimates, or legal (bylaw) or "
    "medical advice — that is out of scope for this tool. I can explain the "
    "residential score components and what the listing photos actually show."
)


def blocked_topic(question: str) -> str | None:
    """Returns a fixed refusal message when the question is out of scope."""
    for pattern in (PRICE_RE, LEGAL_RE, MEDICAL_RE):
        if pattern.search(question or ""):
            return REFUSAL
    return None


def verify_sources(answer: str, sources: list[str], missing: list[str]) -> tuple[str, list[str]]:
    """Output guardrail: append the sources actually used and list data gaps,
    so the answer is traceable and never silently invents data."""
    lines = [answer.rstrip()]
    if missing:
        lines.append("")
        lines.append("Data gaps: " + "; ".join(missing) + ".")
    lines.append("")
    lines.append("Sources: " + ("; ".join(sources) if sources else "none available"))
    return "\n".join(lines), sources
