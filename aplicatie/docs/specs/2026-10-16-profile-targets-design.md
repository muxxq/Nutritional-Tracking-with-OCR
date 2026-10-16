# Spec: Profil utilizator și calculul țintelor nutriționale (backend)

**Data:** 2026-10-16
**Branch:** `feature/NUT-profile`
**Cerință SRS:** FR-02 „Profil și Calculul Targetului Nutrițional” (P1); criteriu: „Target Mifflin–St Jeor calculat; suprascriere manuală”
**Faza din roadmap:** Faza 2 (Backend core), Sprint 2
**Depinde de:** autentificarea JWT (ADR-002); folosește `isProfileComplete` introdus acolo

---

## 1. Scop

### User story
> Ca utilizator NutriScan, vreau să îmi completez profilul fizic și să primesc automat ținte zilnice de calorii și macronutrienți potrivite obiectivului meu, pe care să le pot ajusta manual, ca jurnalul să-mi arate progresul față de ce am nevoie cu adevărat.

### Criterii de acceptanță
- **AC1:** `GET /api/profile` după înregistrare răspunde 200 cu `isProfileComplete: false`, `bmr: null`, `tdee: null` și cele 4 ținte cu sursa `default` (2000 kcal, 150 g proteine, 200 g carbohidrați, 65 g grăsimi).
- **AC2:** `PUT /api/profile` cu date valide salvează profilul și răspunde 200 cu țintele calculate conform §3. Exemplu: pentru cazul R1 din §3.6, răspunsul conține 1700 kcal, 124 g proteine, 195 g carbohidrați, 47 g grăsimi, cu sursa `calculated`.
- **AC3:** după AC2, `isProfileComplete` este `true` și în `GET /api/auth/me`.
- **AC4:** `PUT /api/profile/targets` cu o suprascriere setează sursa acelei ținte pe `manual`, iar celelalte se recalculează conform §3.5. Trimiterea `null` pentru o țintă o readuce la sursa calculată.
- **AC5:** modificarea greutății prin `PUT /api/profile` actualizează țintele calculate, dar nu le schimbă pe cele manuale.
- **AC6:** date invalide (vârstă în afara 18–100, înălțime în afara 100–250 cm, greutate în afara 30–300 kg, enum necunoscut) primesc 400 ProblemDetails, nu 500.
- **AC7:** suprascrieri imposibile (carbohidrați sau grăsimi care ar ieși negativi, sau toate 4 manuale cu abatere energetică peste 15%) primesc 400.
- **AC8:** toate rutele `/api/profile` cer access token valid (401 fără el), iar fiecare utilizator vede și modifică **doar propriul profil**.
- **AC9:** enum-urile circulă în JSON ca text camelCase (`"female"`, `"moderatelyActive"`, `"loseWeight"`), atât la intrare, cât și la ieșire.

### În afara scopului
Istoricul greutății, ștergerea și exportul contului (GDPR), unitățile imperiale, modificarea email-ului sau a parolei, ecranele din client (Faza 3).

---

## 2. Decizii (rezumat pentru ADR-003)

| # | Întrebare | Decizie | Alternative respinse |
|---|---|---|---|
| D1 | Cum funcționează suprascrierea manuală? | Se stochează doar suprascrierile opționale (câte una per țintă); valoarea calculată se derivă la fiecare citire; fiecare țintă își raportează sursa | Comutator global „manual” (prea grosier); valori stocate cu buton „Recalculează” (țintele rămân vechi după o schimbare de greutate) |
| D2 | Formula BMR | Mifflin–St Jeor (1990) | Harris–Benedict (1919, supraestimează; Frankenfield 2005 arată că Mifflin e cea mai precisă la adulți) |
| D3 | Ajustarea după obiectiv | Procentuală: −20% / 0 / +10% din TDEE, cu prag de 1200 kcal (femei) / 1500 kcal (bărbați) pentru țintele calculate | Valori fixe (−500 kcal): deficit disproporționat pentru persoanele mici |
| D4 | Macronutrienți | Proteine în g/kg după obiectiv (2,0 / 1,6 / 1,8, ISSN 2017), grăsimi 25% din calorii (AMDR 20–35%), carbohidrații primesc restul | Procente fixe pentru toate trei: proteinele nu ar mai depinde de masa corporală |
| D5 | Unde stă calculul | Clasă pură în Domain (`NutritionTargetCalculator`) | În handler: logica de business ar ajunge în Application și n-ar mai putea fi refolosită de jurnal |
| D6 | API | `PUT /api/profile` (profil complet) separat de `PUT /api/profile/targets` | Un singur endpoint: amestecă două formulare și două seturi de reguli |

