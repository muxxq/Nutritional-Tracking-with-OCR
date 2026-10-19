# Spec: Catalogul de alimente (backend)

**Data:** 2026-10-16
**Branch:** `feature/NUT-food-catalog`
**Cerințe SRS:** UC01 (catalog personal), FR-05 (formular reutilizabil pentru HitL), FR-06 (coerența energetică, P2); FR-13 (catalog global administrat, P3) doar parțial: catalogul global există, administrarea lui nu
**Faza din roadmap:** Faza 2 (Backend core), Sprint 2
**Depinde de:** autentificare (ADR-002)

---

## 1. Scop

### User story
> Ca utilizator NutriScan, vreau să caut alimente într-un catalog de încredere și să-mi adaug propriile alimente cu valorile de pe etichetă, ca să le pot folosi apoi în jurnal și în rețete.

### Criterii de acceptanță
- **AC1:** `GET /api/foods?query=…` întoarce alimentele globale + alimentele proprii nearhivate, paginat, fără să țină cont de majuscule și diacritice („fara piele” găsește „fără piele”), cu toate cuvintele interogării obligatorii.
- **AC2:** `POST /api/foods` cu date valide creează un aliment personal (201). Alimentul apare în căutare doar pentru autorul lui.
- **AC3:** `PUT` / `DELETE` pe un aliment propriu reușesc (200 / 204). `DELETE` arhivează: alimentul dispare din căutare și din `GET /api/foods/{id}` (404), dar rândurile care îl referă (jurnal, rețete) rămân valide.
- **AC4:** alimentul altui utilizator (sau unul arhivat) primește **404** la `GET`, `PUT` și `DELETE`; un aliment global primește **403** la `PUT` și `DELETE`.
- **AC5:** valori nutriționale imposibile (§3.3) primesc 400 ProblemDetails.
- **AC6:** dacă energia calculată diferă cu peste 15% de cea declarată, alimentul se salvează, iar răspunsul conține `energyWarning` (FR-06).
- **AC7:** sarea se stochează în grame, cum apare pe etichetele UE; valorile de sodiu existente sunt convertite (`sare = sodiu_mg × 2,5 / 1000`).
- **AC8:** toate rutele `/api/foods` cer access token valid (401).

### În afara scopului
Rolul Admin și administrarea catalogului global (FR-13), catalogul comunitar (alimente publice), căutarea după codul de bare, importul din baze externe (Open Food Facts), ecranele din client.

---

## 2. Decizii (rezumat pentru ADR-004)

| # | Întrebare | Decizie | Alternative respinse |
|---|---|---|---|
| D1 | Cine vede alimentele create de un utilizator? | **Personal + global:** `CreatedByUserId == null` = global (verificat, doar citire); altfel privat al autorului | Comunitar (necesită moderare și rol Admin; poate fi adăugat peste D1 cu un câmp de vizibilitate); doar personal (catalog gol la început) |
| D2 | Ștergerea | **Arhivare** (`ArchivedAtUtc`) | Ștergere fizică (strică istoricul sau e blocată de FK `Restrict`); refuz cu 409 (catalogul nu se mai poate curăța) |
| D3 | Sodiu sau sare? | **`SaltGrams`**, exact ce declară eticheta UE (Reg. 1169/2011) | `Sodium` în mg: necesită conversie la fiecare citire OCR și era sursa unui bug cunoscut în parser |
| D4 | Coerența energetică | **Avertisment** calculat la citire (FR-06), nu eroare | Respingere: fibrele (~2 kcal/g), alcoolul (7 kcal/g) și polialcoolii produc diferențe legitime |
| D5 | Căutarea | Coloana **`SearchText`** normalizată în Domain (litere mici, fără diacritice) + `Contains` pe fiecare cuvânt | `unaccent` + `ILIKE` (netestabil în InMemory, neindexabil direct); full-text `tsvector` (fără prefixe parțiale, prea complex pentru volum) |
| D6 | 403 sau 404 | 404 pentru alimentele altora și cele arhivate (nu se confirmă existența); 403 pentru cele globale (sunt vizibile oricum) | 404 peste tot: ar fi fals pentru un aliment pe care utilizatorul tocmai l-a văzut |

---

## 3. Domain

### 3.1 `FoodItem`
- `Sodium` (mg) devine `SaltGrams` (`double?`).
- Câmpuri noi:
  - `ArchivedAtUtc` (`DateTime?`);
  - `SearchText` (`string`, setter privat).
