import os
from fastapi import FastAPI, File, UploadFile, HTTPException, status
from fastapi.middleware.cors import CORSMiddleware
from app.schemas import HealthResponse, OcrExtractionResponse
from app.ocr_engine import OcrEngine

app = FastAPI(
    title="NutriScan OCR Microservice",
    version="1.0.0",
    description="Microserviciu Python FastAPI pentru procesare imagini cu OpenCV și recunoaștere etichete nutriționale"
)

# CORS middleware
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

ocr_engine = OcrEngine()

# Max allowed upload size: 10MB
MAX_FILE_SIZE_BYTES = 10 * 1024 * 1024
ALLOWED_IMAGE_TYPES = {"image/jpeg", "image/png", "image/webp"}

@app.get("/health", response_model=HealthResponse, tags=["Monitoring"])
def health_check():
    """Healthcheck endpoint for Docker Compose and monitoring."""
    return HealthResponse(
        status="Healthy",
        service="NutriScan.OcrService",
        version="1.0.0",
        environment=os.getenv("APP_ENV", "development")
    )

@app.post(
    "/api/v1/ocr/process",
    response_model=OcrExtractionResponse,
    status_code=status.HTTP_200_OK,
    tags=["OCR Pipeline"]
)
async def process_label_image(file: UploadFile = File(...)):
    """
    Receives an image file, applies OpenCV preprocessing, extracts text,
    and returns parsed macronutrient values per 100g with confidence scores.
    """
    # 1. Validate MIME type
    if file.content_type not in ALLOWED_IMAGE_TYPES:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=f"Invalid image type '{file.content_type}'. Allowed types: {list(ALLOWED_IMAGE_TYPES)}"
        )

    # 2. Read bytes and check size
    image_bytes = await file.read()
    if len(image_bytes) == 0:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="Uploaded file is empty."
        )

    if len(image_bytes) > MAX_FILE_SIZE_BYTES:
        raise HTTPException(
            status_code=status.HTTP_413_REQUEST_ENTITY_TOO_LARGE,
            detail=f"Image size exceeds the maximum limit of {MAX_FILE_SIZE_BYTES // (1024 * 1024)}MB."
        )

    try:
        result = ocr_engine.extract_from_bytes(image_bytes)
        return result
    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Image processing error: {str(e)}"
        )
