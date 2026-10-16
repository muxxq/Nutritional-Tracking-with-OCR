# Catalogul de alimente: plan de implementare

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `/api/foods` (căutare, detalii, creare, editare, arhivare) peste un catalog global + personal, cu căutare fără diacritice, sare în grame și avertismentul de coerență energetică (FR-06).

**Architecture:** regulile (normalizarea textului, vizibilitatea, arhivarea, verificarea energetică) stau în Domain. Application are un validator comun pentru câmpurile nutriționale și handlere MediatR care aplică vizibilitatea. WebApi traduce `ForbiddenException`/`NotFoundException` în 403/404.

**Tech Stack:** .NET 8, EF Core 8 + Npgsql, MediatR 14, FluentValidation 12, xUnit + FluentAssertions + Moq, EF InMemory, Testcontainers (Podman).

**Spec:** `aplicatie/docs/specs/2026-10-16-food-catalog-design.md`. Secțiunile sunt referite ca §N.

## Global Constraints

- Comenzile `dotnet` rulează din `aplicatie/backend/`. Commit-urile se fac pe `feature/NUT-food-catalog`, în *Conventional Commits*, **fără `Co-Authored-By`**.
- Testele de integrare cer `export DOCKER_HOST=unix:///run/user/$(id -u)/podman/podman.sock TESTCONTAINERS_RYUK_DISABLED=true`.
- Tiparul Arrange-Act-Assert; numire `Method_Scenario_ExpectedResult`.
- Valori fixe:
  - validare: calorii 0–900; P/C/G 0–100 fiecare și suma ≤ 100; zaharuri ≤ carbohidrați; saturate ≤ grăsimi; fibre și sare 0–100; cod de bare `^\d{8,14}$`; porție 1–2000; nume ≤ 250; marcă ≤ 150;
  - căutare: `pageSize` 1–50 (implicit 20), `page` ≥ 1;
  - toleranța energetică 0,15;
  - conversia `sare = sodiu_mg × 2,5 / 1000`.
- Mesajele pentru utilizator sunt în română; codul în engleză.
- Timpul curent vine doar din `IDateTimeProvider`.
- Fără `ExecuteUpdateAsync`/`ExecuteDeleteAsync` în Application (testele InMemory).

## Review Focus

1. **Interogare doar cu spații sau semne** (`"   "`, `"%"`, `"_"`): fără excepție, iar `%`/`_` tratate literal, nu ca wildcard SQL. Testul `Search_WildcardCharacters_AreLiteral` e în Task 5 (integrare). EF parametrizează `Contains`, dar trebuie dovedit pe PostgreSQL.
2. **`PUT` pe un aliment arhivat propriu** → 404, nu reactivare tacită. Testul `Update_ArchivedOwnFood_ThrowsNotFound` e în Task 3.
3. **`DELETE` de două ori** → al doilea 404, nu 500 (dubla arhivare). Testul `Archive_Twice_SecondThrowsNotFound` e în Task 3.
4. **Valori zecimale cu virgulă sau negative** (`-0.1`) → 400. Negativele sunt acoperite de validator în Task 3; virgula e o problemă de client, JSON nu o acceptă.
5. **Nume care devine gol după normalizare** (de exemplu doar diacritice combinante) → 400, nu `SearchText` gol. Testul `Validate_NameOfOnlyCombiningMarks_IsInvalid` e în Task 3.

---

### Task 1: Domain: normalizare, `FoodItem`, `EnergyCheck`

**Files:**
- Create: `src/Domain/Common/TextNormalizer.cs`, `src/Domain/Nutrition/EnergyCheck.cs`
- Modify: `src/Domain/Entities/FoodItem.cs`
- Test: `tests/Domain.UnitTests/Common/TextNormalizerTests.cs`, `tests/Domain.UnitTests/Entities/FoodItemTests.cs`, `tests/Domain.UnitTests/Nutrition/EnergyCheckTests.cs`

**Interfaces:**
- Produces:
  ```csharp
  public static class TextNormalizer {
      public static string ForSearch(string text);                 // §3.4
      public static IReadOnlyList<string> SearchTerms(string? query);
  }
  public static class EnergyCheck {
      public const double Tolerance = 0.15;
      public static string? Warning(double declaredKcal, double proteins, double carbs, double fats); // §3.2
  }
  // FoodItem: Name/Brand setters refresh SearchText (private set); Sodium → double? SaltGrams;
  // double? ServingSizeGrams; DateTime? ArchivedAtUtc;
  bool IsGlobal { get; }  bool IsVisibleTo(Guid userId);  void Archive(DateTime nowUtc);  string? EnergyWarning();
  ```
  `NutritionTargetCalculator.EnergyTolerance` rămâne, dar trebuie să fie egal cu `EnergyCheck.Tolerance` (un test verifică asta).

