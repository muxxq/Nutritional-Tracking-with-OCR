namespace NutriScan.Application.Scans;

public static class ScanErrors
{
    public const string Empty = "Alege o fotografie a etichetei.";
    public const string TooLarge = "Fotografia poate avea cel mult 10 MB.";
    public const string UnsupportedFormat = "Fotografia trebuie să fie JPEG, PNG sau WebP.";
    public const string OcrUnavailable = "Recunoașterea automată nu este disponibilă acum. Completează valorile manual.";
}
