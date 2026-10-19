# Jurnalul alimentar: plan de implementare

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `/api/diary` (zi agregată, CRUD pe intrări, alimente recente), cu cantitate în grame sau porții, snapshot nutrițional la salvare și totaluri zilnice față de ținte.

**Architecture:** conversia cantității și calculul snapshot-ului stau în Domain (funcții pure). Application are un validator comun pentru câmpurile intrării, handlere MediatR care aplică regula de proprietate (404) și un query de citire în forma ecranului (CQRS). WebApi expune `DiaryController`.

**Tech Stack:** .NET 8, EF Core 8 + Npgsql, MediatR 14, FluentValidation 12, xUnit + FluentAssertions + Moq, EF InMemory, Testcontainers (Podman).

**Spec:** `aplicatie/docs/specs/2026-10-16-food-diary-design.md`. Secțiunile sunt referite ca §N.

## Global Constraints

- Comenzile `dotnet` rulează din `aplicatie/backend/`. Commit-urile se fac pe `feature/NUT-food-diary`, în *Conventional Commits*, **fără `Co-Authored-By`**.
- Testele de integrare cer `export DOCKER_HOST=unix:///run/user/$(id -u)/podman/podman.sock TESTCONTAINERS_RYUK_DISABLED=true`.
- Tiparul Arrange-Act-Assert; numire `Method_Scenario_ExpectedResult`.
- Valori fixe:
  - cantitate: `0 < grame ≤ 5000`; `0 < porții ≤ 50`; după conversie `≤ 5000 g`;
  - dată: `2000-01-01 ≤ date ≤ azi (UTC) + 1 zi`, „azi” din `IDateTimeProvider.UtcNow`;
  - `limit` pentru recente: `1–30`, implicit 10;
  - rotunjire în DTO: `Math.Round(x, 1, MidpointRounding.AwayFromZero)`; în baza de date valorile sunt nerotunjite.
- Enum-urile circulă ca text camelCase (`breakfast`, `lunch`, `dinner`, `snack`), prin convertorul global existent.
- Mesajele pentru utilizator sunt în română; codul în engleză.
- Timpul curent vine doar din `IDateTimeProvider` (și `CreatedAtUtc`/`UpdatedAtUtc` ale intrărilor se setează din el).
- Fără `ExecuteUpdateAsync`/`ExecuteDeleteAsync` în Application (testele InMemory).

## Review Focus

1. **Limita datei** (azi + 1 valid, azi + 2 invalid, 1999-12-31 invalid), pe ceasul controlat, nu pe `DateTime.UtcNow`. Testele sunt în Task 2 (`Validate_DateTomorrow_IsValid`, `Validate_DateInTwoDays_IsInvalid`, `Validate_DateBefore2000_IsInvalid`).
2. **Porții care depășesc 5000 g după conversie** (50 porții × 200 g) → 400, nu salvare. Testul `Add_ServingsAboveLimitAfterConversion_ThrowsValidation` e în Task 2.
3. **Dată malformată în rută** (`/api/diary/days/2026-13-01`, `/api/diary/days/azi`) → 400, nu 500. Testul `GetDay_MalformedDate_Returns400` e în Task 4.
4. **Zile și utilizatori vecini:** intrările de ieri, de mâine și ale altui utilizator nu intră în totalul zilei. Testul `GetDay_IgnoresOtherDaysAndOtherUsers` e în Task 3.
5. **Mutarea unei intrări pe altă zi prin `PUT`** respectă aceeași limită de dată ca `POST`, iar ziua veche nu o mai conține. Testele `Validate_DateInTwoDays_IsInvalid` (comun, Task 2) și `Update_MoveToAnotherDay_LeavesOldDay` (Task 4).

---

### Task 1: Domain și persistență: `DiaryQuantity`, snapshot nerotunjit, `Servings`

**Files:**
- Create: `src/Domain/Nutrition/DiaryQuantity.cs`
- Modify: `src/Domain/Entities/DiaryEntry.cs`
- Modify: `src/Infrastructure/Persistence/ApplicationDbContext.cs` (doar dacă e nevoie de configurare explicită pentru `Servings`)
- Create: migrarea `DiaryServings` (generată)
- Test: `tests/Domain.UnitTests/Nutrition/DiaryQuantityTests.cs`, `tests/Domain.UnitTests/Entities/DiaryEntryTests.cs` (rescris)

