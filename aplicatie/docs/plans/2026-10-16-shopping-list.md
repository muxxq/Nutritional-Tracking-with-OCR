# Lista de cumpărături agregată: plan de implementare

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `/api/shopping-list` (generare din plan pe interval, agregare după aliment cu scalarea rețetelor, articole manuale, bifare) și categoria alimentelor.

**Architecture:** agregarea stă în Domain (`ShoppingListBuilder`, funcție pură peste mese planificate). `ShoppingListItem` devine articol generat (aliment + grame) sau manual (text), garantat de un `CHECK`. Application adaugă `ShoppingList/` după tiparul `Planner/`; `FoodItem` primește `Category`, expusă prin contractul alimentelor.

**Tech Stack:** .NET 8, EF Core 8 + Npgsql, MediatR 14, FluentValidation 12, xUnit + FluentAssertions + Moq, EF InMemory, Testcontainers (Podman).

**Spec:** `aplicatie/docs/specs/2026-10-16-shopping-list-design.md`. Secțiunile sunt referite ca §N, deciziile ca D1–D8.

## Global Constraints

- Comenzile `dotnet` rulează din `aplicatie/backend/`. Commit-urile se fac pe `feature/NUT-shopping-list`, în *Conventional Commits*, **fără `Co-Authored-By`**.
- Teste de integrare: `export DOCKER_HOST=unix:///run/user/$(id -u)/podman/podman.sock TESTCONTAINERS_RYUK_DISABLED=true`. `dotnet ef migrations add` cere `ConnectionStrings__DefaultConnection` (valoare fictivă).
- Arrange-Act-Assert; `Method_Scenario_ExpectedResult`. Mesaje pentru utilizator în română, cod în engleză, enum-uri ca text camelCase.
- Valori fixe: nume articol manual 1–200 după trim; cantitate text ≤ 50; interval ca la plan (`GetPlanQueryValidator.MaxDays = 31`, domeniul de date al planificării); grame rotunjite în sus la întreg doar în DTO (`Math.Ceiling`).
- Ordinea categoriilor = ordinea enum-ului `FoodCategory`: `FruitsVegetables, MeatFish, Dairy, Grains, Pantry, Beverages, Other` (valori implicite 0–6, deci `Other = 6`); implicitul pe entitate și în coloană este `Other`.
- Timpul curent doar din `IDateTimeProvider`. Fără `ExecuteUpdateAsync`/`ExecuteDeleteAsync` în Application.

## Review Focus

1. **Același aliment dintr-o rețetă și dintr-o masă-aliment** → un singur articol cu suma. Test `Build_SameFoodFromRecipeAndFoodMeal_SumsIntoOneItem` (Task 1).
2. **Plan gol** (sau doar mese mâncate) → 400, iar articolele generate anterior rămân. Test `Generate_NothingPlanned_ThrowsAndKeepsList` (Task 3).
3. **Regenerare** → articolele generate vechi dispar, cele manuale (bifate sau nu) rămân. Test `Generate_Twice_ReplacesGeneratedKeepsManual` (Task 3).
4. **Un aliment arhivat după planificare** intră totuși în listă. Test `Generate_ArchivedFoodInPlan_IsIncluded` (Task 3).
5. **Categoria implicită**: un aliment creat fără `category` are `other`; seed-ul global are categoriile din §3.1 după migrare. Teste `Create_WithoutCategory_IsOther` (Task 2) și integrare `SeedFoods_HaveCategories` (Task 5).

---

### Task 1: Domain și persistență

**Files:**
- Create: `src/Domain/Enums/FoodCategory.cs`, `src/Domain/Nutrition/ShoppingListBuilder.cs`
- Modify: `src/Domain/Entities/FoodItem.cs` (`Category`), `src/Domain/Entities/PlannedMeal.cs` (`ShoppingListItem`)
- Modify: `src/Infrastructure/Persistence/ApplicationDbContext.cs` (blocurile `FoodItem`, `ShoppingListItem`), `ApplicationDbContextSeed.cs` (categorii)
- Create: migrarea `ShoppingList` (cu `UPDATE`-urile de categorie din §4)
- Modify: `tests/WebApi.IntegrationTests/NutriScanApiFactory.cs` (`ShoppingListItems` șterse primele)
- Test: `tests/Domain.UnitTests/Nutrition/ShoppingListBuilderTests.cs`