- `Name` și `Brand` sunt proprietăți care, la set, recalculează `SearchText = TextNormalizer.ForSearch($"{Name} {Brand}")`.
- `ServingSizeGrams` devine `double?`: porția uzuală (de exemplu 30 g). Valorile nutriționale rămân per 100 g.
- `bool IsGlobal => CreatedByUserId is null`.
- `bool IsVisibleTo(Guid userId) => ArchivedAtUtc is null && (IsGlobal || CreatedByUserId == userId)`.
- `void Archive(DateTime nowUtc)`.
- `string? EnergyWarning()`: vezi §3.2.

### 3.2 Coerența energetică (FR-06)
În `Domain/Nutrition/EnergyCheck.cs`, refolosibilă de ecranul OCR:
```
calculată = 4·P + 4·C + 9·G
declarată = 0 și calculată = 0          → fără avertisment
declarată = 0 și calculată > 0          → avertisment
|calculată − declarată| / declarată > 0,15 → avertisment
```
Mesaj: „Caloriile declarate ({declarată} kcal) diferă cu {abatere}% de cele calculate din macronutrienți ({calculată} kcal). Verifică valorile.” Valorile kcal sunt rotunjite la întreg, procentul la întreg. Pragul de 15% coincide cu `NutritionTargetCalculator.EnergyTolerance`.

### 3.3 Reguli de validare (per 100 g)

| Câmp | Regulă |
|---|---|
| `Name` | obligatoriu după trim, maximum 250 de caractere |
| `Brand` | opțional, maximum 150 de caractere |
| `Barcode` | opțional, doar cifre, 8–14 caractere |
| `ServingSizeGrams` | opțional, 1–2000 |
| `Calories` | 0–900 |
| `Proteins`, `Carbohydrates`, `Fats` | fiecare 0–100, iar `P + C + G ≤ 100` |
| `Sugars` | opțional, 0 ≤ zaharuri ≤ `Carbohydrates` |
| `SaturatedFats` | opțional, 0 ≤ saturate ≤ `Fats` |
| `Fiber`, `SaltGrams` | opționale, 0–100 |

### 3.4 Normalizarea textului
`TextNormalizer.ForSearch(string)`: `ToLowerInvariant` → `Normalize(FormD)` → elimină caracterele `UnicodeCategory.NonSpacingMark` → spațiile multiple devin unul singur → trim. Acoperă ă, â, î, ș/ş, ț/ţ (inclusiv majusculele).
`TextNormalizer.SearchTerms(string)`: normalizare + împărțire pe spații, fără termeni goi.

---

## 4. Persistență

- `SearchText` `varchar(400)`, obligatoriu, index B-tree. Un index trigram (`pg_trgm`) se poate adăuga ulterior fără schimbări de cod.
- `ArchivedAtUtc` nullable; index pe `CreatedByUserId` (există deja relația).
- Migrarea `FoodCatalog`:
  1. `AddColumn SaltGrams`, apoi `UPDATE "FoodItems" SET "SaltGrams" = "Sodium" * 2.5 / 1000 WHERE "Sodium" IS NOT NULL`, apoi `DropColumn Sodium` (fără redenumire);
  2. `AddColumn ArchivedAtUtc`;
  3. `AddColumn SearchText` cu valoare implicită `''`, apoi `UPDATE "FoodItems" SET "SearchText" = trim(translate(lower("Name" || ' ' || coalesce("Brand", '')), 'ăâîșşțţ', 'aaissstt'))`, apoi indexul;
  4. `ServingSizeGrams` devine nullable.
  - `Down` face operațiile inverse.
- Seed-ul (`ApplicationDbContextSeed`) folosește `SaltGrams`, convertit din valorile actuale.

---

## 5. Contract API

Toate rutele sunt `[Authorize]`, cu id-ul utilizatorului din claim-ul `sub`.

| Metodă și rută | Succes | Erori |
|---|---|---|
| `GET /api/foods?query=&scope=all|mine&page=1&pageSize=20` | 200 `PagedResult<FoodSummaryDto>` | 400 (page < 1, pageSize ∉ 1–50, scope necunoscut), 401 |
| `GET /api/foods/{id}` | 200 `FoodDto` | 401, 404 |
| `POST /api/foods` | 201 `FoodDto` (`Location: /api/foods/{id}`) | 400, 401 |
| `PUT /api/foods/{id}` | 200 `FoodDto` | 400, 401, 403, 404 |
| `DELETE /api/foods/{id}` | 204 | 401, 403, 404 |

