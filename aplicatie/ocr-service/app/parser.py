import re
from typing import Optional
from app.schemas import NutrientField

class NutritionTextParser:
    """Regex-based semantic parser for extracting macronutrients from OCR text."""

    PATTERNS = {
        "calories": [
            r"(?:energie|energy|valoare\s*energetic[aă]|calorii)[\s:\-\.]*([0-9\.,]+)\s*k?cal",
            r"([0-9\.,]+)\s*k?cal",
        ],
        "proteins": [
            r"(?:proteine?|protein)[\s:\-\.]*([0-9\.,]+)\s*g?",
        ],
        "carbohydrates": [
            r"(?:carbohidra[tț]i|glucide|carbohydrates?)[\s:\-\.]*([0-9\.,]+)\s*g?",
        ],
        "fats": [
            r"(?:gr[aă]simi|lipide|fats?|total\s*fat)[\s:\-\.]*([0-9\.,]+)\s*g?",
        ],
        "fiber": [
            r"(?:fibre|fiber)[\s:\-\.]*([0-9\.,]+)\s*g?",
        ],
        "sodium": [
            r"(?:sare|salt|sodiu|sodium)[\s:\-\.]*([0-9\.,]+)\s*g?",
        ]
    }

    @staticmethod
    def _clean_numeric_value(raw_val: str) -> Optional[float]:
        """Convert extracted string (e.g. '12,5' or '12.5') to float."""
        try:
            # Replace common OCR misreads: letter O -> 0, comma -> dot
            normalized = raw_val.strip().replace('O', '0').replace('o', '0').replace(',', '.')
            val = float(normalized)
            return round(val, 1)
        except ValueError:
            return None

    @classmethod
    def parse_text(cls, text: str) -> dict[str, Optional[NutrientField]]:
        """Parses OCR text and returns extracted nutrients with confidence estimates."""
        results: dict[str, Optional[NutrientField]] = {}
        lower_text = text.lower()

        for nutrient, regex_list in cls.PATTERNS.items():
            found_field: Optional[NutrientField] = None
            for pattern in regex_list:
                match = re.search(pattern, lower_text)
                if match:
                    val = cls._clean_numeric_value(match.group(1))
                    if val is not None and val >= 0:
                        unit = "kcal" if nutrient == "calories" else "g"
                        # Initial confidence estimate based on direct regex match
                        confidence = 0.90 if len(match.groups()) == 1 else 0.75
                        found_field = NutrientField(value=val, confidence=confidence, unit=unit)
                        break
            results[nutrient] = found_field

        return results
