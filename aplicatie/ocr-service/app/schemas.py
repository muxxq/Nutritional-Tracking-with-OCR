from typing import Optional
from pydantic import BaseModel, Field

class NutrientField(BaseModel):
    value: float
    confidence: float = Field(..., ge=0.0, le=1.0)
    unit: str = "g"

class OcrExtractionResponse(BaseModel):
    success: bool
    calories: Optional[NutrientField] = None
    proteins: Optional[NutrientField] = None
    carbohydrates: Optional[NutrientField] = None
    fats: Optional[NutrientField] = None
    fiber: Optional[NutrientField] = None
    sodium: Optional[NutrientField] = None
    serving_base: str = "100g"
    overall_confidence: float = Field(..., ge=0.0, le=1.0)
    raw_text: str = ""
    processing_time_ms: float = 0.0

class HealthResponse(BaseModel):
    status: str
    service: str
    version: str
    environment: str
