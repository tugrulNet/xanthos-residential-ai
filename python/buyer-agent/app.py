"""Buyer agent servisi — FastAPI (LangGraph ajanı, Muse Spark).

Endpoints:
  GET  /health   -> servis durumu + hangi modda çalışıyor (llm / rules-only)
  POST /agent/ask -> {address, question, context} -> {answer, sources,
                    step_log, mode}
Port 8001 (foto pipeline 8000'de çalışır). Anahtar yalnızca ortamdan:
MODEL_API_KEY — repoda asla tutulmaz.
"""
import os
from typing import Any

import muse_spark
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field

app = FastAPI(title="xanthos-buyer-agent", version="0.1.0")


class AskRequest(BaseModel):
    address: str = Field(min_length=1, max_length=200)
    question: str = Field(min_length=1, max_length=2000)
    context: dict[str, Any] | None = None


@app.get("/health")
def health() -> dict[str, Any]:
    return {
        "status": "ok",
        "mode": "langgraph" if muse_spark.is_configured() else "rules-only",
        "model": muse_spark.MODEL,
        "api_key_configured": muse_spark.is_configured(),
    }


@app.post("/agent/ask")
def ask(req: AskRequest) -> dict[str, Any]:
    import agent  # deferred import so /health stays fast

    try:
        result = agent.run(req.address.strip(), req.question.strip(), req.context)
    except Exception as exc:  # graph failure is reported, never invented away
        raise HTTPException(status_code=500, detail=f"agent run failed: {exc}"[:300])
    return result
