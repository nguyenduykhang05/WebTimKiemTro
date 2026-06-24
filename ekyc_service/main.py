from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
import httpx
import io
import base64
from PIL import Image
import numpy as np

app = FastAPI(title="SmartRoomFinder eKYC AI Service", version="1.0.0")


# ----------------------------------------------------------------
# Request/Response models
# ----------------------------------------------------------------
class OcrRequest(BaseModel):
    image_url: str


class OcrResponse(BaseModel):
    identity_card_number: str
    full_name: str
    date_of_birth: str
    raw_text: str


class FaceMatchRequest(BaseModel):
    cccd_image_url: str
    selfie_image_url: str


class FaceMatchResponse(BaseModel):
    match_score: float
    is_match: bool
    message: str


# ----------------------------------------------------------------
# Helper: Download image from URL → PIL Image
# ----------------------------------------------------------------
async def download_image(url: str) -> Image.Image:
    async with httpx.AsyncClient(timeout=15.0) as client:
        response = await client.get(url)
        if response.status_code != 200:
            raise HTTPException(status_code=400, detail=f"Không tải được ảnh từ URL: {url}")
        return Image.open(io.BytesIO(response.content)).convert("RGB")


# ----------------------------------------------------------------
# /ocr/cccd  — Bóc tách thông tin từ ảnh CCCD (EasyOCR)
# ----------------------------------------------------------------
@app.post("/ocr/cccd", response_model=OcrResponse)
async def ocr_cccd(req: OcrRequest):
    try:
        import easyocr
        reader = easyocr.Reader(["vi", "en"], gpu=False)

        img = await download_image(req.image_url)
        img_np = np.array(img)
        results = reader.readtext(img_np, detail=0, paragraph=False)
        raw_text = " | ".join(results)

        identity_card_number = ""
        full_name = ""
        date_of_birth = ""

        import re
        for i, text in enumerate(results):
            # Số CCCD: dãy 9-12 chữ số liên tiếp
            if re.fullmatch(r"\d{9,12}", text.strip()):
                identity_card_number = text.strip()

            # Họ tên: thường đứng sau dòng "Họ và tên" hoặc "Họ tên"
            if re.search(r"họ.*(tên|ten)", text, re.IGNORECASE) and i + 1 < len(results):
                full_name = results[i + 1].strip()

            # Ngày sinh: định dạng DD/MM/YYYY
            dob_match = re.search(r"\d{2}/\d{2}/\d{4}", text)
            if dob_match and not date_of_birth:
                date_of_birth = dob_match.group()

        return OcrResponse(
            identity_card_number=identity_card_number,
            full_name=full_name,
            date_of_birth=date_of_birth,
            raw_text=raw_text,
        )

    except ImportError:
        raise HTTPException(
            status_code=503,
            detail="EasyOCR chưa được cài đặt. Chạy: pip install easyocr"
        )
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


# ----------------------------------------------------------------
# /face/match  — So khớp khuôn mặt CCCD vs Selfie (DeepFace)
# ----------------------------------------------------------------
@app.post("/face/match", response_model=FaceMatchResponse)
async def face_match(req: FaceMatchRequest):
    try:
        from deepface import DeepFace
        import tempfile, os

        cccd_img = await download_image(req.cccd_image_url)
        selfie_img = await download_image(req.selfie_image_url)

        # Lưu tạm ra file vì DeepFace yêu cầu đường dẫn file
        with tempfile.NamedTemporaryFile(suffix=".jpg", delete=False) as f1:
            cccd_path = f1.name
            cccd_img.save(cccd_path)

        with tempfile.NamedTemporaryFile(suffix=".jpg", delete=False) as f2:
            selfie_path = f2.name
            selfie_img.save(selfie_path)

        try:
            result = DeepFace.verify(
                img1_path=cccd_path,
                img2_path=selfie_path,
                model_name="ArcFace",
                detector_backend="retinaface",
                enforce_detection=False,
            )
            score = round(1.0 - result.get("distance", 1.0), 4)
            score = max(0.0, min(1.0, score))
            is_match = result.get("verified", False)

            return FaceMatchResponse(
                match_score=score,
                is_match=is_match,
                message="Khuôn mặt khớp với CCCD." if is_match else "Khuôn mặt KHÔNG khớp với CCCD.",
            )
        finally:
            os.unlink(cccd_path)
            os.unlink(selfie_path)

    except ImportError:
        raise HTTPException(
            status_code=503,
            detail="DeepFace chưa được cài đặt. Chạy: pip install deepface"
        )
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


# ----------------------------------------------------------------
# /health  — Kiểm tra service đang chạy
# ----------------------------------------------------------------
@app.get("/health")
def health():
    return {"status": "ok", "service": "SmartRoomFinder eKYC AI Service"}