**Interfaces:**
- Produces:
  ```csharp
  public enum FoodCategory { FruitsVegetables, MeatFish, Dairy, Grains, Pantry, Beverages, Other }
  // FoodItem
  public FoodCategory Category { get; set; } = FoodCategory.Other;
  // ShoppingListItem
  public Guid? FoodItemId { get; set; }  public FoodItem? FoodItem { get; set; }
  public double? QuantityGrams { get; set; }
  public string? QuantityText { get; set; }
  public FoodCategory Category { get; set; } = FoodCategory.Other;
  public bool IsManual => FoodItemId is null;
  // Domain/Nutrition/ShoppingListBuilder.cs
  public sealed record ShoppingLine(FoodItem Food, double Grams);
  public static class ShoppingListBuilder
  {
      /// Meals must be loaded with FoodItem, or Recipe.Ingredients.FoodItem. One line per food, unrounded grams.
      public static IReadOnlyList<ShoppingLine> Build(IEnumerable<PlannedMeal> meals);
  }
  ```

- [ ] **Step 1: Write the failing tests** (`ShoppingListBuilderTests`, using `RecipeTestData` and `PlannedMeal` objects):
  - `Build_SrsExample_ChickenFromTwoRecipes_Is400` (two one-serving recipes with the same chicken, 150 g and 250 g, each planned for 1 serving → one line, 400 g).
  - `Build_RecipeServings_ScalesIngredients` (SRS recipe, 4 servings, planned 2 → oats 100 g).
  - `Build_FoodMeal_UsesGrams` (oats 150 g → 150).
  - `Build_SameFoodFromRecipeAndFoodMeal_SumsIntoOneItem` (Review Focus 1: SRS recipe 2 servings + oats meal 50 g → one line 150 g).
- [ ] **Step 2: Run** `dotnet test tests/Domain.UnitTests` → build errors.
- [ ] **Step 3: Implement** the Interfaces block; group by `Food.Id`.
- [ ] **Step 4: Configure EF:** `ShoppingListItem` — `QuantityText` max 50; `HasOne(FoodItem).WithMany().HasForeignKey(FoodItemId).IsRequired(false).OnDelete(Restrict)`; `HasIndex(UserId, Category)`; check `CK_ShoppingListItems_GeneratedOrManual` with the SQL from §4. `FoodItem.Category` with `HasDefaultValue(FoodCategory.Other)`. Seed sets categories per §3.1.
- [ ] **Step 5: Generate the `ShoppingList` migration**, read it, and append to `Up` one `migrationBuilder.Sql` with the `UPDATE "FoodItems" SET "Category" = … WHERE "CreatedByUserId" IS NULL AND "Name" = …` statements for the 10 seed names (exact names from `ApplicationDbContextSeed.cs`).
- [ ] **Step 6: Update `ResetDatabaseAsync`**: delete `ShoppingListItems` first.
- [ ] **Step 7: Run** all test projects → PASS.
- [ ] **Step 8: Commit** `feat(domain): add food categories and shopping list aggregation`

---

### Task 2: Categoria alimentelor în contract

**Files:**
- Modify: `src/Application/Foods/IFoodFields.cs`, `FoodFieldsValidator.cs`, `FoodAccess.cs` (`Apply`), `Dtos/FoodDtos.cs`, `Commands/CreateFood/CreateFoodCommand.cs`, `Commands/UpdateFood/UpdateFoodCommand.cs`
- Modify: `src/WebApi/Contracts/Foods/SaveFoodRequest.cs`, `src/WebApi/Controllers/FoodsController.cs`
- Test: `tests/Application.UnitTests/Foods/FoodCommandHandlerTests.cs`, `FoodFieldsValidatorTests.cs`

**Interfaces:**
- Produces: `IFoodFields.FoodCategory? Category`; `CreateFoodCommand(…, double? SaltGrams, FoodCategory? Category = null)` and the same last parameter on `UpdateFoodCommand` (and in `UpdateFoodCommand.From`); `SaveFoodRequest(…, FoodCategory? Category = null)`; `FoodDto(…, string? EnergyWarning, FoodCategory Category)`.

- [ ] **Step 1: Write the failing tests:** `Create_WithoutCategory_IsOther` (Review Focus 5), `Create_WithCategory_Stores` (`Dairy`), `Update_ChangesCategory`; validator `Validate_UnknownCategory_IsInvalid` (`(FoodCategory)99`).
- [ ] **Step 2: Run** → build errors.
- [ ] **Step 3: Implement**: `Apply` sets `food.Category = fields.Category ?? FoodCategory.Other`; validator `RuleFor(f => f.Category).IsInEnum().When(f => f.Category is not null)`; controller passes `r.Category`.
- [ ] **Step 4: Run** `dotnet test tests/Application.UnitTests` → PASS.
- [ ] **Step 5: Commit** `feat(foods): add food category`

---

### Task 3: Generarea listei

**Files:**
- Create: `src/Application/ShoppingList/ShoppingListErrors.cs`, `ShoppingListAccess.cs`, `Dtos/ShoppingListDtos.cs`
- Create: `src/Application/ShoppingList/Commands/GenerateShoppingList/{…Command,…CommandHandler,…CommandValidator}.cs`
- Create: `src/Application/ShoppingList/Queries/GetShoppingList/{GetShoppingListQuery,GetShoppingListQueryHandler}.cs`
- Test: `tests/Application.UnitTests/ShoppingList/GenerateShoppingListTests.cs`

