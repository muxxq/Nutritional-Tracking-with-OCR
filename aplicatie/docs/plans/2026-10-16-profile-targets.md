# Profil și ținte nutriționale: plan de implementare

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `GET/PUT /api/profile` și `PUT /api/profile/targets`, cu ținte calculate (Mifflin–St Jeor) și suprascrieri manuale per țintă.

**Architecture:** calculul e o funcție pură în Domain (`NutritionTargetCalculator`). Handlerele MediatR din Application încarcă utilizatorul, aplică modificarea și folosesc calculatorul pentru răspuns și pentru validarea suprascrierilor. Controllerul din WebApi citește id-ul doar din token.

**Tech Stack:** .NET 8, EF Core 8 + Npgsql, MediatR 14, FluentValidation 12, xUnit + FluentAssertions + Moq, EF InMemory, Testcontainers (Podman).

**Spec:** `aplicatie/docs/specs/2026-10-16-profile-targets-design.md`. Secțiunile sunt referite ca §N.

## Global Constraints

- Comenzile `dotnet` rulează din `aplicatie/backend/`. Commit-urile se fac pe `feature/NUT-profile`.
- Cod și commit-uri în engleză (*Conventional Commits*), **fără linii `Co-Authored-By`**. Mesajele pentru utilizator (erori de validare) sunt în română.
- Testele de integrare cer `export DOCKER_HOST=unix:///run/user/$(id -u)/podman/podman.sock TESTCONTAINERS_RYUK_DISABLED=true`.
- Tiparul Arrange-Act-Assert; numire `Method_Scenario_ExpectedResult`; FluentAssertions.
- Valori fixe (§3):
  - factori de activitate 1,2 / 1,375 / 1,55 / 1,725 / 1,9;
  - factori de obiectiv 0,80 / 1,00 / 1,10; proteine 2,0 / 1,6 / 1,8 g/kg; grăsimi 25%;
  - pragul caloriilor calculate 1200 (F) / 1500 (M);
  - ținte implicite 2000 / 150 / 200 / 65;
  - toleranța de consistență energetică 0,15;
  - `Round10` și rotunjirea gramelor cu `MidpointRounding.AwayFromZero`.
- Limite de validare (§5):
  - vârstă 18–100, înălțime 100–250 cm, greutate 30–300 kg;
  - calorii 1000–6000; proteine 0–500, carbohidrați 0–1000, grăsimi 0–300 g.
- Timpul curent vine **numai** din `IDateTimeProvider` (`DateOnly.FromDateTime(clock.UtcNow)`).
- Fișierul `documentatie/pregatire_sustinere/intrebari_comisie.md` nu se adaugă în git.

## Review Focus

1. **Suprascrieri care fac un macronutrient negativ printr-o combinație** (de exemplu calorii manuale 1000 + proteine manuale 300). Ne așteptăm la 400, nu la ținte negative. Testul `Calculate_ManualProteinsExceedCalories_ReturnsError` e în Task 1.
2. **PUT pe profil pentru un utilizator care are deja suprascrieri.** Suprascrierile trebuie păstrate (AC5). Testul `UpdateProfile_KeepsManualTargets` e în Task 4.
3. **Un `birthDate` în viitor sau cu format greșit** (`"14-05-1998"`). Ne așteptăm la 400, nu la 500 și nici la o vârstă negativă. Testul `UpdateProfile_InvalidBirthDateFormat_Returns400` e în Task 4; data viitoare e acoperită de limita de 18 ani din Task 3.
4. **Enum trimis ca număr** (`"gender": 2`). Odată cu convertorul de text, numerele mai sunt acceptate implicit. Decizie: le acceptăm (comportamentul implicit al `JsonStringEnumConverter`), dar `IsInEnum` respinge valorile în afara intervalului (de exemplu `9`). Testul `UpdateProfile_OutOfRangeNumericEnum_Returns400` e în Task 4.
5. **Profil incomplet cu toate 4 țintele manuale, dar inconsistente.** Ne așteptăm la 400, la fel ca pentru profilul complet. Testul `Calculate_IncompleteProfileAllManualInconsistent_ReturnsError` e în Task 1.

---

### Task 1: Domain: modificările `User` și `NutritionTargetCalculator`

**Files:**
- Modify: `src/Domain/Entities/User.cs`
- Create: `src/Domain/Enums/TargetSource.cs`, `src/Domain/Nutrition/NutritionTargets.cs`, `src/Domain/Nutrition/NutritionTargetCalculator.cs`
- Test: `tests/Domain.UnitTests/Entities/UserAgeTests.cs`, `tests/Domain.UnitTests/Nutrition/NutritionTargetCalculatorTests.cs`
- Modify: `tests/Domain.UnitTests/Entities/UserLockoutTests.cs` (testul `IsProfileComplete` primește și data nașterii)

