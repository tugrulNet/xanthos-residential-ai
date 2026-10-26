# photo-triage-service

DDF ilan fotoğrafları için **ön eleme mikroservisi** (Hugging Face açık modeller).
Ücretli API yok: bulanık tespiti (OpenCV) + tekrar tespiti (pHash) + oda sınıflandırma (CLIP ViT-B/32 zero-shot).
Docker yok — lokalde ve sunucuda doğrudan venv ile çalışır.

## Lokal kurulum

```bash
python3 -m venv .venv && source .venv/bin/activate
pip install -r requirements.txt
pip install torch==2.5.1 --index-url https://download.pytorch.org/whl/cpu

# CLIP modelini önceden indir (~350MB; yapılmazsa ilk sınıflandırmada otomatik iner)
python -c "from triage import TriageEngine; TriageEngine()._ensure_clip()"

uvicorn app:app --host 127.0.0.1 --port 8000
```

## Sunucu kurulumu

```bash
# dosyaları kopyala
cp -r photo-triage-service /home/tams/web/zoning.tugrul.net/apps/
cd /home/tams/web/zoning.tugrul.net/apps/photo-triage-service
python3 -m venv .venv && source .venv/bin/activate
pip install -r requirements.txt
pip install torch==2.5.1 --index-url https://download.pytorch.org/whl/cpu
python -c "from triage import TriageEngine; TriageEngine()._ensure_clip()"

# systemd servisi
sudo cp photo-triage.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now photo-triage
curl localhost:8000/health
```

## API

- `GET /health` → `{"status":"ok","clip_loaded":false,...}`
- `POST /warmup` → CLIP'i yükler
- `POST /analyze-url` → `{"image_url":"https://...","known_hashes":[...],"classify":true}`
- `POST /analyze-upload` → multipart `file`

### Örnek cevap

```json
{
  "blur_score": 245.7, "is_blurry": false,
  "phash": "a3f29c1e5b7d42aa", "duplicate_of": null,
  "room_type": "kitchen", "room_confidence": 0.87,
  "informative_label": "kitchen", "is_informative": true,
  "kept": true, "discard_reason": null,
  "processing_ms": 180,
  "model_versions": {"opencv": "4.10.0", "clip": "openai/clip-vit-base-patch32"}
}
```

`discard_reason`: `blurry` | `duplicate` | `uninformative:<etiket>` | `undecodable`

## .NET tarafıyla kullanım

Worker (`BackgroundService`), kuyruktaki her fotoğraf için `POST /analyze-url`
çağrır; dönen JSON'u `photo_analyses` tablosuna yazar. `known_hashes` =
aynı mülkün daha önce işlenen fotoğraflarının pHash listesi (tekrar yakalamak için).

## Test

```bash
source .venv/bin/activate
python test_triage.py   # CLIP indirmeden çalışır (çekirdek mantık)
```