```jsonc
// SaveFoodRequest (POST și PUT)
{ "name": "Fulgi de ovăz", "brand": "Generic", "barcode": null, "servingSizeGrams": 40,
  "calories": 389, "proteins": 16.9, "carbohydrates": 66.3, "sugars": 1.0,
  "fats": 6.9, "saturatedFats": 1.2, "fiber": 10.6, "saltGrams": 0.01 }

// FoodDto
{ "id": "…", "name": "…", "brand": "…", "barcode": null, "servingSizeGrams": 40,
  "per100g": { "calories": 389, "proteins": 16.9, "carbohydrates": 66.3, "sugars": 1.0,
               "fats": 6.9, "saturatedFats": 1.2, "fiber": 10.6, "saltGrams": 0.01 },
  "isVerified": false, "isOwn": true, "energyWarning": null }

// FoodSummaryDto
{ "id": "…", "name": "…", "brand": "…", "calories": 389, "proteins": 16.9,
  "carbohydrates": 66.3, "fats": 6.9, "isVerified": true, "isOwn": false }

// PagedResult<T>
{ "items": [ … ], "page": 1, "pageSize": 20, "totalCount": 42 }
```

**Căutarea:**
- `scope=all`: globale + proprii; `scope=mine`: doar proprii; mereu fără cele arhivate.
- Fiecare termen din `TextNormalizer.SearchTerms(query)` trebuie să apară în `SearchText` (`Contains`); o interogare goală nu filtrează.
- Ordonare:
  1. `SearchText` începe cu primul termen;
  2. `IsVerified` descrescător;
  3. `Name` crescător;
  4. `Id` (pentru o paginare stabilă).
- `isOwn` = `CreatedByUserId == userId`.

**Alimente create de utilizator:** `IsVerified = false`; `CreatedByUserId = userId`. `PUT` înlocuiește toate câmpurile (semantică PUT).

---

## 6. Arhitectură (fișiere)

- **Domain:**
  - `Common/TextNormalizer.cs`;
  - `Nutrition/EnergyCheck.cs`;
  - `Entities/FoodItem.cs` (modificări §3.1).
- **Application:**
  - `Common/Models/PagedResult.cs`;
  - `Common/Exceptions/ForbiddenException.cs`, `NotFoundException.cs`;
  - `Foods/Dtos/FoodDto.cs` (+ `Per100gDto`, `FoodSummaryDto`);
  - `Foods/FoodRules.cs`: validatorul comun pentru câmpurile nutriționale (`AbstractValidator<IFoodFields>`);
  - `Foods/Queries/SearchFoods/`, `Foods/Queries/GetFood/`;
  - `Foods/Commands/CreateFood/`, `UpdateFood/`, `ArchiveFood/`.
- **Infrastructure:** configurarea EF, migrarea `FoodCatalog`, seed actualizat.
- **WebApi:**
  - `Controllers/FoodsController.cs`, `Contracts/Foods/SaveFoodRequest.cs`;
  - în `GlobalExceptionHandler`: `ForbiddenException` → 403, `NotFoundException` → 404.

---

## 7. Testare

- **Domain:**
  - `TextNormalizerTests`: „Fără”→„fara”, „ȘNIȚEL”→„snitel”, sedilă și virgulă, spații multiple;
  - `FoodItemTests`: `SearchText` urmează numele și marca; `IsVisibleTo`; `Archive`;
  - `EnergyCheckTests`: 15% exact fără avertisment, peste 15% cu avertisment, 0/0 fără, 0 cu macronutrienți > 0 cu avertisment, textul mesajului.
- **Application:**
  - validatorul la fiecare limită: 900/901 kcal; suma 100/100,1 g; zaharuri > carbohidrați; saturate > grăsimi; cod de bare de 7/8/14/15 cifre sau cu litere; porție 0/1/2000/2001;
  - căutarea: diacritice, mai multe cuvinte, ordonare, paginare, `scope=mine`, izolare, arhivate excluse;
  - handlerele: 403 pe global, 404 pe alimentul altuia sau pe unul arhivat, arhivarea setează data.
- **Integrare:**
  - AC1–AC8 end-to-end pe PostgreSQL, inclusiv căutarea cu diacritice tradusă în SQL;
  - un `DiaryEntry` inserat direct în DB, care referă un aliment arhivat, rămâne citibil;
  - conversia sare/sodiu pe seed: „Piept de pui” are 65 mg sodiu, deci 0,1625 g sare.

---

## 8. Livrare și documentație

- Commit-uri *Conventional Commits* fără linii de co-author; merge local în `develop`.
- `aplicatie/docs/adr/ADR-004-food-catalog.md`: deciziile D1–D6 și calea spre catalogul comunitar.
- `architecture_and_contracts.md`: secțiunea `/api/foods`; ERD `FOOD_ITEMS` actualizat.
- `sprint-02.md`, `ROADMAP.md` (bifă la Catalog).
- Fișierul local de întrebări: punctul slab #2 (sare/sodiu) rezolvat în backend; întrebări despre căutare și arhivare. **Fără commit.**