- [ ] **Step 1: Scrie testele (pică)**
  - `TextNormalizerTests`:
    - theory: „Fără”→„fara”, „ȘNIȚEL”→„snitel”, „ş ţ” (sedilă)→„s t”, „  Piept   de  PUI ”→„piept de pui”, „Îngheţată”→„inghetata”;
    - `SearchTerms(" piept  pui ")` → [„piept”, „pui”];
    - `SearchTerms(null)` și `SearchTerms("  ")` → listă goală.
  - `FoodItemTests`:
    - `SearchText_FollowsNameAndBrand`: nume „Fără zahăr”, marca „Ţară” → „fara zahar tara”; schimbarea mărcii la null → „fara zahar”;
    - `IsVisibleTo`: global → true pentru oricine; propriu → true; al altcuiva → false; arhivat → false;
    - `Archive_SetsDate`.
  - `EnergyCheckTests`:
    - 100 kcal cu P 0 C 0 G 13,1 (117,9 kcal, 17,9%) → avertisment;
    - 100 kcal cu C 28,75 (115 kcal, exact 15%) → null;
    - 0/0/0/0 → null; 0 kcal cu P 5 → avertisment;
    - mesajul exact pentru 250 kcal cu P 10 C 30 G 5 → „Caloriile declarate (250 kcal) diferă cu 18% de cele calculate din macronutrienți (205 kcal). Verifică valorile.”;
    - `EnergyCheck.Tolerance == NutritionTargetCalculator.EnergyTolerance`.
- [ ] **Step 2: Rulează** `dotnet test tests/Domain.UnitTests`. Așteptat: eroare de compilare.
- [ ] **Step 3: Implementează.** `FoodItem` primește câmpuri private `_name`, `_brand`. Proprietățile publice au setteri care actualizează `SearchText`. Remediază compilarea din Infrastructure (`Sodium` din seed devine `SaltGrams`, cu valorile convertite: 65→0.1625, 2→0.005, 1→0.0025, 142→0.355 etc.).
- [ ] **Step 4: Rulează** `dotnet test tests/Domain.UnitTests` și `dotnet build`. Așteptat: trec / 0 erori.
- [ ] **Step 5: Commit** `feat(domain): add food search normalization, archiving and energy check`

---

### Task 2: Persistență și migrare

**Files:**
- Modify: `src/Infrastructure/Persistence/ApplicationDbContext.cs`: `SearchText` maximum 400, obligatoriu, index; `ServingSizeGrams` nullable; `Name`/`Brand` mapate prin câmpurile `_name`/`_brand` dacă EF nu le detectează automat
- Create: migrarea `FoodCatalog` (generată, apoi editată conform §4)

- [ ] **Step 1: Generează migrarea** `FoodCatalog`.
- [ ] **Step 2: Editează `Up`:**
  - fără `RenameColumn` `Sodium`→`SaltGrams`; în ordinea: `AddColumn SaltGrams`, `Sql(UPDATE … * 2.5 / 1000)`, `DropColumn Sodium`;
  - `SearchText` primește `defaultValue: ""`, apoi `Sql(UPDATE … translate …)`, apoi indexul.
  - `Down` reface `Sodium` din `SaltGrams * 1000 / 2.5`.
- [ ] **Step 3: Verifică** cu `dotnet build` și `dotnet test`. Toate trec; integrarea aplică migrarea pe Postgres.
- [ ] **Step 4: Commit** `feat(foods): migrate catalog to salt grams, archiving and search text`

---

### Task 3: Application: validator, query-uri, comenzi

**Files:**
- Create: `src/Application/Common/Models/PagedResult.cs`, `src/Application/Common/Exceptions/ForbiddenException.cs`, `NotFoundException.cs`
- Create: `src/Application/Foods/Dtos/FoodDtos.cs`, `src/Application/Foods/IFoodFields.cs`, `src/Application/Foods/FoodFieldsValidator.cs`, `src/Application/Foods/FoodErrors.cs`
- Create: `src/Application/Foods/Queries/SearchFoods/*`, `Queries/GetFood/*`, `Commands/CreateFood/*`, `Commands/UpdateFood/*`, `Commands/ArchiveFood/*`
- Test: `tests/Application.UnitTests/Foods/*Tests.cs`

