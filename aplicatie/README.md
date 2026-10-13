# NutriScan — Aplicație Full Stack Tracking Nutrițional cu OCR

Proiect de Lucrare de Licență — **Universitatea din Craiova, Facultatea ACE**  
**Student:** Maximilian Andrei  
**Coordonator:** Dr. Ing. Mocanu Lucian-Mihai  

---

## 🏛️ Arhitectura Sistemului

Aplicația este decuplată în 4 componente principale, rulate în containere Docker:

1. **`client/` (Frontend React PWA)**
   * **Tehnologii:** React 18/19, TypeScript, Vite, Tailwind CSS, shadcn/ui, Lucide Icons, Recharts.
   * **Caracteristici:** PWA instalabilă pe telefon (iOS / Android) și accesibilă din orice browser desktop (PC).
   * **Port local:** `http://localhost:3000`

2. **`backend/` (Core REST API)**
   * **Tehnologii:** ASP.NET Core 8 Web API, C#, Entity Framework Core, MediatR (CQRS), FluentValidation, JWT Authentication.
   * **Arhitectură:** Clean Architecture (`Domain`, `Application`, `Infrastructure`, `WebApi`).
   * **Documentație API:** Swagger UI la `http://localhost:5000/swagger`

3. **`ocr-service/` (Microserviciu Computer Vision & AI)**
   * **Tehnologii:** Python 3.12, FastAPI, OpenCV, EasyOCR / Tesseract.
   * **Responsabilitate:** Preprocesare imagine, detecție tabel nutrițional, extragere valori per 100g și calculul gradului de încredere (confidence).
   * **Endpoint intern:** `http://localhost:8000/docs`

4. **`postgres` (Bază de date)**
   * **Tehnologii:** PostgreSQL 16 Alpine.
   * **Port intern:** `5432`

---

## 🚀 Rulare Rapidă cu Docker Compose

Pentru a porni întreaga infrastructură (bază de date, servicii și interfață):

```bash
cd aplicatie
docker compose up -d
```

Verificare status containere:
```bash
docker compose ps
```

Oprire containere:
```bash
docker compose down
```

---

## 📂 Structura Modulelor

```text
aplicatie/
├── docker-compose.yml       # Orchestrează DB, Backend, OCR și Client
├── README.md                # Ghidul tehnic al aplicației
│
├── client/                  # Frontend SPA / PWA (React + Vite + shadcn)
│   ├── src/
│   │   ├── components/      # Componente UI refolosibile (shadcn/ui)
│   │   ├── features/        # Module specifice (jurnal, scaner OCR, retete)
│   │   ├── layouts/         # Layout adaptiv Mobil vs. Desktop
│   │   └── services/        # Apeluri API catre backend-ul .NET
│   └── Dockerfile
│
├── backend/                 # Solutia .NET 8 (Clean Architecture)
│   ├── src/
│   │   ├── Domain/          # Entitati de business (User, Food, Meal, DiaryEntry)
│   │   ├── Application/     # Comenzi si Interogari (CQRS + MediatR)
│   │   ├── Infrastructure/  # EF Core, DbContext, Migrari, OcrClient
│   │   └── WebApi/          # Controllere REST, Swagger, JWT
│   └── Dockerfile
│
└── ocr-service/             # Microserviciu Python
    ├── app/
    │   ├── main.py          # FastAPI application
    │   ├── preprocessor.py  # Filtre OpenCV (thresholding, denoise)
    │   ├── ocr_engine.py    # EasyOCR / Tesseract runner
    │   └── parser.py        # Regex & parser semantic nutrițional
    ├── requirements.txt
    └── Dockerfile
```