**Interfaces:**
- Produces:
  ```csharp
  public sealed record DiaryQuantityResult(double QuantityGrams, double? Servings, string? Error)
  {
      public bool IsSuccess => Error is null;
  }
  public static class DiaryQuantity
  {
      public const double MaxGrams = 5000;
      public const double MaxServings = 50;
      public const string ExactlyOneQuantity = "Introdu fie cantitatea în grame, fie numărul de porții.";
      public const string NoServingSize = "Alimentul nu are o porție definită; introdu cantitatea în grame.";
      public const string TooMuch = "Cantitatea totală nu poate depăși 5000 g.";
      public static DiaryQuantityResult Resolve(double? grams, double? servings, double? servingSizeGrams); // §3.2
  }
  // DiaryEntry: double? Servings { get; set; }
  public void RecalculateNutrients(FoodItem food); // §3.1, fără rotunjire; înlocuiește varianta fără parametri
  ```

- [ ] **Step 1: Scrie testele (pică)**
  - `DiaryQuantityTests`:
    - `Resolve_Grams_ReturnsGramsWithoutServings`: `(150, null, 40)` → `(150, null)`;
    - `Resolve_Servings_ConvertsWithServingSize`: `(null, 1.5, 40)` → `(60, 1.5)`;
    - `Resolve_BothQuantities_ReturnsError` și `Resolve_NoQuantity_ReturnsError` → `ExactlyOneQuantity`;
    - `Resolve_ServingsWithoutServingSize_ReturnsError` → `NoServingSize`;
    - `Resolve_ServingsAboveLimitAfterConversion_ReturnsError`: `(null, 50, 200)` → `TooMuch`; `(null, 25, 200)` → succes 5000 g.
  - `DiaryEntryTests` (rescrie testul existent):
    - `RecalculateNutrients_ScalesPer100g`: ovăz 389 / 16,9 / 66,3 / 6,9, 60 g → 233,4 / 10,14 / 39,78 / 4,14, comparate cu `BeApproximately(…, 1e-9)` (dovedește lipsa rotunjirii);
    - `RecalculateNutrients_UsesGivenFood_NotNavigation`: `entry.FoodItem` rămâne null, apelul cu alimentul explicit calculează valorile.
- [ ] **Step 2: Rulează** `dotnet test tests/Domain.UnitTests`. Așteptat: eroare de compilare.
- [ ] **Step 3: Implementează** `DiaryQuantity` și modificările din `DiaryEntry` (scoate `Math.Round` și verificarea `FoodItem == null`).
- [ ] **Step 4: Generează migrarea** `dotnet ef migrations add DiaryServings -p src/Infrastructure -s src/WebApi -o Persistence/Migrations`. `Up` conține doar `AddColumn<double>("Servings", "DiaryEntries", nullable: true)`; `Down` face `DropColumn`.
- [ ] **Step 5: Rulează** `dotnet build` și `dotnet test`. Așteptat: 0 erori, toate trec (integrarea aplică migrarea pe Postgres).
- [ ] **Step 6: Commit** `feat(domain): add diary quantity resolution and unrounded snapshots`

---

### Task 2: Application: comenzi pe intrări, validator, DTO-uri

**Files:**
- Create: `src/Application/Diary/IDiaryEntryFields.cs`, `DiaryEntryFieldsValidator.cs`, `DiaryErrors.cs`, `DiaryAccess.cs`
- Create: `src/Application/Diary/Dtos/DiaryDtos.cs`
- Create: `src/Application/Diary/Commands/AddDiaryEntry/*` (command, handler, validator), `Commands/UpdateDiaryEntry/*`, `Commands/DeleteDiaryEntry/*`
- Create: `src/Application/Diary/Queries/GetDiaryEntry/*`
- Test: `tests/Application.UnitTests/Diary/DiaryEntryFieldsValidatorTests.cs`, `DiaryCommandHandlerTests.cs`, `DiaryTestData.cs`