**Interfaces:**
- Produces:
  ```csharp
  public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
  public sealed class ForbiddenException(string message) : Exception(message);
  public sealed class NotFoundException(string message) : Exception(message);
  public interface IFoodFields { string Name {get;} string? Brand {get;} string? Barcode {get;} double? ServingSizeGrams {get;}
      double Calories {get;} double Proteins {get;} double Carbohydrates {get;} double? Sugars {get;}
      double Fats {get;} double? SaturatedFats {get;} double? Fiber {get;} double? SaltGrams {get;} }
  public sealed class FoodFieldsValidator : AbstractValidator<IFoodFields>  // §3.3; name must be non-empty after TextNormalizer.ForSearch
  public sealed record Per100gDto(double Calories, double Proteins, double Carbohydrates, double? Sugars,
      double Fats, double? SaturatedFats, double? Fiber, double? SaltGrams);
  public sealed record FoodDto(Guid Id, string Name, string? Brand, string? Barcode, double? ServingSizeGrams,
      Per100gDto Per100g, bool IsVerified, bool IsOwn, string? EnergyWarning) { public static FoodDto From(FoodItem f, Guid userId); }
  public sealed record FoodSummaryDto(Guid Id, string Name, string? Brand, double Calories, double Proteins,
      double Carbohydrates, double Fats, bool IsVerified, bool IsOwn);
  public enum FoodScope { All, Mine }
  public sealed record SearchFoodsQuery(Guid UserId, string? Query, FoodScope Scope = FoodScope.All, int Page = 1, int PageSize = 20)
      : IRequest<PagedResult<FoodSummaryDto>>;
  public sealed record GetFoodQuery(Guid UserId, Guid FoodId) : IRequest<FoodDto>;
  public sealed record CreateFoodCommand(Guid UserId, /* IFoodFields members */) : IRequest<FoodDto>, IFoodFields;
  public sealed record UpdateFoodCommand(Guid UserId, Guid FoodId, /* IFoodFields members */) : IRequest<FoodDto>, IFoodFields;
  public sealed record ArchiveFoodCommand(Guid UserId, Guid FoodId) : IRequest;
  public static class FoodErrors { public const string NotFound = "Alimentul nu a fost găsit.";
      public const string GlobalReadOnly = "Alimentele din catalogul global nu pot fi modificate."; }
  ```
  `CreateFoodCommandValidator` și `UpdateFoodCommandValidator` fac `Include(new FoodFieldsValidator())`. `SearchFoodsQueryValidator` verifică `Page`, `PageSize` și `Scope` (`IsInEnum`).
  Regula de acces pentru `Update`/`Archive`: negăsit sau `!IsVisibleTo(user)` → `NotFoundException`; `IsGlobal` → `ForbiddenException`.

- [ ] **Step 1: Scrie testele (pică)**
  - `FoodFieldsValidatorTests`:
    - limite: calorii 900/901, −0,1; suma P+C+G 100/100,1; zaharuri > carbohidrați; saturate > grăsimi;
    - cod de bare: „1234567” (7) invalid, „12345678” valid, 14 cifre valid, 15 invalid, „12345678a” invalid;
    - porție 0/1/2000/2001; nume gol; nume de 251 de caractere;
    - `Validate_NameOfOnlyCombiningMarks_IsInvalid` (`"́́"`);
    - toate opționalele null → valid.
  - `SearchFoodsQueryHandlerTests` (seed: 2 globale verificate „Piept de pui fără piele” și „Pui întreg”, 1 propriu „Pui la grătar”, 1 al altui utilizator „Pui secret”, 1 propriu arhivat „Pui vechi”):
    - „fara piele” → doar „Piept de pui fără piele”;
    - „pui” → „Pui întreg”, „Pui la grătar” (prefix; verificat înainte), apoi „Piept de pui fără piele”; niciodată „Pui secret” sau „Pui vechi”;
    - `scope=Mine` + query gol → doar „Pui la grătar”;
    - paginare: `PageSize = 1`, `Page = 2` → al doilea element, `TotalCount = 3`;
    - `IsOwn` corect.
  - `SearchFoodsQueryValidatorTests`: page 0, pageSize 0/51 → invalide.
  - `FoodCommandHandlerTests`:
    - `Create_SetsOwnerAndUnverified` (+ `FoodDto.IsOwn`, `EnergyWarning` prezent pentru 250 kcal / P 10 C 30 G 5);
    - `Get_OtherUsersFood_ThrowsNotFound`;
    - `Update_OwnFood_ReplacesFields` (`SearchText` actualizat);
    - `Update_GlobalFood_ThrowsForbidden`;
    - `Update_ArchivedOwnFood_ThrowsNotFound`;
    - `Archive_OwnFood_SetsArchivedAt`;
    - `Archive_Twice_SecondThrowsNotFound`;
    - `Archive_GlobalFood_ThrowsForbidden`.
