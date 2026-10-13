# 🏃‍♂️ Sprint 1 — Fundație Tehnică, Arhitectură & Infrastructură
**Epic:** [NUT-1] Faza 1: Fundație Tehnică, Arhitectură & Infrastructură  
**Board Jira:** [NUT Sprint 1](https://maximilianandrei2101.atlassian.net/jira/software/projects/NUT/boards/3/backlog)  
**Status:** Planificat / Pregătit de Execuție  
**Durată:** Săptămânile 1 – 2 (~40 ore efort)

---

## 🎯 Obiectivul Sprintului
Inițializarea completă a scheletului arhitectural pe toate cele 3 subsisteme software (Backend .NET 8, Microserviciu Python OCR, Client React PWA), orchestrarea multi-container prin Docker Compose și modelarea schemei de date inițiale în PostgreSQL prin Entity Framework Core.

---

## 📋 Tichete Sprint 1 (Backlog Activ)

| Cheie Jira | Tip | Titlu Story | Responsabilitate Tehnologică | Status |
|:---:|:---:|---|---|:---:|
| **[NUT-2](https://maximilianandrei2101.atlassian.net/browse/NUT-2)** | Story | **Scaffold Backend .NET 8 (Clean Architecture)** | `backend/` — Domain, Application, Infrastructure, WebApi | ⏳ Ready |
| **[NUT-8](https://maximilianandrei2101.atlassian.net/browse/NUT-8)** | Story | **Scaffold Microserviciu Python OCR (FastAPI & OpenCV)** | `ocr-service/` — FastAPI, OpenCV, Uvicorn, Dockerfile | ⏳ Ready |
| **[NUT-9](https://maximilianandrei2101.atlassian.net/browse/NUT-9)** | Story | **Scaffold Client Frontend (React 18/19 PWA + shadcn/ui)** | `client/` — Vite, TypeScript, Tailwind, Lucide, PWA base | ⏳ Ready |
| **[NUT-10](https://maximilianandrei2101.atlassian.net/browse/NUT-10)** | Story | **Orchestrare Multi-Container Docker Compose** | `docker-compose.yml` — PostgreSQL 16, Healthchecks, Rețea | ⏳ Ready |
| **[NUT-11](https://maximilianandrei2101.atlassian.net/browse/NUT-11)** | Story | **Proiectare & Migrare Model Date PostgreSQL (EF Core)** | `backend/src/Domain`, `Infrastructure` — Migrare inițială | ⏳ Ready |
| **[NUT-12](https://maximilianandrei2101.atlassian.net/browse/NUT-12)** | Story | **Contract API & Diagrame de Arhitectură (UML & ERD)** | `aplicatie/docs/` — OpenAPI contract, UML Use Cases, ERD | ⏳ Ready |

---

## 🔍 Detaliere User Stories & Acceptance Criteria (AC)

### 1. [NUT-2] Scaffold Backend .NET 8 (Clean Architecture)
* **User Story:** Ca dezvoltator, doresc o soluție .NET 8 organizată pe 4 straturi decuplate (Clean Architecture), astfel încât logica de business să fie complet independentă de framework-uri și baze de date.
* **Acceptance Criteria (AC):**
  1. Soluția conține proiectele:
     * `NutriScan.Domain` (clasa `BaseEntity`, `AuditableEntity`, erori de domeniu).
     * `NutriScan.Application` (MediatR, FluentValidation, comportamente pipeline de validare).
     * `NutriScan.Infrastructure` (`ApplicationDbContext`, mapări EF Core, implementări de repository).
     * `NutriScan.WebApi` (configurare DI, middleware global de tratare erori, Swagger/OpenAPI).
  2. Compilarea soluției (`dotnet build`) se finalizează cu 0 erori și 0 avertismente.
  3. Endpoint-ul de test `GET /health` sau `GET /api/v1/health` returnează `200 OK`.

### 2. [NUT-8] Scaffold Microserviciu Python OCR (FastAPI & OpenCV)
* **User Story:** Ca dezvoltator, doresc o structură modulară pentru microserviciul Python de viziune artificială, pentru a putea dezvolta și testa independent algoritmii de preprocesare OpenCV și motoarele OCR.
* **Acceptance Criteria (AC):**
  1. Structură modulară creată:
     * `app/main.py`: aplicația FastAPI cu suport CORS și tratare excepții.
     * `app/preprocessor.py`: schelet pentru operațiile de preprocesare OpenCV.
     * `app/ocr_engine.py`: schelet pentru inferența OCR.
     * `app/parser.py`: schelet pentru parsarea valorilor nutriționale.
  2. `requirements.txt` conține versiuni fixate pentru: `fastapi`, `uvicorn`, `opencv-python-headless`, `pillow`, `pydantic`.
  3. Swagger UI este accesibil la `http://localhost:8000/docs`.

### 3. [NUT-9] Scaffold Client Frontend (React 18/19 PWA + shadcn/ui)
* **User Story:** Ca utilizator, doresc o interfață modernă, fluidă și responsivă, pregătită pentru utilizare atât pe mobil, cât și pe desktop.
* **Acceptance Criteria (AC):**
  1. Aplicație inițializată cu Vite și TypeScript în `aplicatie/client/`.
  2. Tailwind CSS configurat corect cu variabile CSS pentru tematica Dark/Light.
  3. Bibliotecile `lucide-react`, `clsx`, `tailwind-merge` configurate conform standardului shadcn/ui.
  4. Structura de directoare: `src/components/ui/`, `src/features/`, `src/layouts/`, `src/services/`.
  5. Aplicația pornește fără erori pe `http://localhost:3000`.

### 4. [NUT-10] Orchestrare Multi-Container Docker Compose
* **User Story:** Ca dezvoltator și evaluator de licență, doresc pornirea întregului ecosistem cu o singură comandă, garantând reproductibilitatea pe orice sistem de operare.
* **Acceptance Criteria (AC):**
  1. `docker compose up -d` pornește cele 4 servicii: `postgres`, `ocr-service`, `backend`, `client`.
  2. PostgreSQL are healthcheck configurat (`pg_isready`), iar backend-ul așteaptă ca DB să fie sănătoasă (`condition: service_healthy`).
  3. Rețeaua bridge `nutriscan-network` asigură rezolvarea numelor DNS între containere.
  4. Datele PostgreSQL persistă între reporniri prin volumul `postgres_data`.

### 5. [NUT-11] Proiectare & Migrare Model Date PostgreSQL (EF Core)
* **User Story:** Ca sistem, am nevoie de un model relațional optimizat pentru stocarea profilului nutrițional, jurnalului alimentar, alimentelor și rețetelor.
* **Acceptance Criteria (AC):**
  1. Entitățile `User`, `FoodItem`, `DiaryEntry`, `MealType`, `Recipe`, `PlannedMeal`, `ShoppingListItem` definite conform cerințelor din SRS.
  2. Migrarea inițială `InitialCreate` generată și aplicată automat pe PostgreSQL la pornirea aplicației sau via comandă dedicată.
  3. Seed cu cel puțin 10 alimente uzuale cu date nutriționale complete per 100g.

### 6. [NUT-12] Contract API & Diagrame de Arhitectură (UML & ERD)
* **User Story:** Ca autor al tezei, am nevoie de diagrame tehnice riguroase și de un contract stabil de comunicare între Backend și Modulul OCR pentru Capitolele 3 și 4.
* **Acceptance Criteria (AC):**
  1. Specificarea contractului OpenAPI JSON/YAML pentru endpoint-ul `POST /api/v1/ocr/process`.
  2. Generarea diagramelor Mermaid în `aplicatie/docs/`:
     * Diagrama de Arhitectură / Componente.
     * Diagrama Entitate-Relație (ERD).
     * Diagrama fluxului de scanare OCR (Secvență).