**Interfaces:**
- Consumes: Task 1; `FoodAccess.LoadVisibleAsync` (catalog); `IDateTimeProvider`.
- Produces:
  ```csharp
  public interface IDiaryEntryFields { DateOnly Date { get; } MealType MealType { get; } double? QuantityGrams { get; } double? Servings { get; } }
  public sealed class DiaryEntryFieldsValidator : AbstractValidator<IDiaryEntryFields> // §3.3; ctor(IDateTimeProvider clock)
  public static class DiaryErrors { public const string NotFound = "Intrarea din jurnal nu a fost găsită."; }

  public sealed record NutrientsDto(double Calories, double Proteins, double Carbs, double Fats)
  {
      public static readonly NutrientsDto Zero;
      public NutrientsDto Rounded();                           // rotunjirea din Global Constraints
      public static NutrientsDto operator +(NutrientsDto a, NutrientsDto b);
      public static NutrientsDto operator -(NutrientsDto a, NutrientsDto b);
      public static NutrientsDto Of(DiaryEntry e);             // valorile Calculated*, nerotunjite
  }
  public sealed record DiaryFoodDto(Guid Id, string Name, string? Brand, bool IsArchived);
  public sealed record DiaryEntryDto(Guid Id, DateOnly Date, MealType MealType, double QuantityGrams, double? Servings,
      DiaryFoodDto Food, NutrientsDto Nutrients)
  { public static DiaryEntryDto From(DiaryEntry entry, FoodItem food); } // Nutrients = Of(entry).Rounded()

  public sealed record AddDiaryEntryCommand(Guid UserId, Guid FoodId, DateOnly Date, MealType MealType,
      double? QuantityGrams, double? Servings) : IRequest<DiaryEntryDto>, IDiaryEntryFields;
  public sealed record UpdateDiaryEntryCommand(Guid UserId, Guid EntryId, DateOnly Date, MealType MealType,
      double? QuantityGrams, double? Servings) : IRequest<DiaryEntryDto>, IDiaryEntryFields;
  public sealed record DeleteDiaryEntryCommand(Guid UserId, Guid EntryId) : IRequest;
  public sealed record GetDiaryEntryQuery(Guid UserId, Guid EntryId) : IRequest<DiaryEntryDto>;

  internal static class DiaryAccess
  {
      // Intrarea utilizatorului, cu FoodItem inclus (și arhivat); altfel NotFoundException(DiaryErrors.NotFound).
      public static Task<DiaryEntry> LoadOwnAsync(this IApplicationDbContext db, Guid userId, Guid entryId, CancellationToken ct);
  }
  ```
  Validatoarele `AddDiaryEntryCommandValidator` și `UpdateDiaryEntryCommandValidator` fac `Include(new DiaryEntryFieldsValidator(clock))`.
  Handlerele `Add`/`Update` apelează `DiaryQuantity.Resolve(…, food.ServingSizeGrams)`. Pe eroare aruncă `ValidationException(new[] { new ValidationFailure("Quantity", result.Error) })`, ca `UpdateTargetsCommandHandler`.

- [ ] **Step 1: Scrie testele (pică)**
  - `DiaryEntryFieldsValidatorTests` (ceas fix 2026-10-16 12:00 UTC):
    - `Validate_DateTomorrow_IsValid` (2026-10-17); `Validate_DateInTwoDays_IsInvalid` (2026-10-18); `Validate_DateBefore2000_IsInvalid` (1999-12-31); 2000-01-01 valid;
    - grame 0 / −1 / 5000,1 invalide, 5000 valid; porții 0 / 50,1 invalide, 50 valid;
    - ambele cantități sau niciuna → invalid;
    - `MealType` `(MealType)99` → invalid.
  - `DiaryCommandHandlerTests` (`AuthTestContext`, ovăz propriu cu porție 40 g, `DiaryTestData`):
    - `Add_Grams_StoresSnapshot` (100 g → 389 kcal; `CreatedAtUtc == ctx.Now`);
    - `Add_Servings_StoresGramsAndServings` (1,5 → 60 g, `Servings 1.5`, DTO 233,4 / 10,1 / 39,8 / 4,1);
    - `Add_ServingsForFoodWithoutServingSize_ThrowsValidation`;
    - `Add_ServingsAboveLimitAfterConversion_ThrowsValidation` (aliment cu porție 200 g, 50 porții);
    - `Add_OtherUsersFood_ThrowsNotFound`; `Add_ArchivedFood_ThrowsNotFound`;
    - `Update_FoodChangedAfterAdd_RecalculatesFromCurrentFood` (AC4: schimbă `Calories` la 400 → intrarea veche rămâne 389 până la `Update`, apoi 400 la 100 g);
    - `Update_EntryWithArchivedFood_Succeeds` (AC6);
    - `Update_OtherUsersEntry_ThrowsNotFound`; `Delete_OtherUsersEntry_ThrowsNotFound`; `Get_OtherUsersEntry_ThrowsNotFound`;
    - `Delete_OwnEntry_RemovesIt`.
- [ ] **Step 2: Rulează** `dotnet test tests/Application.UnitTests`. Așteptat: eroare de compilare.
- [ ] **Step 3: Implementează.** `Update` setează `UpdatedAtUtc = clock.UtcNow`; alimentul unei intrări nu se schimbă (D7).
- [ ] **Step 4: Rulează** `dotnet test tests/Application.UnitTests`. Așteptat: toate trec.
- [ ] **Step 5: Commit** `feat(diary): add diary entry commands`

