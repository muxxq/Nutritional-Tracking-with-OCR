# Planificatorul săptămânal și „Am mâncat asta”: plan de implementare

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `/api/plan` (mese planificate din alimente sau rețete, valori calculate la citire, starea zilei față de țintă) și transferul unei mese planificate în jurnal.

**Architecture:** `PlannedMeal` devine polimorfă ca `DiaryEntry` (exact o cheie, `CHECK`-uri în PostgreSQL). Valorile se calculează la citire în Domain (`PlannedMeal.Nutrients`, `PlanDayStatus`). Application adaugă `Planner/` după tiparul `Diary/`; transferul creează un `DiaryEntry` cu snapshot și leagă masa prin `DiaryEntryId` (`ON DELETE SET NULL`). WebApi expune `PlannerController`.

**Tech Stack:** .NET 8, EF Core 8 + Npgsql, MediatR 14, FluentValidation 12, xUnit + FluentAssertions + Moq, EF InMemory, Testcontainers (Podman).

**Spec:** `aplicatie/docs/specs/2026-10-16-meal-planner-design.md`. Secțiunile sunt referite ca §N, deciziile ca D1–D8.

## Global Constraints

- Comenzile `dotnet` rulează din `aplicatie/backend/`. Commit-urile se fac pe `feature/NUT-planner`, în *Conventional Commits*, **fără `Co-Authored-By`**.
- Testele de integrare cer `export DOCKER_HOST=unix:///run/user/$(id -u)/podman/podman.sock TESTCONTAINERS_RYUK_DISABLED=true`.
- `dotnet ef migrations add` cere `ConnectionStrings__DefaultConnection` setat (o valoare fictivă ajunge; comanda nu se conectează).
- Tiparul Arrange-Act-Assert; numire `Method_Scenario_ExpectedResult`.
- Valori fixe (§3.4): cantități ca în jurnal (`DiaryQuantity.MaxGrams = 5000`, `MaxServings = 50`); data planificată 01.01.2000 – azi + 366 zile; interval ≤ 31 zile inclusiv capetele; starea zilei: `< 0,95 × țintă` → `under`, `> 1,05 × țintă` → `over`.
- Rotunjire doar în DTO (`NutrientsDto.Rounded()`); starea se calculează din totalul nerotunjit.
- Mesajele pentru utilizator sunt în română; codul în engleză. Enum-urile circulă ca text camelCase.
- Timpul curent vine doar din `IDateTimeProvider`. Fără `ExecuteUpdateAsync`/`ExecuteDeleteAsync` în Application.
- `FoodAccess`/`RecipeAccess` au semnături identice: se apelează cu numele clasei acolo unde ambele sunt în scope.

## Review Focus

1. **`PUT` cu `quantityGrams` pe o masă-rețetă** → 400, nesalvat. Test `Update_RecipeMealWithGrams_ThrowsValidation` (Task 2).
2. **Transfer din nou după ștergerea intrării din jurnal** (`ON DELETE SET NULL` există doar în PostgreSQL) → 201. Test de integrare `Eat_AfterDiaryEntryDeleted_Returns201` (Task 5).
3. **Transferul unei mese al cărei aliment a fost arhivat după planificare** → intrare creată, nu 404 (D7). Test `Eat_ArchivedFood_CreatesEntry` (Task 3).
4. **Limitele intervalului:** `from == to` → o zi; 31 de zile → 200; 32 → 400; zilele fără mese apar. Teste `GetPlan_IncludesEmptyDays`, `Validate_Range32Days_IsInvalid`, `Validate_Range31Days_IsValid` (Task 4).
5. **Ștergerea unei mese deja transferate** păstrează intrarea din jurnal. Test `Delete_EatenMeal_KeepsDiaryEntry` (Task 2).

---

### Task 1: Domain și persistență

**Files:**
- Modify: `src/Domain/Entities/PlannedMeal.cs` (`PlannedMeal`)
- Create: `src/Domain/Nutrition/PlanDayStatus.cs`
- Modify: `src/Infrastructure/Persistence/ApplicationDbContext.cs` (blocul `PlannedMeal`)
- Create: migrarea `Planner`
- Modify: `tests/WebApi.IntegrationTests/NutriScanApiFactory.cs` (`ResetDatabaseAsync`: `PlannedMeals` primul)
- Test: `tests/Domain.UnitTests/Nutrition/PlanDayStatusTests.cs`, `tests/Domain.UnitTests/Entities/PlannedMealTests.cs`

