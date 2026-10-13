import io
import numpy as np
import cv2
from fastapi.testclient import TestClient
from app.main import app

client = TestClient(app)

def test_health_endpoint():
    response = client.get("/health")
    assert response.status_code == 200
    data = response.json()
    assert data["status"] == "Healthy"
    assert data["service"] == "NutriScan.OcrService"

def test_ocr_process_endpoint():
    # Create test dummy image in memory
    dummy_img = np.full((150, 150, 3), 255, dtype=np.uint8)
    cv2.putText(dummy_img, "450 kcal", (10, 80), cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 0, 0), 2)
    success, encoded = cv2.imencode(".png", dummy_img)
    assert success
    image_bytes = encoded.tobytes()

    response = client.post(
        "/api/v1/ocr/process",
        files={"file": ("label.png", io.BytesIO(image_bytes), "image/png")}
    )

    assert response.status_code == 200
    data = response.json()
    assert data["success"] is True
    assert "calories" in data
    assert "proteins" in data
    assert data["overall_confidence"] > 0
