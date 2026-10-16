# 🏛️ Arhitectura Sistemului, Diagrame Tehnice & Contracte API
**Documentație Tehnică de Proiectare — Lucrare de Licență NutriScan**  
**Corelat cu:** [NUT-12](https://maximilianandrei2101.atlassian.net/browse/NUT-12) & Capitolele 3 și 4 din Teză

---

## 1. Diagrama de Pachete și Componente a Sistemului

Următoarea diagramă ilustrează decuplarea componentelor și fluxul de date între containerele Docker:

```mermaid
flowchart TD
    subgraph ClientTier["📱 Stratul de Prezentare (Frontend PWA)"]
        UI["React 18/19 SPA / PWA<br/>(TypeScript, Tailwind CSS, shadcn/ui)"]
        CamMod["Modul Cameră & Upload"]
        HitLMod["Formular Validare HitL<br/>(Scor Încredere)"]
        DiaryMod["Jurnal & Inel Caloric SVG"]
        UI --> CamMod & HitLMod & DiaryMod
    end

    subgraph ApiGatewayTier["⚙️ Stratul de Business (Backend .NET 8 Web API)"]
        WebApi["WebApi Controllers<br/>(Auth, Scans, Foods, Diary, Recipes)"]
        AppLayer["Application Layer<br/>(CQRS, MediatR Handlers, FluentValidation)"]
        DomainLayer["Domain Layer<br/>(Entities, Enums, Value Objects)"]
        InfraLayer["Infrastructure Layer<br/>(EF Core, Polly HTTP Client, JWT)"]

        WebApi --> AppLayer
        AppLayer --> DomainLayer
        InfraLayer -.-> AppLayer
        InfraLayer --> DomainLayer
    end

    subgraph AiTier["👁️ Stratul Computer Vision (Microserviciu Python)"]
        FastAPIApp["FastAPI Service (Port 8000)"]
        CvPipe["OpenCV Pipeline<br/>(Grayscale, Bilateral, Otsu)"]
        OcrRunner["OCR Engines<br/>(EasyOCR / Tesseract)"]
        Parser["Parser Semantic Regex<br/>(RO/EN Nutrient Matcher)"]

        FastAPIApp --> CvPipe --> OcrRunner --> Parser
    end

    subgraph DataTier["🗄️ Stratul de Persistență"]
        PostgresDB[("PostgreSQL 16 DB<br/>(nutriscan_db)")]
    end

    UI -- "HTTPS REST (JWT Auth)<br/>Port 5000" --> WebApi
    InfraLayer -- "Internal HTTP REST<br/>POST /api/v1/ocr/process" --> FastAPIApp
    InfraLayer -- "Npgsql TCP Port 5432" --> PostgresDB
```

---

## 2. Diagrama Entitate-Relație (ERD) — Baza de Date PostgreSQL

Structura relațională implementată prin Entity Framework Core (`InitialCreate`):

```mermaid
erDiagram
    USERS ||--o{ DIARY_ENTRIES : logs
    USERS ||--o{ RECIPES : creates
    USERS ||--o{ PLANNED_MEALS : plans
    USERS ||--o{ SHOPPING_LIST_ITEMS : manages
    USERS ||--o{ REFRESH_TOKENS : authenticates_with
    FOOD_ITEMS ||--o{ DIARY_ENTRIES : contains
    FOOD_ITEMS ||--o{ RECIPE_INGREDIENTS : used_in
    RECIPES ||--o{ RECIPE_INGREDIENTS : composed_of
    RECIPES ||--o{ PLANNED_MEALS : scheduled_in
    FOOD_ITEMS ||--o{ PLANNED_MEALS : scheduled_in

    USERS {
        uuid Id PK
        string Email UK
        string PasswordHash
        string FirstName
        string LastName
        date BirthDate
        float HeightCm
        float WeightKg
        int Gender
        int ActivityLevel
        int Goal
        int TargetCalories
        float TargetProteinsGrams
        float TargetCarbsGrams
        float TargetFatsGrams
        int FailedLoginAttempts
        timestamp LockoutEndUtc
        timestamp CreatedAtUtc
    }

    REFRESH_TOKENS {
        uuid Id PK
        uuid UserId FK
        string TokenHash UK
        uuid FamilyId
        timestamp CreatedAtUtc
        timestamp ExpiresAtUtc
        timestamp RevokedAtUtc
        uuid ReplacedByTokenId
    }

    FOOD_ITEMS {
        uuid Id PK
        string Name
        string Brand
        string Barcode
        float ServingSizeGrams
        float Calories
        float Proteins
        float Carbohydrates
        float Fats
        float Fiber
        float Sodium
        boolean IsVerified
        uuid CreatedByUserId FK
        timestamp CreatedAtUtc
    }

    DIARY_ENTRIES {
        uuid Id PK
        uuid UserId FK
        uuid FoodItemId FK
        date ConsumedDate
        int MealType
        float QuantityGrams
        float CalculatedCalories
        float CalculatedProteins
        float CalculatedCarbs
        float CalculatedFats
        timestamp CreatedAtUtc
    }

    RECIPES {
        uuid Id PK
        uuid UserId FK
        string Name
        string Description
        string Instructions
        int ServingsCount
        timestamp CreatedAtUtc
    }

    RECIPE_INGREDIENTS {
        uuid Id PK
        uuid RecipeId FK
        uuid FoodItemId FK
        float QuantityGrams
    }

    PLANNED_MEALS {
        uuid Id PK
        uuid UserId FK
        date PlanDate
        int MealType
        uuid FoodItemId FK
        uuid RecipeId FK
        float QuantityGrams
        boolean IsTransferredToDiary
        timestamp CreatedAtUtc
    }

    SHOPPING_LIST_ITEMS {
        uuid Id PK
        uuid UserId FK
        string ItemName
        float QuantityGrams
        boolean IsPurchased
        timestamp CreatedAtUtc
    }
```

---

## 3. Diagrama de Secvență: Fluxul de Scanare OCR Human-in-the-Loop

```mermaid
sequenceDiagram
    autonumber
    actor User as Utilizator (Mobil / PWA)
    participant Client as Client React PWA
    participant Backend as ASP.NET Core API
    participant OCR as Python FastAPI OCR
    participant DB as PostgreSQL DB

    User->>Client: Deschide camera & capturează eticheta
    Client->>Client: Verificare dimensiune & format (JPG/PNG)
    Client->>Backend: POST /api/scans (Multipart Image + JWT)
    Backend->>Backend: Validare JWT & autorizare utilizator
    Backend->>OCR: POST /api/v1/ocr/process (Internal HTTP)
    
    rect rgb(240, 248, 255)
        note over OCR: Pipeline Computer Vision
        OCR->>OCR: Redimensionare & Grayscale (OpenCV)
        OCR->>OCR: Bilateral Filter (reducere reflexii)
        OCR->>OCR: Binarizare Otsu adaptivă
        OCR->>OCR: Inferență OCR (Text Bounding Boxes)
        OCR->>OCR: Parsare semantică nutrienți & calcul confidence
    end

    OCR-->>Backend: HTTP 200 OK (JSON cu nutrienți + confidence)
    Backend-->>Client: HTTP 200 OK (DTO cu valori extrase & scoruri)

    rect rgb(255, 250, 240)
        note over User,Client: Flux Human-in-the-Loop (HitL)
        Client->>User: Afișare dialog validare (câmpuri verzi/galbene)
        User->>Client: Confirmă / corectează valorile numerice
    end

    Client->>Backend: POST /api/diary/entries (Salvare aliment în masă)
    Backend->>DB: INSERT INTO DiaryEntries & Recalculate Totals
    DB-->>Backend: Confirmare salvare
    Backend-->>Client: HTTP 201 Created (Actualizare total caloric zilnic)
    Client->>User: Animație inel caloric actualizat în timp real
```

---

## 4. Contractul OpenAPI Înghețat (API ↔ OCR)

### Endpoint: `POST /api/v1/ocr/process`
* **Content-Type:** `multipart/form-data`
* **Request Payload:**
  * `file`: Fișier binar imagine (`image/jpeg`, `image/png`, `image/webp`), maxim 10 MB.
* **Exemplu Răspuns (HTTP 200 OK):**
```json
{
  "success": true,
  "calories": {
    "value": 389.0,
    "confidence": 0.95,
    "unit": "kcal"
  },
  "proteins": {
    "value": 16.9,
    "confidence": 0.92,
    "unit": "g"
  },
  "carbohydrates": {
    "value": 66.3,
    "confidence": 0.88,
    "unit": "g"
  },
  "fats": {
    "value": 6.9,
    "confidence": 0.94,
    "unit": "g"
  },
  "fiber": {
    "value": 10.6,
    "confidence": 0.78,
    "unit": "g"
  },
  "sodium": {
    "value": 0.02,
    "confidence": 0.70,
    "unit": "g"
  },
  "serving_base": "100g",
  "overall_confidence": 0.86,
  "raw_text": "Valoare energetica: 389 kcal\nGrasimi: 6.9 g\nCarbohidrati: 66.3 g\nProteine: 16.9 g\nSare: 0.02 g",
  "processing_time_ms": 340.5
}
```

---

## 5. Autentificare: JWT + Refresh Token Rotativ

Decizia completă și alternativele respinse: [ADR-002](adr/ADR-002-authentication-strategy.md). Design detaliat: [spec](specs/2026-10-16-auth-jwt-design.md).

### Contract `/api/auth`

| Metodă și rută | Auth | Corp cerere | Succes | Erori |
|---|---|---|---|---|
| `POST /register` | — | `{ email, password, firstName, lastName }` | 201 `AuthResponse` + cookie | 400, 409, 429 |
| `POST /login` | — | `{ email, password }` | 200 `AuthResponse` + cookie | 400, 401, 429 |
| `POST /refresh` | cookie | — | 200 `AuthResponse` + cookie nou | 401 (+ cookie șters), 429 |
| `POST /logout` | cookie (opțional) | — | 204 + cookie șters | 429 |
| `GET /me` | Bearer | — | 200 `UserDto` | 401 |

* **`AuthResponse`:** `{ accessToken, accessTokenExpiresAtUtc, user: { id, email, firstName, lastName, isProfileComplete } }`
* **Access token:** JWT HS256, valabil 15 minute, păstrat de client **doar în memorie**.
* **Refresh token:** cookie `nutriscan_rt; HttpOnly; Secure; SameSite=Strict; Path=/api/auth`, valabil 7 zile, rotit la fiecare folosire. În baza de date se salvează doar hash-ul SHA-256.
* **Erori:** toate sunt `application/problem+json` (RFC 7807).

### Diagrama de secvență: rotație și detectarea reutilizării

```mermaid
sequenceDiagram
    participant C as Client PWA
    participant A as AuthController
    participant H as RefreshCommandHandler
    participant DB as PostgreSQL
    C->>A: POST /api/auth/refresh (cookie RT1)
    A->>H: RefreshCommand(RT1)
    H->>DB: SELECT RefreshToken WHERE TokenHash = sha256(RT1)
    alt RT1 activ
        H->>DB: INSERT RT2 (aceeași FamilyId), RT1.Revoke(replacedBy = RT2)
        H-->>A: AuthResult(access token, RT2)
        A-->>C: 200 + Set-Cookie RT2
    else RT1 deja revocat (token copiat, reutilizat)
        H->>DB: revocă toate tokenurile active din familie
        H-->>A: UnauthorizedException
        A-->>C: 401 + cookie șters
    end
```