**Interfaces:**
- Produces:
  ```csharp
  // PlannedMeal
  public double? QuantityGrams { get; set; }
  public double? Servings { get; set; }
  public Guid? DiaryEntryId { get; set; }  public DiaryEntry? DiaryEntry { get; set; }
  public bool IsRecipe => RecipeId is not null;
  public Macros Nutrients();   // food: Macros.Of(FoodItem!, QuantityGrams!.Value); recipe: RecipeNutrition.Of(Recipe!).PerServing * Servings!.Value
  // IsTransferredToDiary removed
  // Domain/Nutrition/PlanDayStatus.cs
  public enum PlanDayStatus { Under, Within, Over }
  public static class PlanDayStatusCalculator
  {
      public const double Tolerance = 0.05;
      public static PlanDayStatus Of(double totalKcal, double targetKcal);
  }
  ```

- [ ] **Step 1: Write the failing tests**
  - `PlanDayStatusTests`: theory `Of_Target1700_ClassifiesBoundaries` with `(1614.9, Under)`, `(1615, Within)`, `(1785, Within)`, `(1785.1, Over)`, `(0, Under)`.
  - `PlannedMealTests`: `Nutrients_Food_ScalesPer100g` (oats 389 kcal, 150 g → 583.5); `Nutrients_Recipe_PerServingTimesServings` (SRS recipe from `RecipeTestData`, 2 servings → 389).
- [ ] **Step 2: Run** `dotnet test tests/Domain.UnitTests` → build errors for the missing members.
- [ ] **Step 3: Implement** the Interfaces block. Floating-point limits: compare with `<` / `>` against `targetKcal * (1 ∓ Tolerance)`.
- [ ] **Step 4: Configure EF** (§4): optional `FoodItem` and `Recipe` relationships with `Restrict`; `HasOne(pm => pm.DiaryEntry).WithMany().HasForeignKey(pm => pm.DiaryEntryId).OnDelete(DeleteBehavior.SetNull)`; `HasIndex(pm => pm.DiaryEntryId).IsUnique()`; both checks via `ToTable(t => t.HasCheckConstraint(...))` with the SQL from §4.
- [ ] **Step 5: Generate the `Planner` migration** and read it: `QuantityGrams` nullable, `Servings` added, `IsTransferredToDiary` dropped, `DiaryEntryId` + FK `SetNull` + unique index, FKs to foods/recipes now `Restrict`, both checks, no data transformation.
- [ ] **Step 6: Update `ResetDatabaseAsync`**: delete `PlannedMeals` before `DiaryEntries`.
- [ ] **Step 7: Run** all three test projects → all PASS.
- [ ] **Step 8: Commit** `feat(domain): make planned meals polymorphic with day status`

---

### Task 2: Comenzile planificatorului

**Files:**
- Create: `src/Application/Planner/PlannerErrors.cs`, `PlannerAccess.cs`, `IPlannedMealFields.cs`, `PlannedMealFieldsValidator.cs`, `Dtos/PlannerDtos.cs`
- Create: `src/Application/Planner/Commands/{CreatePlannedMeal,UpdatePlannedMeal,DeletePlannedMeal}/…` (validator only for create/update)
- Test: `tests/Application.UnitTests/Planner/PlannerTestData.cs`, `PlannedMealValidatorTests.cs`, `PlannedMealCommandTests.cs`