**Interfaces:**
- Consumes: `ShoppingListBuilder`, `ShoppingListItem` (Task 1); `PlannerAccess.WithItem()`, `PlannedMealFieldsValidator.EarliestDate/MaxDaysAhead`, `GetPlanQueryValidator.MaxDays` (planner).
- Produces:
  ```csharp
  public static class ShoppingListErrors
  {
      public const string NotFound = "Articolul nu a fost găsit în lista de cumpărături.";
      public const string NothingPlanned = "Nu există mese planificate nemâncate în intervalul ales. Planifică mai întâi mesele.";
  }
  public sealed record ShoppingListItemDto(Guid Id, string Name, double? QuantityGrams, string? QuantityText,
      FoodCategory Category, bool IsPurchased, bool IsManual, Guid? FoodId)
  { public static ShoppingListItemDto From(ShoppingListItem item); }   // QuantityGrams rounded up with Math.Ceiling
  public sealed record ShoppingCategoryDto(FoodCategory Category, IReadOnlyList<ShoppingListItemDto> Items);
  public sealed record ShoppingListDto(IReadOnlyList<ShoppingCategoryDto> Categories, int TotalCount, int PurchasedCount)
  { public static ShoppingListDto From(IEnumerable<ShoppingListItem> items); }  // category enum order, then Name, then Id
  public sealed record GenerateShoppingListCommand(Guid UserId, DateOnly From, DateOnly To) : IRequest<ShoppingListDto>;
  public sealed record GetShoppingListQuery(Guid UserId) : IRequest<ShoppingListDto>;
  internal static class ShoppingListAccess
  {
      public static Task<List<ShoppingListItem>> LoadAllAsync(this IApplicationDbContext db, Guid userId, CancellationToken ct);
      public static Task<ShoppingListItem> LoadOwnAsync(this IApplicationDbContext db, Guid userId, Guid itemId, CancellationToken ct);
  }
  ```