---

## 3. Calculul țintelor (Domain)

### 3.1 Vârsta
Ani împliniți la data curentă (`DateOnly today`): `today.Year − birth.Year`, minus 1 dacă `(today.Month, today.Day) < (birth.Month, birth.Day)`. Consecință: o persoană născută pe 29 februarie împlinește anii pe 1 martie în anii nebisecți.

### 3.2 BMR și TDEE
```
BMR  = 10·greutate_kg + 6,25·înălțime_cm − 5·vârstă + (5 la bărbați | −161 la femei)
TDEE = BMR × factor_activitate
```
Factorii de activitate: Sedentary 1,2 · LightlyActive 1,375 · ModeratelyActive 1,55 · VeryActive 1,725 · ExtremelyActive 1,9.

### 3.3 Calorii calculate
```
brut      = TDEE × factor_obiectiv     (LoseWeight 0,80 · MaintainWeight 1,00 · GainMuscle 1,10)
calculate = max(prag, Round10(brut))  (prag: 1200 femei · 1500 bărbați)
```
`Round10` rotunjește la cel mai apropiat multiplu de 10, cu jumătățile în sus (`MidpointRounding.AwayFromZero`).

### 3.4 Macronutrienți calculați
Plecând de la caloriile **efective** `K` (manuale dacă există, altfel calculate):
```
P = greutate_kg × g_per_kg      (LoseWeight 2,0 · MaintainWeight 1,6 · GainMuscle 1,8)
G = K × 0,25 / 9
C = (K − 4·P − 9·G) / 4
```
Calculul intern folosește valorile nerotunjite. Doar ieșirea se rotunjește la gram întreg (jumătățile în sus). BMR și TDEE se raportează rotunjite la kcal întreg.

### 3.5 Combinarea cu suprascrierile
Suprascrierile sunt **intrări** în calcul. Valoarea manuală înlocuiește formula, iar țintele care depind de ea se recalculează:
1. `K` = calorii manuale, altfel cele calculate (§3.3).
2. `P` = proteine manuale, altfel formula din §3.4.
3. Absorbantul diferenței energetice e **primul macronutrient nesuprascris** din lista [carbohidrați, grăsimi]:
   - **Carbohidrați nesuprascriși:** grăsimile = manual sau `K × 0,25 / 9`; carbohidrații = `(K − 4P − 9G) / 4`.
   - **Carbohidrați manuali, grăsimi nesuprascrise:** grăsimile = `(K − 4P − 4C) / 9`.
   - **Carbohidrați și grăsimi manuali:**
     - calorii manuale → verificarea de consistență: `|4P + 4C + 9G − K| / K ≤ 0,15`, altfel eroare;
     - calorii nesuprascrise → caloriile se derivă: `K = 4P + 4C + 9G`, rotunjite cu `Round10`, sursa `calculated`. Pragul de siguranță nu se aplică, fiind derivate din valori alese de utilizator.
4. Dacă absorbantul ar ieși negativ, rezultă eroare.

Erorile din pașii 3–4 sunt raportate de calculator ca rezultat invalid cu un mesaj. Handler-ul le transformă în `ValidationException`, deci 400.

**Profil incomplet** (lipsește înălțimea, greutatea sau data nașterii): nu se calculează nimic. Fiecare țintă = suprascrierea ei (sursa `manual`) sau valoarea implicită (sursa `default`): 2000 kcal, 150 g, 200 g, 65 g. Verificarea de consistență se aplică doar dacă toate 4 sunt manuale. `bmr` și `tdee` sunt `null`.

### 3.6 Cazuri de referință (valori exacte, folosite în teste)