**Interfaces:**
- Consumes: Task 1; `DiaryQuantity.Resolve`, `FoodAccess.LoadVisibleAsync`, `RecipeAccess.LoadVisibleAsync`, `DiaryFoodDto`, `DiaryRecipeDto`, `DiaryEntryKind`, `NutrientsDto`.
- Produces:
  ```csharp
  public static class PlannerErrors
  {
      public const string NotFound = "Masa planificată nu a fost găsită.";
      public const string ExactlyOneItem = "Alege fie un aliment, fie o rețetă.";
      public const string RecipeServingsOnly = "Rețetele se planifică în porții, nu în grame.";
      public const string AlreadyEaten = "Masa planificată a fost deja trecută în jurnal.";
      public const string FutureMeal = "O masă planificată după ziua de mâine nu poate fi trecută în jurnal.";
      public const string DateRange = "Data trebuie să fie între 01.01.2000 și un an de acum înainte.";
  }
  public interface IPlannedMealFields { DateOnly Date { get; } MealType MealType { get; } double? QuantityGrams { get; } double? Servings { get; } }
  public sealed record CreatePlannedMealCommand(Guid UserId, Guid? FoodId, Guid? RecipeId, DateOnly Date, MealType MealType,
      double? QuantityGrams, double? Servings) : IRequest<PlannedMealDto>, IPlannedMealFields;
  public sealed record UpdatePlannedMealCommand(Guid UserId, Guid PlannedMealId, DateOnly Date, MealType MealType,
      double? QuantityGrams, double? Servings) : IRequest<PlannedMealDto>, IPlannedMealFields;
  public sealed record DeletePlannedMealCommand(Guid UserId, Guid PlannedMealId) : IRequest;
  public sealed record PlannedMealDto(Guid Id, DiaryEntryKind Kind, DateOnly Date, MealType MealType, double? QuantityGrams,
      double? Servings, DiaryFoodDto? Food, DiaryRecipeDto? Recipe, NutrientsDto Nutrients, Guid? DiaryEntryId)
  { public static PlannedMealDto From(PlannedMeal meal); }   // NutrientsDto.From(meal.Nutrients()).Rounded()
  internal static class PlannerAccess
  {
      // Own meal with FoodItem, or Recipe.Ingredients.FoodItem, even archived; otherwise NotFoundException(PlannerErrors.NotFound).
      public static Task<PlannedMeal> LoadOwnAsync(this IApplicationDbContext db, Guid userId, Guid id, CancellationToken ct);
      public static IQueryable<PlannedMeal> WithItem(this IQueryable<PlannedMeal> meals);
      // food: DiaryQuantity.Resolve (ValidationException "Quantity"); recipe: grams → ValidationException("Quantity", RecipeServingsOnly)
      public static void Apply(this PlannedMeal meal, IPlannedMealFields fields);
  }
  ```

- [ ] **Step 1: Write the failing validator tests** (`PlannedMealFieldsValidator` on `IPlannedMealFields` + create validator): `Validate_FoodAndRecipe_IsInvalid`, `Validate_NeitherFoodNorRecipe_IsInvalid` (message `ExactlyOneItem`), `Validate_RecipeWithGrams_IsInvalid` (`RecipeServingsOnly`), `Validate_BothQuantities_IsInvalid`, `Validate_Grams5001_IsInvalid`, `Validate_Servings51_IsInvalid`, `Validate_DateBefore2000_IsInvalid`, `Validate_Date367DaysAhead_IsInvalid`, `Validate_Date366DaysAhead_IsValid`.
- [ ] **Step 2: Write the failing handler tests** (InMemory via `AuthTestContext`, foods from `DiaryTestData`, recipes via `CreateRecipeCommandHandler`):
  - `Create_Recipe_ComputesFromCurrentRecipe` (SRS recipe, 2 servings → `Kind == Recipe`, `Nutrients.Calories == 389`, `QuantityGrams == null`, stored `UserId == me`, `CreatedAtUtc == ctx.Now`).
  - `Create_FoodInServings_ConvertsToGrams` (oats, 1.5 servings → `QuantityGrams == 60`, `Nutrients.Calories == 233.4`).
  - `Create_FoodServingsWithoutServingSize_ThrowsValidation`; `Create_OtherUsersFood_ThrowsNotFound`; `Create_ArchivedRecipe_ThrowsNotFound`.
  - `Update_ChangesDateMealAndServings`; `Update_RecipeMealWithGrams_ThrowsValidation` (Review Focus 1; key `"Quantity"`, nothing saved); `Update_ArchivedFoodMeal_Succeeds`; `Update_OtherUsersMeal_ThrowsNotFound`.
  - `Delete_RemovesMeal`; `Delete_OtherUsersMeal_ThrowsNotFound`; `Delete_EatenMeal_KeepsDiaryEntry` (Review Focus 5; arrange a meal with `DiaryEntryId` set to a saved entry).
- [ ] **Step 3: Run** `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~Planner"` → build errors.
- [ ] **Step 4: Implement.** Create branches on `RecipeId` like `AddDiaryEntryCommandHandler`, sets the navigation (`FoodItem` / `Recipe`) so `From` works, and sets `CreatedAtUtc = clock.UtcNow`; update sets `UpdatedAtUtc`. Validator date bounds use `clock.UtcNow` (`DateOnly.FromDateTime(...).AddDays(366)`).
- [ ] **Step 5: Run** `dotnet test tests/Application.UnitTests` → all PASS.
- [ ] **Step 6: Commit** `feat(planner): add planned meal commands`

---

### Task 3: „Am mâncat asta”