- [ ] **Step 1: Write the failing tests** (InMemory; foods with categories; recipes via `CreateRecipeCommandHandler`; meals via `CreatePlannedMealCommandHandler`):
  - `Generate_SrsExample_OneChickenItem400g` (AC1; `Name == "Piept de pui"`, `Category == MeatFish`, `QuantityGrams == 400`).
  - `Generate_GroupsByCategoryInEnumOrder` (chicken MeatFish, oats Grains, banana FruitsVegetables → categories `FruitsVegetables, MeatFish, Grains`).
  - `Generate_Twice_ReplacesGeneratedKeepsManual` (Review Focus 3; a manual purchased item survives; first generation's items gone; stored item count matches).
  - `Generate_ExcludesEatenMeals` (meal with `DiaryEntryId` set contributes nothing).
  - `Generate_NothingPlanned_ThrowsAndKeepsList` (Review Focus 2; `ValidationException` with `NothingPlanned`; earlier generated items still stored).
  - `Generate_ArchivedFoodInPlan_IsIncluded` (Review Focus 4).
  - `Generate_RoundsGramsUp` (recipe 100 g, 3 servings, planned 1 → stored 33.33…, DTO 34).
  - `Generate_IgnoresOtherUsersMealsAndOutOfRange`.
  - Validator: `Validate_Range32Days_IsInvalid`, `Validate_FromAfterTo_IsInvalid`.
- [ ] **Step 2: Run** → build errors.
- [ ] **Step 3: Implement.** Handler: load meals `UserId == me && PlanDate in range && DiaryEntryId == null` with `WithItem()`; empty → `ValidationException("Range", NothingPlanned)` before touching items; remove the user's items with `FoodItemId != null`; add one `ShoppingListItem` per `ShoppingLine` (`ItemName = Food.Name`, `Category = Food.Category`, `CreatedAtUtc = clock.UtcNow`); one `SaveChangesAsync`; return `ShoppingListDto.From(all items)`. Validator reuses the plan range rule (same limits and message as `GetPlanQueryValidator`).
- [ ] **Step 4: Run** `dotnet test tests/Application.UnitTests` → PASS.
- [ ] **Step 5: Commit** `feat(shopping): generate an aggregated shopping list from the plan`

---

### Task 4: Articolele listei

**Files:**
- Create: `src/Application/ShoppingList/Commands/{AddShoppingListItem,SetShoppingListItemPurchased,DeleteShoppingListItem,ClearPurchasedItems}/…`
- Test: `tests/Application.UnitTests/ShoppingList/ShoppingListItemTests.cs`

**Interfaces:**
- Consumes: Task 3.
- Produces:
  ```csharp
  public sealed record AddShoppingListItemCommand(Guid UserId, string Name, string? Quantity, FoodCategory? Category) : IRequest<ShoppingListItemDto>;
  public sealed record SetShoppingListItemPurchasedCommand(Guid UserId, Guid ItemId, bool IsPurchased) : IRequest<ShoppingListItemDto>;
  public sealed record DeleteShoppingListItemCommand(Guid UserId, Guid ItemId) : IRequest;
  public sealed record ClearPurchasedItemsCommand(Guid UserId) : IRequest;
  ```

- [ ] **Step 1: Write the failing tests:** `Add_Manual_TrimsAndDefaultsCategory` (`"  Hârtie de copt  "`, quantity `"1 rolă"`, no category → `Other`, `IsManual`); `SetPurchased_TogglesAndSetsUpdatedAt`; `SetPurchased_OtherUsersItem_ThrowsNotFound`; `Delete_OwnItem_Removes`; `Delete_OtherUsersItem_ThrowsNotFound`; `ClearPurchased_RemovesOnlyOwnPurchased` (own unpurchased and another user's purchased remain); `GetShoppingList_CountsPurchased`. Validator: `Validate_BlankName_IsInvalid`, `Validate_Name201_IsInvalid`, `Validate_Quantity51_IsInvalid`, `Validate_UnknownCategory_IsInvalid`.
- [ ] **Step 2: Run** → build errors.
- [ ] **Step 3: Implement** (blank quantity → null after trim).
- [ ] **Step 4: Run** `dotnet test tests/Application.UnitTests` → PASS.
- [ ] **Step 5: Commit** `feat(shopping): manage shopping list items`

---

### Task 5: API și teste de integrare

**Files:**
- Create: `src/WebApi/Controllers/ShoppingListController.cs`, `src/WebApi/Contracts/ShoppingList/{GenerateShoppingListRequest,AddShoppingListItemRequest,SetPurchasedRequest}.cs`
- Test: `tests/WebApi.IntegrationTests/ShoppingList/ShoppingListTests.cs`; `tests/WebApi.IntegrationTests/Foods/FoodsTests.cs` (category round-trip)

**Interfaces:**
- Produces: routes and codes exactly as §5, `[Route("api/shopping-list")]`; `SetPurchasedRequest(bool? IsPurchased)`: the controller returns `ValidationProblem` (400) when `IsPurchased` is null, otherwise sends the command.

- [ ] **Step 1: Write the failing integration tests:** AC1 `Generate_SrsExample_Returns400gChicken`; AC2 `Generate_ScalesRecipeServings`; AC3 `Generate_GroupsByCategory`; AC4 `Generate_KeepsManualAndSkipsEaten`; AC5 `Generate_EmptyRange_Returns400AndKeepsList`; AC6 `Items_AddCheckDeleteAndClearPurchased`; AC7 `Food_CategoryRoundTrip` (in `FoodsTests`) and `SeedFoods_HaveCategories` (search `"piept de pui"` → `GET /api/foods/{id}` → `meatFish`); AC8 `OtherUsersItem_Returns404`, `ShoppingList_WithoutToken_Return401`, `AddItem_BlankName_Returns400`; AC9 `CheckConstraint_RejectsDirectSqlInserts` (generated without grams, generated with text, manual with grams → `23514`).
- [ ] **Step 2: Run** `dotnet test tests/WebApi.IntegrationTests --filter "FullyQualifiedName~ShoppingList|FullyQualifiedName~Food_CategoryRoundTrip"` → failures.
- [ ] **Step 3: Implement** the controller and request records. `FoodSummaryDto` stays unchanged.
- [ ] **Step 4: Run** `dotnet test` → all PASS, no new warnings.
- [ ] **Step 5: Commit** `feat(api): add shopping list endpoints`

---

### Task 6: Documentație

**Files:**
- Create: `aplicatie/docs/adr/ADR-008-shopping-list.md`
- Modify: `aplicatie/docs/architecture_and_contracts.md` (secțiunea 11 Lista de cumpărături; `category` în secțiunea 7; ERD: `FOOD_ITEMS.Category`, `SHOPPING_LIST_ITEMS` cu `FoodItemId`, `QuantityText`, `Category`, relația `FOOD_ITEMS ||--o{ SHOPPING_LIST_ITEMS`)
- Modify: `ROADMAP.md` (bifează lista de cumpărături; pasul următor: stub `IOcrService`, raport de acoperire, apoi Faza 3)

- [ ] **Step 1: Write the documents** (cifre din spec: 150 + 250 = 400 g; 200 × 2 / 4 = 100 g; 33,3 → 34 g; numărul final de teste din Task 5).
- [ ] **Step 2: Check** `git ls-files | grep intrebari_comisie` → no output.
- [ ] **Step 3: Commit** `docs: add ADR-008, shopping list contract and roadmap progress`
