# 🏃‍♂️ Sprint 1 — Fundație Tehnică, Arhitectură & Infrastructură
**Epic:** [NUT-1] Faza 1: Fundație Tehnică, Arhitectură & Infrastructură  
**Board Jira:** [NUT Sprint 1](https://maximilianandrei2101.atlassian.net/jira/software/projects/NUT/boards/3/backlog)  
**Status:** ✅ Finalizat cu Succes (100% Done)  
**Durată:** Săptămânile 1 – 2 (~40 ore efort)

---

## 🎯 Obiectivul Sprintului
Inițializarea completă a scheletului arhitectural pe toate cele 3 subsisteme software (Backend .NET 8, Microserviciu Python OCR, Client React PWA), orchestrarea multi-container prin Docker Compose și modelarea schemei de date inițiale în PostgreSQL prin Entity Framework Core.

---

## 📋 Tichete Sprint 1 (Status Final)

| Cheie Jira | Tip | Titlu Story | Responsabilitate Tehnologică | Status |
|:---:|:---:|---|---|:---:|
| **[NUT-2](https://maximilianandrei2101.atlassian.net/browse/NUT-2)** | Story | **Scaffold Backend .NET 8 (Clean Architecture)** | `backend/` — Domain, Application, Infrastructure, WebApi | ✅ Done |
| **[NUT-8](https://maximilianandrei2101.atlassian.net/browse/NUT-8)** | Story | **Scaffold Microserviciu Python OCR (FastAPI & OpenCV)** | `ocr-service/` — FastAPI, OpenCV, Uvicorn, Dockerfile | ✅ Done |
| **[NUT-9](https://maximilianandrei2101.atlassian.net/browse/NUT-9)** | Story | **Scaffold Client Frontend (React 18/19 PWA + shadcn/ui)** | `client/` — Vite, TypeScript, Tailwind v4, Lucide, PWA base | ✅ Done |
| **[NUT-10](https://maximilianandrei2101.atlassian.net/browse/NUT-10)** | Story | **Orchestrare Multi-Container Docker Compose** | `docker-compose.yml` — PostgreSQL 16, Healthchecks, Rețea | ✅ Done |
| **[NUT-11](https://maximilianandrei2101.atlassian.net/browse/NUT-11)** | Story | **Proiectare & Migrare Model Date PostgreSQL (EF Core)** | `backend/src/Domain`, `Infrastructure` — Migrare `InitialCreate` | ✅ Done |
| **[NUT-12](https://maximilianandrei2101.atlassian.net/browse/NUT-12)** | Story | **Contract API & Diagrame de Arhitectură (UML & ERD)** | `aplicatie/docs/` — OpenAPI contract, UML Use Cases, ERD, ADR-001 | ✅ Done |

---

## 🔍 Detaliere User Stories & Criterii de Acceptanță Îndeplinite

### 1. [NUT-2] Scaffold Backend .NET 8 (Clean Architecture) — ✅ Finalizat
* **Commit:** `f9ca13d`
* **Realizări:**
  * Soluție .NET 8 structurată pe 4 straturi decuplate: `NutriScan.Domain`, `NutriScan.Application`, `NutriScan.Infrastructure`, `NutriScan.WebApi`.
  * Modele de domeniu: `User`, `FoodItem`, `DiaryEntry`, `Recipe`, `PlannedMeal`, `ShoppingListItem`.
  * Pipeline MediatR (CQRS) cu `ValidationBehavior` FluentValidation.
  * Suită de teste unitare xUnit (Arrange-Act-Assert) care testează scalarea liniară a caloriilor în `DiaryEntryTests`.

### 2. [NUT-8] Scaffold Microserviciu Python OCR (FastAPI & OpenCV) — ✅ Finalizat
* **Commit:** `e4389fa`
* **Realizări:**
  * Aplicație FastAPI modulară cu suport CORS, `/health` și `POST /api/v1/ocr/process`.
  * Modul OpenCV `ImagePreprocessor` (Grayscale, BilateralFilter, Otsu adaptive thresholding).
  * Parser semantic regex `NutritionTextParser` pentru termeni bilingvi (RO/EN) și calcul `confidence_score`.
  * Suită de 4 teste unitare și de integrare pytest validate 100%.

### 3. [NUT-9] Scaffold Client Frontend (React 18/19 PWA + shadcn/ui) — ✅ Finalizat
* **Commit:** `eec5d3c`
* **Realizări:**
  * Proiect React 18/19 cu TypeScript și Vite configurat pe portul 3000.
  * Integrare Tailwind CSS v4 și sistem de design conform mockup-ului oficial (`mockup.html`).
  * Inel caloric SVG animat cu calcul dinamic al caloriilor rămase și bare de macronutrienți.
  * Modal de scanare cameră cu flux Human-in-the-Loop (HitL).
  * Build de producție compilat cu 0 erori TypeScript (`tsc -b && vite build`).

### 4. [NUT-10] Orchestrare Multi-Container Docker Compose — ✅ Finalizat
* **Realizări:**
  * Dockerfile-uri multi-stage create pentru toate cele 3 servicii.
  * Configurație `docker-compose.yml` validată cu rețea `nutriscan-network`, volum persistent `postgres_data` și healthcheck PostgreSQL (`pg_isready`).

### 5. [NUT-11] Proiectare & Migrare Model Date PostgreSQL (EF Core) — ✅ Finalizat
* **Commit:** `3c95cb9`
* **Realizări:**
  * Migrarea inițială EF Core `InitialCreate` generată în `Persistence/Migrations/`.
  * Script `ApplicationDbContextSeed` populat cu 10 alimente uzuale cu valori nutriționale verificate.
  * Migrarea automată și seed-ul executate în `Program.cs` la pornirea WebApi.

### 6. [NUT-12] Contract API & Diagrame de Arhitectură (UML & ERD) — ✅ Finalizat
* **Commit:** `2f05cb5`
* **Realizări:**
  * Înregistrată decizia de arhitectură [ADR-001](file:///home/Max/Documents/Obsidian%20Vault.linux-backup/College/Licenta/aplicatie/docs/adr/ADR-001-clean-architecture-and-ocr-decoupling.md).
  * Generat documentul tehnic complet [architecture_and_contracts.md](file:///home/Max/Documents/Obsidian%20Vault.linux-backup/College/Licenta/aplicatie/docs/architecture_and_contracts.md) cu diagrame Mermaid (Componente, ERD, Secvență OCR HitL) și contract OpenAPI înghețat.

---

## 🏆 Reperul Reșită: Milestone M1 Atins
> **Criteriu M1:** Scheletul Clean Architecture este complet, toate cele 3 servicii au Dockerfile validat, schema bazei de date este modelată și migrată, testele automate trec 100%, iar board-ul Jira este sincronizat în timp real!
