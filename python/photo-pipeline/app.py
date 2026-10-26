"""Foto ön eleme mikroservisi — FastAPI.

Endpoints:
  GET  /health            -> servis durumu + CLIP yüklü mü
  POST /analyze           -> {"image_url": "..."} veya multipart file
                             + opsiyonel {"known_hashes": [...], "classify": true}
"""
import httpx
from fastapi import FastAPI, UploadFile, File, HTTPException
from pydantic import BaseModel

from triage import TriageEngine

app = FastAPI(title="photo-triage-service", version="1.0.0")
engine = TriageEngine()

MAX_BYTES = 15 * 1024 * 1024  # 15 MB


class AnalyzeRequest(BaseModel):
    image_url: str | None = None
    known_hashes: list[str] | None = None
    classify: bool = True


async def _fetch(url: str) -> bytes:
    try:
        async with httpx.AsyncClient(timeout=20.0, follow_redirects=True) as c:
            r = await c.get(url)
            r.raise_for_status()
            data = r.content
    except Exception as e:
        raise HTTPException(status_code=422, detail=f"indirilemedi: {e}"[:300])
    if len(data) > MAX_BYTES:
        raise HTTPException(status_code=413, detail="dosya çok büyük")
    ct = r.headers.get("content-type", "")
    if not ct.startswith("image/"):
        raise HTTPException(status_code=422, detail=f"görsel değil: {ct}")
    return data


@app.get("/health")
def health():
    return {"status": "ok", "clip_loaded": engine.clip_loaded,
            "clip_model": engine.clip_model_id}


@app.post("/analyze-url")
async def analyze_url(req: AnalyzeRequest):
    if not req.image_url:
        raise HTTPException(status_code=400, detail="image_url gerekli")
    data = await _fetch(req.image_url)
    return engine.analyze(data, known_hashes=req.known_hashes or [],
                          classify=req.classify)


@app.post("/analyze-upload")
async def analyze_upload(file: UploadFile = File(...), classify: bool = True):
    data = await file.read()
    if len(data) > MAX_BYTES:
        raise HTTPException(status_code=413, detail="dosya çok büyük")
    return engine.analyze(data, classify=classify)


@app.post("/warmup")
def warmup():
    """CLIP modelini ilk isteğe bırakmadan yükler (deploy sonrası çağrılır)."""
    engine._ensure_clip()
    return {"status": "ok", "clip_loaded": True}
