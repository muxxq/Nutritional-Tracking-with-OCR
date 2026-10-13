import unittest
import numpy as np
import cv2
from app.preprocessor import ImagePreprocessor
from app.parser import NutritionTextParser

class TestOcrService(unittest.TestCase):

    def test_nutrition_text_parser(self):
        sample_text = (
            "Valoare energetica: 450 kcal\n"
            "Grasimi: 15.5 g\n"
            "Carbohidrati: 55.0 g\n"
            "Proteine: 22.3 g\n"
            "Sare: 1.2 g\n"
        )

        nutrients = NutritionTextParser.parse_text(sample_text)

        self.assertIsNotNone(nutrients["calories"])
        self.assertEqual(nutrients["calories"].value, 450.0)
        self.assertEqual(nutrients["calories"].unit, "kcal")

        self.assertIsNotNone(nutrients["proteins"])
        self.assertEqual(nutrients["proteins"].value, 22.3)

        self.assertIsNotNone(nutrients["carbohydrates"])
        self.assertEqual(nutrients["carbohydrates"].value, 55.0)

        self.assertIsNotNone(nutrients["fats"])
        self.assertEqual(nutrients["fats"].value, 15.5)

        self.assertIsNotNone(nutrients["sodium"])
        self.assertEqual(nutrients["sodium"].value, 1.2)

    def test_image_preprocessor_pipeline(self):
        # Create a synthetic test image (200x200 3-channel RGB image)
        dummy_img = np.full((200, 200, 3), 255, dtype=np.uint8)
        cv2.putText(dummy_img, "TEST", (50, 100), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 0, 0), 2)

        success, encoded = cv2.imencode(".jpg", dummy_img)
        self.assertTrue(success)
        image_bytes = encoded.tobytes()

        resized, binary = ImagePreprocessor.run_pipeline(image_bytes)

        self.assertEqual(resized.shape[:2], (200, 200))
        self.assertEqual(binary.shape, (200, 200))
        self.assertEqual(len(binary.shape), 2)  # single-channel binary mask

if __name__ == "__main__":
    unittest.main()
