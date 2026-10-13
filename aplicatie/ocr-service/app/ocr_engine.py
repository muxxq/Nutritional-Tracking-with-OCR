import time
import numpy as np
from app.preprocessor import ImagePreprocessor
from app.parser import NutritionTextParser
from app.schemas import OcrExtractionResponse, NutrientField

class OcrEngine:
    """Core recognition runner coordinating preprocessing and text parsing."""

    def __init__(self):
        # Engine initialization (models will be loaded in Phase 2)
        pass

    def extract_from_bytes(self, image_bytes: bytes) -> OcrExtractionResponse:
        start_time = time.time()
        
        # 1. Run OpenCV preprocessing pipeline
        _, binary_img = ImagePreprocessor.run_pipeline(image_bytes)

        # 2. Text recognition mock/placeholder for scaffold phase
        # In Phase 2 (Sprint 2/3), EasyOCR and Tesseract models perform inference here.
        extracted_text = (
            "Valoare energetica: 389 kcal\n"
            "Grasimi: 6.9 g\n"
            "Carbohidrati: 66.3 g\n"
            "Fibre: 10.6 g\n"
            "Proteine: 16.9 g\n"
            "Sare: 0.02 g\n"
            "Per 100g produs"
        )

        # 3. Parse nutritional values
        parsed_nutrients = NutritionTextParser.parse_text(extracted_text)

        elapsed_ms = (time.time() - start_time) * 1000

        # Calculate overall confidence
        valid_fields = [f for f in parsed_nutrients.values() if f is not None]
        overall_conf = (
            sum(f.confidence for f in valid_fields) / len(valid_fields)
            if valid_fields else 0.0
        )

        return OcrExtractionResponse(
            success=True,
            calories=parsed_nutrients.get("calories"),
            proteins=parsed_nutrients.get("proteins"),
            carbohydrates=parsed_nutrients.get("carbohydrates"),
            fats=parsed_nutrients.get("fats"),
            fiber=parsed_nutrients.get("fiber"),
            sodium=parsed_nutrients.get("sodium"),
            serving_base="100g",
            overall_confidence=round(overall_conf, 2),
            raw_text=extracted_text,
            processing_time_ms=round(elapsed_ms, 2)
        )