**Interfaces:**
- Produces:
  ```csharp
  // User: remove TargetCalories/TargetProteinsGrams/TargetCarbsGrams/TargetFatsGrams; BirthDate → DateOnly?
  int? CaloriesOverride; double? ProteinsGramsOverride; double? CarbsGramsOverride; double? FatsGramsOverride;
  bool IsProfileComplete => HeightCm > 0 && WeightKg > 0 && BirthDate is not null;
  int? AgeOn(DateOnly today);   // §3.1; null when BirthDate is null

  public enum TargetSource { Default, Calculated, Manual }
  public sealed record TargetValue(double Value, TargetSource Source);
  public sealed record NutritionTargets(int? Bmr, int? Tdee, TargetValue Calories, TargetValue ProteinsGrams,
                                        TargetValue CarbsGrams, TargetValue FatsGrams);
  public sealed record NutritionTargetResult(NutritionTargets? Targets, string? Error)
  { public bool IsValid => Error is null; }
  public static class NutritionTargetCalculator
  {
      public const double EnergyTolerance = 0.15;
      public static NutritionTargetResult Calculate(User user, DateOnly today);
  }
  ```
  Valorile `TargetValue.Value` sunt deja rotunjite (calorii la 10 când sunt calculate, grame la întreg). Mesajele de eroare sunt în română, de exemplu „Țintele setate manual nu sunt compatibile: carbohidrații ar deveni negativi.” și „Țintele setate manual diferă cu peste 15% de totalul caloric (4P + 4C + 9G).”

- [ ] **Step 1: Scrie testele (pică)**
  - `UserAgeTests` (născut 1998-05-14): pe 2026-05-13 → 27; pe 2026-05-14 → 28; născut 2000-02-29: pe 2025-02-28 → 24, pe 2025-03-01 → 25; `BirthDate` null → null.
  - `NutritionTargetCalculatorTests`:
    - cazurile R1–R5 din §3.6, exact: BMR, TDEE, kcal, P, C, G și sursele (`Calculated` / `Manual`);
    - `Calculate_ManualCarbs_FatsAbsorbDifference`: R1 + carbohidrați manuali 150 → grăsimi = round((1700 − 4·124 − 4·150)/9) = 67, sursa `Calculated`;
    - `Calculate_ManualCarbsAndFatsWithoutCalories_DerivesCalories`: R1 + C 200, G 60 → kcal = Round10(4·124 + 4·200 + 9·60) = 1840, sursa `Calculated`;
    - `Calculate_AllManualWithinTolerance_IsValid`: kcal 2000, P 150, C 200, G 65 → valid;
    - `Calculate_AllManualBeyondTolerance_ReturnsError`: kcal 2000, P 100, C 100, G 30 → eroare (1070 kcal din macronutrienți, adică 46% sub țintă);
    - `Calculate_ManualProteinsExceedCalories_ReturnsError`: R1 + kcal 1000 + P 300 → eroare (Review Focus 1);
    - `Calculate_IncompleteProfile_ReturnsDefaults`: user fără înălțime → 2000/150/200/65 `Default`, `Bmr` și `Tdee` null;
    - `Calculate_IncompleteProfileWithManualCalories_MixesManualAndDefault`: kcal 1800 `Manual`, restul `Default`;
    - `Calculate_IncompleteProfileAllManualInconsistent_ReturnsError` (Review Focus 5).
- [ ] **Step 2: Rulează** `dotnet test tests/Domain.UnitTests`. Așteptat: eroare de compilare.
- [ ] **Step 3: Implementează** conform §3. Calculul intern rămâne nerotunjit (§3.4); rotunjirea se face doar la construirea `TargetValue`.
- [ ] **Step 4: Rulează** `dotnet test tests/Domain.UnitTests`. Așteptat: toate trec. `dotnet build` poate pica în Infrastructure; se rezolvă în Task 2.
- [ ] **Step 5: Commit** `feat(domain): add nutrition target calculator and profile overrides`

---

### Task 2: Persistență și migrare

**Files:**
- Modify: `src/Infrastructure/Persistence/ApplicationDbContext.cs` (`BirthDate` ca `date`, fără configurări noi pentru override-uri)
- Create: migrarea `ProfileTargetOverrides` (generată, apoi editată)