---

### Task 3: Application: ziua agregată și alimentele recente

**Files:**
- Create: `src/Application/Diary/Queries/GetDiaryDay/*` (query și handler; fără validator: la citire orice dată bine formată e acceptată)
- Create: `src/Application/Diary/Queries/GetRecentFoods/*` (query, handler, validator)
- Modify: `src/Application/Diary/Dtos/DiaryDtos.cs`
- Test: `tests/Application.UnitTests/Diary/GetDiaryDayTests.cs`, `GetRecentFoodsTests.cs`

**Interfaces:**
- Consumes: Task 2 (`NutrientsDto`, `DiaryEntryDto`, `DiaryFoodDto`); `ProfileLoader.LoadUserAsync`, `ProfileLoader.CalculateTargets`.
- Produces:
  ```csharp
  public sealed record DiaryMealDto(MealType MealType, IReadOnlyList<DiaryEntryDto> Entries, NutrientsDto Totals);
  public sealed record DiaryDayDto(DateOnly Date, IReadOnlyList<DiaryMealDto> Meals, NutrientsDto Totals,
      NutrientsDto Targets, NutrientsDto Remaining, bool IsProfileComplete);
  public sealed record RecentFoodDto(DiaryFoodDto Food, double LastQuantityGrams, double? LastServings, DateTime LastUsedAtUtc);

  public sealed record GetDiaryDayQuery(Guid UserId, DateOnly Date) : IRequest<DiaryDayDto>;
  public sealed record GetRecentFoodsQuery(Guid UserId, int Limit = 10) : IRequest<IReadOnlyList<RecentFoodDto>>;
  ```
  - Ziua: mesele în ordinea `Enum.GetValues<MealType>()` (Breakfast=1…Snack=4), intrările după `CreatedAtUtc`. Totalurile se adună din `NutrientsDto.Of` nerotunjit, iar `Rounded()` se aplică doar la construirea DTO-ului (D4). `Targets` vin din `CalculateTargets(user, DateOnly.FromDateTime(clock.UtcNow))` (D5). `Remaining = Targets − Totals`, calculat înainte de rotunjire.
  - Recente: ultimele 200 de intrări ale utilizatorului cu aliment nearhivat, ordonate descrescător după `CreatedAtUtc`, grupate în memorie după `FoodItemId` (prima apariție = cea mai nouă), apoi `Take(limit)`. Fereastra de 200 evită `GroupBy` + `First` netraductibil și ține interogarea mărginită.
  - `GetRecentFoodsQueryValidator`: `Limit` 1–30.

- [ ] **Step 1: Scrie testele (pică)**
  - `GetDiaryDayTests` (profil R1 aplicat cu `UpdateProfileCommandHandler` și `ProfileTestData.R1(userId)`, ținte 1700 / 124 / 195 / 47):
    - `GetDay_Empty_ReturnsFourEmptyMealsAndZeroTotals` (ordinea `Breakfast, Lunch, Dinner, Snack`; `Remaining == Targets`);
    - `GetDay_SumsMealsAndDay`: 2 intrări la mic dejun, 1 la cină → subtotaluri pe masă, total pe zi, `Remaining = Targets − Totals`;
    - `GetDay_SumsBeforeRounding`: 3 intrări de câte 0,04 g proteine → `Totals.Proteins == 0.1` (nu 0);
    - `GetDay_IgnoresOtherDaysAndOtherUsers` (Review Focus 4);
    - `GetDay_EntryWithArchivedFood_IsListedAsArchived`;
    - `GetDay_IncompleteProfile_ReturnsDefaultTargets` (`IsProfileComplete == false`).
  - `GetRecentFoodsTests`:
    - `Recent_DistinctByFood_NewestFirst_WithLastQuantity` (ovăz de 2 ori, ultima 1,5 porții; pui o dată → [ovăz (60 g, 1,5), pui]);
    - `Recent_ExcludesArchivedFoods`;
    - `Recent_RespectsLimit`;
    - `Recent_OnlyOwnEntries`;
    - validator: `Limit` 0 / 31 invalide.
- [ ] **Step 2: Rulează** `dotnet test tests/Application.UnitTests`. Așteptat: eroare de compilare.
- [ ] **Step 3: Implementează.**
- [ ] **Step 4: Rulează** `dotnet test tests/Application.UnitTests`. Așteptat: toate trec.
- [ ] **Step 5: Commit** `feat(diary): add diary day and recent foods queries`

