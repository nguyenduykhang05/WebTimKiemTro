# SmartRoomFinder — eKYC AI Service

Dịch vụ AI chạy độc lập bằng Python FastAPI, cung cấp 2 tính năng:
1. **OCR CCCD** – Bóc tách số định danh, họ tên, ngày sinh từ ảnh CCCD bằng EasyOCR
2. **Face Matching** – So khớp khuôn mặt CCCD vs Selfie bằng DeepFace (ArcFace)

## Cài đặt & Chạy

```bash
cd ekyc_service

# Tạo môi trường ảo Python
python -m venv venv
venv\Scripts\activate       # Windows
# source venv/bin/activate  # Linux/macOS

# Cài dependencies
pip install -r requirements.txt

# Khởi động service
uvicorn main:app --host 0.0.0.0 --port 8000 --reload
```

Service sẽ chạy tại: http://localhost:8000

## API Endpoints

| Method | Path | Mô tả |
|--------|------|-------|
| GET | /health | Kiểm tra service đang chạy |
| POST | /ocr/cccd | OCR bóc tách thông tin CCCD |
| POST | /face/match | So khớp khuôn mặt |

## Swagger UI
Truy cập http://localhost:8000/docs để xem và thử nghiệm API.

## Cấu hình trong ASP.NET Core
Trong `appsettings.json`, cấu hình URL của service:
```json
"EkycService": {
  "BaseUrl": "http://localhost:8000"
}
```
