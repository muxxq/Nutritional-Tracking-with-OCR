import cv2
import numpy as np

class ImagePreprocessor:
    """OpenCV-based image preprocessing pipeline optimized for nutritional labels."""

    @staticmethod
    def bytes_to_cv2(image_bytes: bytes) -> np.ndarray:
        """Decode raw image bytes into an OpenCV BGR image array."""
        np_arr = np.frombuffer(image_bytes, np.uint8)
        img = cv2.imdecode(np_arr, cv2.IMREAD_COLOR)
        if img is None:
            raise ValueError("Failed to decode image from bytes. Ensure valid image format (JPEG, PNG).")
        return img

    @staticmethod
    def resize_max_dimension(image: np.ndarray, max_dim: int = 1600) -> np.ndarray:
        """Resize image while maintaining aspect ratio if larger than max_dim."""
        h, w = image.shape[:2]
        if max(h, w) <= max_dim:
            return image
        scale = max_dim / float(max(h, w))
        new_w, new_h = int(w * scale), int(h * scale)
        return cv2.resize(image, (new_w, new_h), interpolation=cv2.INTER_AREA)

    @staticmethod
    def to_grayscale(image: np.ndarray) -> np.ndarray:
        """Convert BGR image to single channel grayscale."""
        if len(image.shape) == 2:
            return image
        return cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)

    @staticmethod
    def apply_bilateral_filter(gray: np.ndarray) -> np.ndarray:
        """Smooth textures while preserving sharp text edges."""
        return cv2.bilateralFilter(gray, d=9, sigmaColor=75, sigmaSpace=75)

    @staticmethod
    def apply_adaptive_threshold(gray: np.ndarray) -> np.ndarray:
        """Binarize image adaptively using Otsu thresholding."""
        _, thresh = cv2.threshold(gray, 0, 255, cv2.THRESH_BINARY + cv2.THRESH_OTSU)
        return thresh

    @classmethod
    def run_pipeline(cls, image_bytes: bytes) -> tuple[np.ndarray, np.ndarray]:
        """
        Executes complete preprocessing pipeline:
        Returns:
            (original_resized, preprocessed_binary)
        """
        original = cls.bytes_to_cv2(image_bytes)
        resized = cls.resize_max_dimension(original)
        gray = cls.to_grayscale(resized)
        denoised = cls.apply_bilateral_filter(gray)
        binary = cls.apply_adaptive_threshold(denoised)
        return resized, binary