- [ ] **Step 1: Generează migrarea** cu aceeași comandă ca la autentificare (`ConnectionStrings__DefaultConnection=... dotnet ef migrations add ProfileTargetOverrides ...`).
- [ ] **Step 2: Editează `Up`/`Down`:**
  - orice `RenameColumn` între `Target*` și `*Override` se înlocuiește cu `DropColumn` + `AddColumn`;
  - `BirthDate` se schimbă în `date` cu `migrationBuilder.Sql("ALTER TABLE \"Users\" ALTER COLUMN \"BirthDate\" TYPE date USING \"BirthDate\"::date;")` în `Up`, iar `Down` face invers spre `timestamp with time zone`.
  - Așteptat în final: 4 `DropColumn` și 4 `AddColumn` nullable, fără `RenameColumn`.
- [ ] **Step 3: Verifică** cu `dotnet build` (0 erori) și `dotnet test` (toate trec, inclusiv integrarea, care aplică migrarea pe Postgres real).
- [ ] **Step 4: Commit** `feat(profile): migrate target overrides and date-only birth date`

---

### Task 3: Application: query și comenzi pentru profil

**Files:**
- Create: `src/Application/Profile/Dtos/ProfileDto.cs`
- Create: `src/Application/Profile/Queries/GetProfile/GetProfileQuery.cs`, `GetProfileQueryHandler.cs`
- Create: `src/Application/Profile/Commands/UpdateProfile/UpdateProfileCommand.cs`, `UpdateProfileCommandValidator.cs`, `UpdateProfileCommandHandler.cs`
- Create: `src/Application/Profile/Commands/UpdateTargets/UpdateTargetsCommand.cs`, `UpdateTargetsCommandValidator.cs`, `UpdateTargetsCommandHandler.cs`
- Test: `tests/Application.UnitTests/Profile/*Tests.cs` (folosește `AuthTestContext` pentru DB și ceas)

**Interfaces:**
- Consumes: Task 1; `IApplicationDbContext`, `IDateTimeProvider`, `UnauthorizedException`, `AuthErrors.InvalidSession`.
- Produces:
  ```csharp
  public sealed record TargetDto(double Value, TargetSource Source);
  public sealed record TargetsDto(TargetDto Calories, TargetDto ProteinsGrams, TargetDto CarbsGrams, TargetDto FatsGrams);
  public sealed record ProfileDto(string Email, string FirstName, string LastName, DateOnly? BirthDate, int? Age,
      double HeightCm, double WeightKg, Gender Gender, ActivityLevel ActivityLevel, NutritionGoal Goal,
      bool IsProfileComplete, int? Bmr, int? Tdee, TargetsDto Targets)
  { public static ProfileDto From(User user, NutritionTargets targets, DateOnly today); }
  public sealed record GetProfileQuery(Guid UserId) : IRequest<ProfileDto>;
  public sealed record UpdateProfileCommand(Guid UserId, string FirstName, string LastName, DateOnly BirthDate,
      double HeightCm, double WeightKg, Gender Gender, ActivityLevel ActivityLevel, NutritionGoal Goal) : IRequest<ProfileDto>;
  public sealed record UpdateTargetsCommand(Guid UserId, int? Calories, double? ProteinsGrams,
      double? CarbsGrams, double? FatsGrams) : IRequest<ProfileDto>;
  ```
  Handlerele: utilizator inexistent → `UnauthorizedException(AuthErrors.InvalidSession)`. `UpdateTargets` aplică suprascrierile pe entitate, rulează calculatorul și, dacă rezultatul e invalid, aruncă `ValidationException` cu o eroare pe proprietatea `"Targets"` și **nu salvează**. `UpdateProfile` face trim pe nume și **nu atinge suprascrierile**. Dacă după actualizare calculatorul ar fi invalid (suprascrieri vechi devenite imposibile), salvează oricum, iar `GET` raportează ce se poate calcula. Decizia se notează în ledger.

- [ ] **Step 1: Scrie testele (pică)**
  - `UpdateProfileCommandValidatorTests` (ceas 2026-10-16):
    - date de naștere: 2008-10-16 (18 ani) valid, 2008-10-17 (17) invalid, 1926-10-17 (99) valid, 1925-10-16 (101) invalid, 2030-01-01 invalid;
    - înălțime 99 / 100 / 250 / 251;
    - greutate 29 / 30 / 300 / 301;
    - `(Gender)9` invalid; nume goale invalide.
  - `UpdateTargetsCommandValidatorTests`: calorii 999 / 1000 / 6000 / 6001; proteine −1 / 501; carbohidrați 1001; grăsimi 301; toate null valid.
  - `GetProfileQueryHandlerTests`: utilizator nou → `IsProfileComplete == false`, sursa caloriilor `Default`; utilizator inexistent → `UnauthorizedException`.
  - `UpdateProfileCommandHandlerTests`:
    - R1 → `Targets.Calories.Value == 1700`;
    - `Handle_UserWithManualProteins_KeepsOverride`: AC5;
    - utilizator inexistent → 401.
  - `UpdateTargetsCommandHandlerTests`: R1 + P 140 → C 179 (R2); null readuce `Calculated`; kcal 1000 + P 300 → `ValidationException`, iar în DB `ProteinsGramsOverride` rămâne null.