| # | Profil | BMR | TDEE | kcal | P | C | G |
|---|---|---|---|---|---|---|---|
| R1 | F, 62 kg, 168 cm, 28 ani, ModeratelyActive, LoseWeight | 1369 | 2122 | 1700 | 124 | 195 | 47 |
| R2 | R1 + proteine manuale 140 | 1369 | 2122 | 1700 | 140 (manual) | 179 | 47 |
| R3 | R1 + calorii manuale 1800 | 1369 | 2122 | 1800 (manual) | 124 | 214 | 50 |
| R4 | M, 80 kg, 180 cm, 30 ani, Sedentary, MaintainWeight | 1780 | 2136 | 2140 | 128 | 273 | 59 |
| R5 | F, 45 kg, 150 cm, 60 ani, Sedentary, LoseWeight (prag) | 927 | 1112 | 1200 | 90 | 135 | 33 |

### 3.7 Tipuri (Domain)
```csharp
public enum TargetSource { Default, Calculated, Manual }
public sealed record TargetValue(double Value, TargetSource Source);
public sealed record NutritionTargets(int? Bmr, int? Tdee, TargetValue Calories, TargetValue ProteinsGrams,
                                      TargetValue CarbsGrams, TargetValue FatsGrams);
public static class NutritionTargetCalculator
{
    // Returns targets or an error message when overrides are inconsistent (§3.5).
    public static NutritionTargetResult Calculate(User user, DateOnly today);
}
```

---

## 4. Modelul de date

`User`:
- se șterg `TargetCalories`, `TargetProteinsGrams`, `TargetCarbsGrams`, `TargetFatsGrams`;
- se adaugă `int? CaloriesOverride`, `double? ProteinsGramsOverride`, `double? CarbsGramsOverride`, `double? FatsGramsOverride`;
- `BirthDate` devine `DateOnly?` (coloană `date`);
- `IsProfileComplete => HeightCm > 0 && WeightKg > 0 && BirthDate is not null`.

Migrarea `ProfileTargetOverrides` este editată manual: ștergere + adăugare, fără redenumiri ghicite de EF. Coloana `BirthDate` se convertește cu `ALTER COLUMN ... TYPE date USING "BirthDate"::date`. Valorile țintelor stocate până acum erau doar cele implicite, deci nu se pierd date reale.

---

## 5. Contract API

Toate rutele sunt `[Authorize]`. Id-ul utilizatorului se citește din claim-ul `sub`. Dacă utilizatorul nu mai există, răspunsul e 401 `AuthErrors.InvalidSession`, ca la `/api/auth/me`.

| Metodă și rută | Corp | Succes | Erori |
|---|---|---|---|
| `GET /api/profile` | — | 200 `ProfileDto` | 401 |
| `PUT /api/profile` | `UpdateProfileRequest` | 200 `ProfileDto` | 400, 401 |
| `PUT /api/profile/targets` | `UpdateTargetsRequest` | 200 `ProfileDto` | 400, 401 |

```jsonc
// UpdateProfileRequest (toate câmpurile obligatorii)
{ "firstName": "Ana", "lastName": "Pop", "birthDate": "1998-05-14",
  "heightCm": 168, "weightKg": 62, "gender": "female",
  "activityLevel": "moderatelyActive", "goal": "loseWeight" }

// UpdateTargetsRequest (null sau lipsă = calculat automat)
{ "calories": null, "proteinsGrams": 140, "carbsGrams": null, "fatsGrams": null }

// ProfileDto
{ "email": "ana@exemplu.ro", "firstName": "Ana", "lastName": "Pop",
  "birthDate": "1998-05-14", "age": 28, "heightCm": 168, "weightKg": 62,
  "gender": "female", "activityLevel": "moderatelyActive", "goal": "loseWeight",
  "isProfileComplete": true, "bmr": 1369, "tdee": 2122,
  "targets": {
    "calories":      { "value": 1700, "source": "calculated" },
    "proteinsGrams": { "value": 140,  "source": "manual" },
    "carbsGrams":    { "value": 179,  "source": "calculated" },
    "fatsGrams":     { "value": 47,   "source": "calculated" } } }
```

