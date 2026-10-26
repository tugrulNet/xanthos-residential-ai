"""Muse Spark adapter (Meta Model API).

Endpoint, model and auth verified against the official Meta Model API docs
(developer.meta.ai, checked 28 Oct 2026):
  - base URL:  https://api.meta.ai/v1
  - auth:      Authorization: Bearer $MODEL_API_KEY
  - model:     muse-spark-1.3 (standard tier, recommended for new work)
  - tool calling: OpenAI function-calling format; the API is drop-in
    compatible with the OpenAI SDK, so ChatOpenAI with the verified base URL
    is the whole adapter (no custom protocol code).

The API key is read from the environment only and never enters this repo.
When the key is missing, the agent runs in rules-only mode and the UI says
so — it never pretends to be an LLM answer.
"""
import os

BASE_URL = "https://api.meta.ai/v1"
MODEL = os.environ.get("MUSE_SPARK_MODEL", "muse-spark-1.3")
API_KEY_ENV = "MODEL_API_KEY"


def api_key() -> str | None:
    return os.environ.get(API_KEY_ENV) or None


def is_configured() -> bool:
    return api_key() is not None


def chat_model():
    """OpenAI-compatible adapter for Muse Spark. Import is deferred so the
    service still starts when langchain-openai is not installed."""
    from langchain_openai import ChatOpenAI

    return ChatOpenAI(
        model=MODEL,
        base_url=BASE_URL,
        api_key=api_key(),
        temperature=0.2,
        timeout=60,
        max_retries=1,
    )