- [ ] **Step 2: Rulează** `dotnet test tests/Application.UnitTests`. Așteptat: eroare de compilare.
- [ ] **Step 3: Implementează.**
- [ ] **Step 4: Rulează** `dotnet test tests/Application.UnitTests`. Așteptat: toate trec.
- [ ] **Step 5: Commit** `feat(profile): add profile query and update commands`

---

### Task 4: WebApi: `ProfileController`, enum-uri ca text, extensie pentru claim-uri

**Files:**
- Create: `src/WebApi/Controllers/ProfileController.cs`, `src/WebApi/Contracts/Profile/UpdateProfileRequest.cs`, `UpdateTargetsRequest.cs`, `src/WebApi/Auth/ClaimsPrincipalExtensions.cs`
- Modify: `src/WebApi/Controllers/AuthController.cs` (`Me` folosește `GetUserId()`), `src/WebApi/Program.cs` (convertorul de enum-uri)
- Test: `tests/WebApi.IntegrationTests/Profile/ProfileTests.cs`

**Interfaces:**
- Consumes: Task 3.
- Produces:
  ```csharp
  public static class ClaimsPrincipalExtensions { public static Guid? GetUserId(this ClaimsPrincipal user); } // parses "sub"
  public sealed record UpdateProfileRequest(string FirstName, string LastName, DateOnly BirthDate, double HeightCm,
      double WeightKg, Gender Gender, ActivityLevel ActivityLevel, NutritionGoal Goal);
  public sealed record UpdateTargetsRequest(int? Calories, double? ProteinsGrams, double? CarbsGrams, double? FatsGrams);
  // ProfileController: [ApiController][Authorize][Route("api/profile")]; GET "", PUT "", PUT "targets"
  ```

- [ ] **Step 1: Scrie testele (pică)**, cu `ResetDatabaseAsync()` și un utilizator nou per test:
  - `GetProfile_AfterRegister_ReturnsDefaults` (AC1);
  - `UpdateProfile_R1_ReturnsCalculatedTargets` (AC2): verifică `bmr == 1369`, `tdee == 2122`, `targets.calories.value == 1700`, `targets.calories.source == "calculated"`, `gender == "female"` (AC9);
  - `UpdateProfile_MakesAuthMeProfileComplete` (AC3);
  - `UpdateTargets_ManualProteins_RecalculatesCarbs` (AC4): P 140 → carbohidrați 179, sursa proteinelor `"manual"`; apoi `proteinsGrams: null` → `"calculated"`;
  - `UpdateProfile_KeepsManualTargets` (AC5, Review Focus 2): după P 140 manual, greutatea 62 → 70: proteinele rămân 140 `"manual"`, carbohidrații se recalculează;
  - `UpdateProfile_InvalidValues_Returns400` (AC6): theory cu înălțime 99, greutate 301, `gender: "alien"`;
  - `UpdateProfile_InvalidBirthDateFormat_Returns400` (Review Focus 3);
  - `UpdateProfile_OutOfRangeNumericEnum_Returns400` (Review Focus 4);
  - `UpdateTargets_Impossible_Returns400` (AC7);
  - `Profile_WithoutToken_Returns401` pentru toate 3 rutele (AC8);
  - `Profile_TwoUsers_SeeOnlyTheirOwn` (AC8).
- [ ] **Step 2: Rulează** testele de integrare. Așteptat: 404 sau eșecuri.
- [ ] **Step 3: Implementează.** În `Program.cs`: `AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)))`.
- [ ] **Step 4: Rulează** `dotnet test`. Așteptat: toate trec, inclusiv testele de autentificare.
- [ ] **Step 5: Commit** `feat(api): add profile endpoints with string enums`

---

### Task 5: Documentație

- [ ] **Step 1:**
  - `ADR-003-nutrition-target-calculation.md` (D1–D6, formule, surse din §8);
  - `architecture_and_contracts.md`: secțiunea 6 „Profil” cu contractul din §5 și ERD-ul `USERS` actualizat;
  - `sprint-02.md`: story-ul Profil, cu tabelul AC → teste;
  - `ROADMAP.md`: bifele pentru autentificare (JWT, PBKDF2, brute-force) și profil;
  - fișierul local de întrebări: secțiunea de calcul al țintelor (doar pe disc).
- [ ] **Step 2: Verificare:** `dotnet build && dotnet test` (toate trec); `git status --short | grep intrebari_comisie` nu afișează nimic.
- [ ] **Step 3: Commit** `docs: add ADR-003, profile contract and roadmap progress`