**Serializare:** `JsonStringEnumConverter(JsonNamingPolicy.CamelCase)` se înregistrează global în `AddControllers().AddJsonOptions(...)`. Un enum necunoscut eșuează la model binding și produce 400 (AC6, AC9).

**Validare (FluentValidation):**
- `UpdateProfileCommand`:
  - nume ca la înregistrare (obligatorii, maximum 100 de caractere);
  - `BirthDate` cu vârsta la data curentă (prin `IDateTimeProvider`) în **18–100** de ani;
  - `HeightCm` **100–250**, `WeightKg` **30–300**;
  - `Gender`, `ActivityLevel`, `Goal` cu `IsInEnum()`.
- `UpdateTargetsCommand`:
  - `Calories` **1000–6000**;
  - `ProteinsGrams` **0–500**, `CarbsGrams` **0–1000**, `FatsGrams` **0–300** (toate opționale);
  - consistența din §3.5 se verifică în handler, cu calculatorul.

---

## 6. Arhitectură (fișiere)

**Domain**
- `Enums/TargetSource.cs`
- `Nutrition/NutritionTargets.cs` (`TargetValue`, `NutritionTargets`, `NutritionTargetResult`)
- `Nutrition/NutritionTargetCalculator.cs`
- `Entities/User.cs` (modificări din §4; metodă `int? AgeOn(DateOnly today)`)

**Application**
- `Profile/Dtos/ProfileDto.cs` (+ `TargetDto`)
- `Profile/Queries/GetProfile/` (query, handler)
- `Profile/Commands/UpdateProfile/` (comandă, validator, handler)
- `Profile/Commands/UpdateTargets/` (comandă, validator, handler)

**Infrastructure**
- configurarea EF pentru `BirthDate` ca `date` și migrarea `ProfileTargetOverrides`

**WebApi**
- `Controllers/ProfileController.cs`
- `Contracts/Profile/UpdateProfileRequest.cs`, `UpdateTargetsRequest.cs`
- `Auth/ClaimsPrincipalExtensions.cs` (`Guid? GetUserId()`), folosit și de `AuthController.Me`
- `Program.cs`: convertorul de enum-uri

---

## 7. Testare

- **Domain.UnitTests:**
  - `NutritionTargetCalculatorTests`: R1–R5 exact; carbohidrați manuali → grăsimile absorb diferența; carbohidrați și grăsimi manuali fără calorii → calorii derivate; toate 4 manuale în limita de ±15% → acceptat, peste 15% → eroare; proteine manuale prea mari → carbohidrați negativi → eroare; profil incomplet → `default`, iar cu suprascriere de calorii → `manual` + `default`.
  - `UserAgeTests`: cu o zi înainte de aniversare, în ziua aniversării, 29 februarie.
- **Application.UnitTests:**
  - validatoarele: limite inclusiv/exclusiv (17 / 18 / 100 / 101 ani; 99 / 100 / 250 / 251 cm; 29 / 30 / 300 / 301 kg; calorii 999 / 1000 / 6000 / 6001), enum în afara intervalului;
  - handlerele: salvare, suprascrieri imposibile → `ValidationException`, utilizator inexistent → `UnauthorizedException`.
- **WebApi.IntegrationTests:** AC1–AC9 end-to-end, inclusiv doi utilizatori care își văd doar propriul profil și enum-uri ca text.

---

## 8. Livrare și documentație

- Commit-uri *Conventional Commits* fără linii de co-author; merge local în `develop`.
- `aplicatie/docs/adr/ADR-003-nutrition-target-calculation.md`: deciziile D1–D6 și sursele (Mifflin et al. 1990; Frankenfield et al. 2005; Jäger et al. 2017, ISSN Position Stand; IOM AMDR 2005).
- `architecture_and_contracts.md`: secțiunea `/api/profile`; ERD actualizat pentru `USERS`.
- `sprint-02.md`: story-ul Profil; `ROADMAP.md`: bifă la autentificare și profil.
- Fișierul local de întrebări pentru comisie: secțiune nouă despre calculul țintelor, **fără commit**.