- [ ] **Step 2: Rulează** `dotnet test tests/Application.UnitTests`. Așteptat: eroare de compilare.
- [ ] **Step 3: Implementează.** Căutarea:
  - filtru de vizibilitate în LINQ (`ArchivedAtUtc == null && (CreatedByUserId == null || CreatedByUserId == userId)`; cu `Mine`, doar proprii);
  - pentru fiecare termen, `Where(f => f.SearchText.Contains(term))`;
  - ordonare `OrderByDescending(f => terms.Count > 0 && f.SearchText.StartsWith(terms[0])).ThenByDescending(IsVerified).ThenBy(Name).ThenBy(Id)`;
  - `CountAsync` + `Skip`/`Take`.
- [ ] **Step 4: Rulează** `dotnet test tests/Application.UnitTests`. Așteptat: toate trec.
- [ ] **Step 5: Commit** `feat(foods): add catalog search and food commands`

---

### Task 4: WebApi: `FoodsController` și maparea 403/404

**Files:**
- Create: `src/WebApi/Controllers/FoodsController.cs`, `src/WebApi/Contracts/Foods/SaveFoodRequest.cs`
- Modify: `src/WebApi/Infrastructure/GlobalExceptionHandler.cs` (`ForbiddenException` → 403, `NotFoundException` → 404)
- Test: `tests/WebApi.IntegrationTests/Auth/ErrorHandlingTests.cs` (2 cazuri noi), `tests/WebApi.IntegrationTests/Foods/FoodsTests.cs`

**Interfaces:**
- Consumes: Task 3.
- Produces: `SaveFoodRequest(string Name, string? Brand, string? Barcode, double? ServingSizeGrams, double Calories, double Proteins, double Carbohydrates, double? Sugars, double Fats, double? SaturatedFats, double? Fiber, double? SaltGrams)`. `FoodsController` are `[ApiController][Authorize][Route("api/foods")]`, iar `scope` vine ca text (`all`/`mine`) prin convertorul global.

- [ ] **Step 1: Scrie testele (pică)**
  - `ErrorHandlingTests`: `ForbiddenException("z")` → 403 cu titlul „z”; `NotFoundException("w")` → 404 cu titlul „w”.
  - `FoodsTests` (reset DB per test; seed-ul global există după migrare):
    - `Search_WithoutDiacritics_FindsSeededFood`: „piept pui fara piele” găsește alimentul global din seed (AC1);
    - `Create_ThenSearch_OnlyAuthorSeesIt` (AC2);
    - `Create_InconsistentEnergy_Returns201WithWarning` (AC6);
    - `Create_InvalidValues_Returns400` (theory: 901 kcal; P 60 + C 50; zaharuri > carbohidrați) (AC5);
    - `Update_OwnFood_Returns200`; `Archive_OwnFood_Returns204ThenGet404` (AC3);
    - `Archive_FoodReferencedByDiary_KeepsDiaryRowReadable`: inserează direct un `DiaryEntry` prin `ApplicationDbContext` din scope-ul factory-ului, arhivează alimentul prin API, apoi recitește `DiaryEntry` cu `Include(FoodItem)` (AC3);
    - `OtherUsersFood_Returns404ForGetPutDelete`; `GlobalFood_Returns403ForPutDelete` (AC4);
    - `SeededChicken_HasSaltConvertedFromSodium`: căutarea „piept de pui” → detalii → `per100g.saltGrams == 0.1625` (AC7);
    - `Search_WildcardCharacters_AreLiteral`: `query=%` și `query=_` → 200 cu 0 rezultate (Review Focus 1);
    - `Foods_WithoutToken_Returns401` (AC8).
  - `ResetDatabaseAsync` din factory șterge și `DiaryEntries` și alimentele **non-globale** (`CreatedByUserId != null`), păstrând seed-ul.
- [ ] **Step 2: Rulează** testele de integrare. Așteptat: eșecuri (404 / status-uri greșite).
- [ ] **Step 3: Implementează.**
- [ ] **Step 4: Rulează** `dotnet test`. Așteptat: toate trec.
- [ ] **Step 5: Commit** `feat(api): add food catalog endpoints`

---

### Task 5: Documentație

- [ ] **Step 1:**
  - `ADR-004-food-catalog.md` (D1–D6, calea spre catalogul comunitar: un câmp `Visibility` + moderare și rol Admin);
  - `architecture_and_contracts.md`: secțiunea 7 „Catalog” și ERD-ul `FOOD_ITEMS` (`SaltGrams`, `ArchivedAtUtc`, `SearchText`);
  - `sprint-02.md` (story + tabel AC → teste); `ROADMAP.md` (bifă Catalog);
  - fișierul local de întrebări: punctul slab #2 rezolvat în backend și întrebări noi (doar pe disc).
- [ ] **Step 2: Verificare:** `dotnet build && dotnet test` (toate trec); `git status --short | grep intrebari_comisie` nu afișează nimic.
- [ ] **Step 3: Commit** `docs: add ADR-004, catalog contract and roadmap progress`