---

### Task 4: WebApi: `DiaryController` și testele de integrare

**Files:**
- Create: `src/WebApi/Controllers/DiaryController.cs`, `src/WebApi/Contracts/Diary/AddDiaryEntryRequest.cs`, `UpdateDiaryEntryRequest.cs`
- Test: `tests/WebApi.IntegrationTests/Diary/DiaryTests.cs`

**Interfaces:**
- Consumes: Tasks 2–3.
- Produces:
  ```csharp
  public sealed record AddDiaryEntryRequest(Guid FoodId, DateOnly Date, MealType MealType, double? QuantityGrams, double? Servings);
  public sealed record UpdateDiaryEntryRequest(DateOnly Date, MealType MealType, double? QuantityGrams, double? Servings);
  // [ApiController][Authorize][Route("api/diary")]
  // GET days/{date}, GET entries/{id:guid}, POST entries (CreatedAtAction → GET entries/{id}),
  // PUT entries/{id:guid}, DELETE entries/{id:guid}, GET recent-foods?limit=10
  ```
  Același tipar ca `FoodsController`: `User.GetUserId() is { } userId ? … : Unauthorized()`; `[ProducesResponseType]` pe fiecare acțiune.

- [ ] **Step 1: Scrie testele (pică)** în `DiaryTests` (`[Collection(IntegrationTestCollection.Name)]`, `ResetDatabaseAsync` per test, `today = DateOnly.FromDateTime(DateTime.UtcNow)`, ovăz propriu creat prin `POST /api/foods` cu `servingSizeGrams = 40`):
  - `Add_Grams_Returns201WithLocationAndSnapshot` (AC1);
  - `Add_Servings_ConvertsToGrams` (1,5 → 60 g, 233,4 kcal) și `Add_ServingsForSeededOats_Returns400` (ovăzul din seed nu are porție) (AC2);
  - `GetDay_ReturnsFourMealsTotalsAndTargets` (profil R1 prin `PUT /api/profile`; `meals[*].mealType == ["breakfast","lunch","dinner","snack"]`; `targets.calories == 1700`) (AC3);
  - `EditFood_DoesNotChangeExistingEntry` (AC4);
  - `Update_ChangesQuantityMealAndDate_Returns200`; `Update_MoveToAnotherDay_LeavesOldDay`; `Delete_Returns204ThenGet404` (AC5);
  - `OtherUsersEntry_Returns404ForGetPutDelete`; `Add_OtherUsersFood_Returns404`; `ArchivedFoodEntry_StaysInDayAndIsEditable` (AC6);
  - `Add_InvalidRequest_Returns400` (theory: ambele cantități; niciuna; 5001 g; `mealType = "brunch"`; dată azi + 2) și `GetDay_MalformedDate_Returns400` (`2026-13-01`, `azi`) (AC7);
  - `RecentFoods_ReturnsDistinctNewestFirst` (AC8);
  - `Diary_WithoutToken_Returns401` (AC9).
- [ ] **Step 2: Rulează** testele de integrare. Așteptat: eșecuri (404 pe rute inexistente).
- [ ] **Step 3: Implementează.**
- [ ] **Step 4: Rulează** `dotnet test`. Așteptat: toate trec.
- [ ] **Step 5: Commit** `feat(api): add food diary endpoints`

---

### Task 5: Documentație

- [ ] **Step 1:**
  - `ADR-005-food-diary.md` (D1–D7 din §2; limitarea D5 și legătura cu FR-08);
  - `architecture_and_contracts.md`: secțiunea 8 „Jurnalul alimentar” (tabelul de rute din §5, forma `DiaryDayDto`) și ERD-ul `DIARY_ENTRIES` (`Servings`, `UpdatedAtUtc`); fluxul din diagrama de secvență folosește deja `POST /api/diary/entries`;
  - `sprint-02.md` (story + tabel AC → teste, numărul nou de teste); `ROADMAP.md` (bifa „Modul Jurnal Alimentar”; bifa „Reguli de validare energetică” rămâne cum este);
  - fișierul local de întrebări: întrebări noi despre snapshot, rotunjire și țintele zilelor trecute (doar pe disc).
- [ ] **Step 2: Verificare:** `dotnet build && dotnet test` (toate trec); `git status --short | grep intrebari_comisie` nu afișează nimic.
- [ ] **Step 3: Commit** `docs: add ADR-005, diary contract and roadmap progress`