**Files:**
- Create: `src/Application/Planner/Commands/EatPlannedMeal/EatPlannedMealCommand.cs`, `…Handler.cs`
- Test: `tests/Application.UnitTests/Planner/EatPlannedMealTests.cs`

**Interfaces:**
- Consumes: `PlannerAccess.LoadOwnAsync`, `PlannerErrors` (Task 2); `DiaryEntry.RecalculateNutrients(FoodItem|Recipe)`, `DiaryEntryDto.From(entry)`, `ConflictException`.
- Produces: `public sealed record EatPlannedMealCommand(Guid UserId, Guid PlannedMealId) : IRequest<DiaryEntryDto>;`

- [ ] **Step 1: Write the failing tests**
  - `Eat_Recipe_CreatesSnapshotEntry` (SRS recipe, 2 servings, planned today at lunch → `Kind == Recipe`, `Servings == 2`, `Nutrients.Calories == 389`, `MealType == Lunch`, `Date == planned`; meal's `DiaryEntryId == dto.Id`; entry `CreatedAtUtc == ctx.Now`).
  - `Eat_Food_CopiesGramsAndServings` (oats 1.5 servings → entry `QuantityGrams == 60`, `Servings == 1.5`).
  - `Eat_Twice_ThrowsConflict` (message `AlreadyEaten`, still one diary entry).
  - `Eat_DayAfterTomorrow_ThrowsValidation` (message `FutureMeal`); `Eat_Tomorrow_Succeeds`.
  - `Eat_ArchivedFood_CreatesEntry` (Review Focus 3).
  - `Eat_OtherUsersMeal_ThrowsNotFound`.
  - `EditFoodAfterEating_KeepsEntrySnapshot`.
- [ ] **Step 2: Run** → build errors.
- [ ] **Step 3: Implement.** Check order: not found → already eaten (409) → future date (400, `ValidationException` key `"Date"`). Build the `DiaryEntry` directly (not through `AddDiaryEntryCommand`, which rejects archived items), copy `QuantityGrams`/`Servings`, set `FoodItem`/`Recipe` navigation, recalculate, add, set `meal.DiaryEntry = entry`, one `SaveChangesAsync`.
- [ ] **Step 4: Run** `dotnet test tests/Application.UnitTests` → all PASS.
- [ ] **Step 5: Commit** `feat(planner): log a planned meal as eaten`

---

### Task 4: Interogările planului

**Files:**
- Create: `src/Application/Planner/Queries/GetPlannedMeal/…`, `src/Application/Planner/Queries/GetPlan/GetPlanQuery.cs`, `…Handler.cs`, `…Validator.cs`
- Modify: `src/Application/Planner/Dtos/PlannerDtos.cs` (plan DTOs)
- Test: `tests/Application.UnitTests/Planner/GetPlanTests.cs`

**Interfaces:**
- Consumes: Tasks 1–2; `ProfileLoader.LoadUserAsync`, `ProfileLoader.CalculateTargets`.
- Produces:
  ```csharp
  public sealed record GetPlannedMealQuery(Guid UserId, Guid PlannedMealId) : IRequest<PlannedMealDto>;
  public sealed record GetPlanQuery(Guid UserId, DateOnly From, DateOnly To) : IRequest<PlanDto>;
  public sealed record PlanMealDto(MealType MealType, IReadOnlyList<PlannedMealDto> Items, NutrientsDto Totals);
  public sealed record PlanDayDto(DateOnly Date, IReadOnlyList<PlanMealDto> Meals, NutrientsDto Totals, NutrientsDto Targets, PlanDayStatus Status);
  public sealed record PlanDto(DateOnly From, DateOnly To, IReadOnlyList<PlanDayDto> Days, bool IsProfileComplete);
  public sealed class GetPlanQueryValidator { public const int MaxDays = 31; }
  ```

- [ ] **Step 1: Write the failing tests**
  - `GetPlan_IncludesEmptyDays` (Review Focus 4; 7-day range, meals on day 2 only → 7 days, 4 meals each, empty days `Totals.Calories == 0`, `Status == Under`).
  - `GetPlan_SumsMealsAndDay_StatusAgainstTarget` (R1 profile from `ProfileTestData`, target 1700: plan 1650 kcal on one day → `Within`; 1800 → `Over`). Use own foods with round values (e.g. 100 kcal/100 g) to hit totals exactly.
  - `GetPlan_OrdersItemsByCreatedAt`; `GetPlan_ExcludesOtherUsersAndOutOfRange`.
  - `GetPlan_ReflectsCurrentFoodValues` (AC3: edit food after planning → new total).
  - `GetPlannedMeal_OtherUsers_ThrowsNotFound`.
  - Validator: `Validate_FromAfterTo_IsInvalid`, `Validate_Range31Days_IsValid`, `Validate_Range32Days_IsInvalid`, `Validate_SingleDay_IsValid`.
- [ ] **Step 2: Run** → build errors.
- [ ] **Step 3: Implement.** One query with `WithItem()` over `UserId == me && PlanDate >= From && PlanDate <= To`, ordered by `CreatedAtUtc`; group in memory per day (every date `From..To`) and meal (`Enum.GetValues<MealType>()`); sums from unrounded `Nutrients()`; status from unrounded day total vs `targets.Calories.Value`; round once in the DTO. Range length = `To.DayNumber - From.DayNumber + 1`.
- [ ] **Step 4: Run** `dotnet test tests/Application.UnitTests` → all PASS.
- [ ] **Step 5: Commit** `feat(planner): add plan range and planned meal queries`

---

### Task 5: API și teste de integrare

**Files:**
- Create: `src/WebApi/Controllers/PlannerController.cs`, `src/WebApi/Contracts/Planner/{CreatePlannedMealRequest,UpdatePlannedMealRequest}.cs`
- Test: `tests/WebApi.IntegrationTests/Planner/PlannerTests.cs`

**Interfaces:**
- Consumes: commands and queries from Tasks 2–4.
- Produces: routes and status codes exactly as the §5 table, `[Route("api/plan")]`; `GET` binds `[FromQuery] DateOnly from, [FromQuery] DateOnly to`; `POST items` → `Created($"/api/plan/items/{id}", dto)`; `POST items/{id}/eat` → `Created($"/api/diary/entries/{entry.Id}", entry)`.

- [ ] **Step 1: Write the failing integration tests** (one or more per AC):
  - AC1 `Create_Recipe_Returns201WithComputedValues` (Location, `nutrients.calories == 389`, `kind == "recipe"`).
  - AC2 `GetPlan_WeekWithStatus` (R1 profile; days length 7; status `within` on a 1650 kcal day; empty day present).
  - AC3 `EditFood_ChangesPlan`.
  - AC4 `Eat_CreatesDiaryEntry_Returns201` (Location `/api/diary/entries/…`; diary day shows it; planned meal `diaryEntryId` set).
  - AC5 `Eat_Twice_Returns409`; `Eat_AfterDiaryEntryDeleted_Returns201` (Review Focus 2).
  - AC6 `Eat_FutureMeal_Returns400`.
  - AC7 `ArchivedRecipe_StaysInPlanAndCanBeEaten`; `Create_ArchivedFood_Returns404`.
  - AC8 `OtherUsersMeal_Returns404Everywhere`.
  - AC9 `Create_InvalidBody_Returns400` (theory: both ids, recipe with grams, servings 0); `GetPlan_Range32Days_Returns400`; `CheckConstraints_RejectDirectSqlInserts` (all four rejected combinations, `SqlState == "23514"`, NULLs via typed `NpgsqlParameter`).
  - AC10 `Plan_WithoutToken_Return401`.
- [ ] **Step 2: Run** `dotnet test tests/WebApi.IntegrationTests --filter "FullyQualifiedName~Planner"` → 404s / failures.
- [ ] **Step 3: Implement** the controller and request records.
- [ ] **Step 4: Run** `dotnet test` → all PASS, no new build warnings.
- [ ] **Step 5: Commit** `feat(api): add meal planner endpoints`

---

### Task 6: Documentație

**Files:**
- Create: `aplicatie/docs/adr/ADR-007-meal-planner.md` (format ADR-006: context, D1–D8 cu alternative respinse, consecințe, riscuri: transfer simultan, ținte curente)
- Modify: `aplicatie/docs/architecture_and_contracts.md` (secțiunea 10 Planificator; ERD `PLANNED_MEALS`: `Servings`, `DiaryEntryId`, fără `IsTransferredToDiary`; relația `DIARY_ENTRIES ||--o| PLANNED_MEALS`)
- Modify: `ROADMAP.md` (bifează „Rețetar & Planificator”; pasul următor: lista de cumpărături FR-12)

- [ ] **Step 1: Write the documents**; every number from the spec (1615 / 1785 pentru ținta 1700; 389 kcal pentru 2 porții) and the final test counts from the Task 5 run.
- [ ] **Step 2: Check** `git ls-files | grep intrebari_comisie` → no output.
- [ ] **Step 3: Commit** `docs: add ADR-007, planner contract and roadmap progress`
