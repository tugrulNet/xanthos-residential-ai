"""Foto ön eleme çekirdek mantığı.

Aşama 1 (ucuz, CPU):
  1. Bulanık mı?      -> OpenCV Laplacian varyansı
  2. Tekrar mı?        -> perceptual hash (pHash)
  3. Ne fotoğrafı?     -> CLIP ViT-B/32 zero-shot (lazy yüklenir)

Hiçbir adımda ücretli API kullanılmaz.
"""
import time

import cv2
import numpy as np
from PIL import Image
import imagehash

BLUR_THRESHOLD = 100.0   # Laplacian varyansı bunun altındaysa bulanık
PHASH_SIZE = 8
DUPLICATE_HAMMING = 5    # pHash Hamming mesafesi <= bu ise tekrar

# CLIP zero-shot promptları (İngilizce — model İngilizce eğitildi)
PROMPTS = {
    "kitchen": "a photo of a kitchen",
    "bedroom": "a photo of a bedroom",
    "bathroom": "a photo of a bathroom",
    "living": "a photo of a living room",
    "dining": "a photo of a dining room",
    "exterior": "a photo of a house exterior",
    "building": "a photo of an apartment building exterior",
    "map": "a map",
    "floorplan": "a floor plan drawing",
    "logo": "a company logo",
    "person": "a photo of a person",
    "document": "a photo of a document with text",
}
# Bilgi vermeyen etiketler -> elenir
UNINFORMATIVE = {"map", "floorplan", "logo", "person", "document"}

DISCARD_BLURRY = "blurry"
DISCARD_DUPLICATE = "duplicate"


class TriageEngine:
    def __init__(self, clip_model_id="openai/clip-vit-base-patch32",
                 blur_threshold=BLUR_THRESHOLD):
        self.clip_model_id = clip_model_id
        self.blur_threshold = blur_threshold
        self._clip = None  # lazy

    # ---- CLIP (ilk kullanımda yüklenir) ----
    def _ensure_clip(self):
        if self._clip is None:
            from transformers import CLIPProcessor, CLIPModel
            import torch
            model = CLIPModel.from_pretrained(self.clip_model_id)
            processor = CLIPProcessor.from_pretrained(self.clip_model_id)
            model.eval()
            self._clip = (model, processor, torch)
        return self._clip

    @property
    def clip_loaded(self):
        return self._clip is not None

    # ---- Adım 1: bulanıklık ----
    @staticmethod
    def blur_score(img_bgr: np.ndarray) -> float:
        gray = cv2.cvtColor(img_bgr, cv2.COLOR_BGR2GRAY)
        return float(cv2.Laplacian(gray, cv2.CV_64F).var())

    # ---- Adım 2: pHash ----
    @staticmethod
    def phash(img_pil: Image.Image) -> str:
        return str(imagehash.phash(img_pil, hash_size=PHASH_SIZE))

    @staticmethod
    def is_duplicate(phash_hex: str, known_hashes: list[str]) -> str | None:
        """known_hashes içinde eşleşen varsa o hash'i döndürür."""
        try:
            h1 = imagehash.hex_to_hash(phash_hex)
        except Exception:
            return None
        for kh in known_hashes:
            try:
                if h1 - imagehash.hex_to_hash(kh) <= DUPLICATE_HAMMING:
                    return kh
            except Exception:
                continue
        return None

    # ---- Adım 3: CLIP zero-shot ----
    def classify(self, img_pil: Image.Image) -> tuple[str, float]:
        model, processor, torch = self._ensure_clip()
        labels = list(PROMPTS.keys())
        texts = [PROMPTS[l] for l in labels]
        inputs = processor(text=texts, images=img_pil, return_tensors="pt",
                           padding=True)
        with torch.no_grad():
            out = model(**inputs)
        probs = out.logits_per_image.softmax(dim=1)[0]
        best = int(probs.argmax())
        return labels[best], float(probs[best])

    # ---- Tam analiz ----
    def analyze(self, image_bytes: bytes,
                known_hashes: list[str] | None = None,
                classify: bool = True) -> dict:
        t0 = time.time()
        result = {
            "blur_score": None, "is_blurry": False,
            "phash": None, "duplicate_of": None,
            "room_type": None, "room_confidence": None,
            "informative_label": None, "is_informative": True,
            "kept": True, "discard_reason": None,
            "processing_ms": None,
            "model_versions": {
                "opencv": cv2.__version__,
                "clip": self.clip_model_id if classify else None,
            },
        }

        # decode
        arr = np.frombuffer(image_bytes, np.uint8)
        img_bgr = cv2.imdecode(arr, cv2.IMREAD_COLOR)
        if img_bgr is None:
            result["kept"] = False
            result["discard_reason"] = "undecodable"
            result["processing_ms"] = int((time.time() - t0) * 1000)
            return result
        img_pil = Image.fromarray(cv2.cvtColor(img_bgr, cv2.COLOR_BGR2RGB))

        # 1) bulanık
        score = self.blur_score(img_bgr)
        result["blur_score"] = round(score, 2)
        result["is_blurry"] = bool(score < self.blur_threshold)

        # 2) tekrar
        ph = self.phash(img_pil)
        result["phash"] = ph
        if known_hashes:
            dup = self.is_duplicate(ph, known_hashes)
            result["duplicate_of"] = dup

        # 3) sınıflandır
        if classify:
            try:
                label, conf = self.classify(img_pil)
                result["room_type"] = label
                result["room_confidence"] = round(conf, 4)
                result["informative_label"] = label
                result["is_informative"] = label not in UNINFORMATIVE
            except Exception as e:  # model yoksa eleme yine çalışsın
                result["model_versions"]["clip_error"] = str(e)[:200]

        # karar
        if result["is_blurry"]:
            result["kept"] = False
            result["discard_reason"] = DISCARD_BLURRY
        elif result["duplicate_of"]:
            result["kept"] = False
            result["discard_reason"] = DISCARD_DUPLICATE
        elif not result["is_informative"]:
            result["kept"] = False
            result["discard_reason"] = f"uninformative:{result['informative_label']}"

        result["processing_ms"] = int((time.time() - t0) * 1000)
        return result
