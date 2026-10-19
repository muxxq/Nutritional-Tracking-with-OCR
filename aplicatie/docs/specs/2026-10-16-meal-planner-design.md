# Spec: Planificatorul săptămânal și „Am mâncat asta” (backend)

**Data:** 2026-10-16
**Branch:** `feature/NUT-planner`
**Cerințe SRS:** UC04, FR-10 (planificator săptămânal), FR-11 (transferul unei mese planificate în jurnal)
**Faza din roadmap:** Faza 2 (Backend core), ciclul 2 din 3 al zonei Rețetar / Planificator / Listă de cumpărături
**Depinde de:** profil și ținte (ADR-003), catalogul de alimente (ADR-004), jurnalul alimentar (ADR-005), rețetarul (ADR-006)

---

## 1. Scop

### User story
> Ca utilizator NutriScan, vreau să-mi planific mesele săptămânii din alimente și rețete și să văd pentru fiecare zi dacă planul se încadrează în ținta calorică, apoi să marchez dintr-o singură acțiune că am mâncat o masă planificată, fără să o introduc din nou în jurnal.

### Criterii de acceptanță
- **AC1:** `POST /api/plan/items` cu o rețetă și `servings = 2` (sau cu un aliment, în grame sau porții) întoarce 201 (`Location: /api/plan/items/{id}`), cu valorile calculate din rețeta curentă (`perServing × 2`).
- **AC2:** `GET /api/plan?from=…&to=…` întoarce toate zilele intervalului (și cele goale), fiecare cu cele 4 mese, totalurile, ținta zilnică și starea față de țintă: `under` dacă totalul caloric < 95% din țintă, `within` între 95% și 105% inclusiv, `over` peste 105% (FR-10). Exemplu: țintă 1700 kcal → `within` între 1615 și 1785 kcal.
- **AC3 (calcul la citire):** după editarea unui aliment sau a unei rețete, următorul `GET` al planului întoarce valorile noi.
- **AC4 (FR-11):** `POST /api/plan/items/{id}/eat` creează o intrare de jurnal la data și masa planificate, cu aceeași cantitate (grame sau porții) și cu snapshot din valorile curente; întoarce 201 cu `DiaryEntryDto` (`Location: /api/diary/entries/{id}`). Masa planificată primește `diaryEntryId`.
- **AC5:** al doilea `eat` pe aceeași masă întoarce 409. După ștergerea intrării din jurnal, masa poate fi transferată din nou.
- **AC6:** `eat` pe o masă planificată pentru o zi de după ziua de mâine întoarce 400 (nu se poate mânca în viitor; aceeași limită ca la jurnal).
- **AC7:** o masă planificată al cărei aliment sau rețetă a fost arhivat după planificare rămâne în plan, cu `isArchived = true`, și poate fi editată, ștearsă și transferată. Un aliment sau o rețetă invizibile (ale altui utilizator sau arhivate) nu pot fi planificate (404).
- **AC8:** masa planificată a altui utilizator primește 404 la `GET`, `PUT`, `DELETE` și `eat` (NFR-10).
- **AC9:** cererile invalide (§3.4) primesc 400 ProblemDetails; baza de date respinge prin `CHECK` o masă planificată cu ambele chei (sau niciuna) și o rețetă planificată cu grame sau fără porții.
- **AC10:** toate rutele `/api/plan` cer access token valid (401).

### În afara scopului
Lista de cumpărături (FR-12, ciclul 3; designul de aici o pregătește prin porțiile rețetelor), copierea unei săptămâni, planificarea automată, statisticile (FR-08), ecranele din client, notificările.

---

## 2. Decizii (rezumat pentru ADR-007)

| # | Întrebare | Decizie | Alternative respinse |
|---|---|---|---|
| D1 | Ce formă are o masă planificată? | **Polimorfă, ca intrarea de jurnal** (ADR-006 D2, D3): exact una dintre cheile `FoodItemId` / `RecipeId`; alimentul în grame sau porții, rețeta doar în porții; aceleași constrângeri `CHECK` | Doar grame pentru tot (entitatea inițială): o rețetă în grame crude e greșită nutrițional, iar FR-12 cere scalarea ingredientelor la porțiile planificate |
| D2 | Se stochează valorile planului? | **Nu, se calculează la citire** din alimentele și rețetele curente. Planul este o intenție pentru viitor, ca rețeta (ADR-006 D1); snapshot-ul apare la transfer, când intenția devine fapt | Snapshot la planificare: o corectare a alimentului n-ar mai ajunge în plan, deși masa nu a fost încă mâncată |
| D3 | Cum citește clientul planul? | **Un endpoint pe interval** (`GET /api/plan?from&to`, maximum 31 de zile), în forma ecranului: zilele cu mesele, totalurile, ținta și starea | Un endpoint pe săptămână fixă (luni–duminică): impune clientului începutul săptămânii și nu servește FR-12, care cere „un interval de zile”. Endpoint-uri separate pentru mese și sumar: două request-uri care pot fi inconsistente (ADR-005 D3) |
| D4 | Cum se calculează starea zilei? | **Pe calorii, ±5% din ținta curentă**, exact formularea FR-10; țintele curente ca în jurnal (ADR-005 D5) | Stare pe fiecare macronutrient: FR-10 cere doar comparația calorică; ecranul are un singur indicator |
| D5 | Ce face „Am mâncat asta”? | **Creează o intrare de jurnal** la data și masa planificate, cu aceeași cantitate și snapshot din valorile curente, refolosind regulile jurnalului | Mutarea mesei din plan în jurnal (ștergerea din plan): utilizatorul pierde planul săptămânii și nu mai vede ce a respectat. Alegerea datei de azi: o masă planificată ieri și consemnată azi ar ajunge în ziua greșită |
| D6 | Cum se evită transferul dublu? | **Referință `DiaryEntryId`** pe masa planificată, cu `ON DELETE SET NULL` și index unic. Al doilea transfer primește 409; dacă intrarea din jurnal e ștearsă, referința devine NULL și masa se poate transfera din nou | Boolean `IsTransferredToDiary` (entitatea inițială): rămâne `true` după ștergerea intrării, deci masa nu mai poate fi transferată. Transfer fără verificare: dublu-tap → două intrări |
| D7 | Ce se întâmplă cu un aliment sau o rețetă arhivate după planificare? | **Masa rămâne și se poate transfera** (marcată `isArchived`). Planificarea nouă cere însă un aliment sau o rețetă vizibile | Blocarea transferului (409): utilizatorul a planificat masa și a mâncat-o; arhivarea alimentului nu schimbă acest fapt. Aceeași logică ca ingredientul arhivat păstrat în rețetă (ADR-006) |
| D8 | Cum se editează și se șterge? | `PUT` schimbă data, masa și cantitatea; **tipul nu se schimbă** (ADR-005 D7). `DELETE` este **fizic**: nimic nu referă o masă planificată. Editarea sau ștergerea unei mese transferate nu atinge intrarea din jurnal | Arhivarea meselor planificate: nimic nu le referă, deci n-ar proteja nimic |

---

## 3. Domain și Application

### 3.1 `PlannedMeal`
| Câmp | Masă-aliment (`kind = food`) | Masă-rețetă (`kind = recipe`) |
|---|---|---|
| `FoodItemId` | obligatoriu | NULL |
| `RecipeId` | NULL | obligatoriu |
| `QuantityGrams` (devine nullable) | obligatoriu | NULL |
| `Servings` (nou) | opțional (porțiile alimentului) | obligatoriu |

- `DiaryEntryId` (`Guid?`, nou): intrarea de jurnal creată prin transfer. Înlocuiește `IsTransferredToDiary`, care se elimină.
- `IsRecipe => RecipeId is not null`.
- `Nutrients()`: valorile nerotunjite, calculate acum: aliment → `Macros.Of(food, QuantityGrams)`; rețetă → `RecipeNutrition.Of(recipe).PerServing × Servings`. Alimentul, respectiv rețeta cu ingredientele și alimentele lor, trebuie încărcate.

### 3.2 Starea zilei (Domain/Nutrition)
```
PlanDayStatus.Of(totalKcal, targetKcal):
    totalKcal < 0,95 × targetKcal  → Under
    totalKcal > 1,05 × targetKcal  → Over
    altfel                         → Within
```
Pe valori nerotunjite. Exemplu: țintă 1700 → 1614,9 este `under`, 1615 și 1785 sunt `within`, 1785,1 este `over`.

### 3.3 Transferul în jurnal
- Se creează un `DiaryEntry` cu `UserId`, `FoodItemId` sau `RecipeId`, `ConsumedDate = PlanDate`, `MealType`, aceeași cantitate (`QuantityGrams` și `Servings` ale mesei planificate) și snapshot prin `RecalculateNutrients` (aliment sau rețetă).
- Data trebuie să respecte regula jurnalului (cel mult ziua de mâine, UTC); altfel 400 cu mesajul `PlannerErrors.FutureMeal`.
- Dacă `DiaryEntryId` nu e NULL, 409 cu mesajul `PlannerErrors.AlreadyEaten`.
- Intrarea nouă și referința `DiaryEntryId` se salvează în același `SaveChanges`, deci în aceeași tranzacție.
- Limitare asumată: două cereri `eat` *simultane* pe aceeași masă pot crea două intrări (nu există token de concurență). Clientul dezactivează butonul după prima apăsare; cazul secvențial (409) este acoperit.

### 3.4 Reguli de validare

| Câmp | Regulă |
|---|---|
| `foodId` / `recipeId` | exact unul (`PlannerErrors.ExactlyOneItem`) |
| cu `foodId` | exact una dintre `quantityGrams` / `servings`; `0 < quantityGrams ≤ 5000`; `0 < servings ≤ 50`; porțiile cer `ServingSizeGrams` (handler, 400); după conversie ≤ 5000 g, ca în jurnal |
| cu `recipeId` | `servings` obligatoriu, `0 < servings ≤ 50`; `quantityGrams` interzis |
| `date` | între 01.01.2000 și azi + 366 de zile |
| `mealType` | una dintre cele 4 valori |
| interval (`from`, `to`) | ambele obligatorii, `from ≤ to`, cel mult 31 de zile (inclusiv capetele), în același domeniu de date ca mai sus |
| `PUT` pe o masă-rețetă cu `quantityGrams` | 400 (handlerul cunoaște tipul, validatorul nu) |

### 3.5 Comenzi și interogări (MediatR)
- `CreatePlannedMealCommand`: validează; cu `foodId` încarcă alimentul vizibil, cu `recipeId` rețeta vizibilă (404 altfel); aplică cantitatea; întoarce `PlannedMealDto`.
- `UpdatePlannedMealCommand`: încarcă masa proprie (404 altfel) cu alimentul sau rețeta, chiar arhivate (D7); aplică data, masa și cantitatea.
- `DeletePlannedMealCommand`: ștergere fizică a mesei proprii.
- `EatPlannedMealCommand`: încarcă masa proprie, aplică regulile din §3.3, întoarce `DiaryEntryDto`.
- `GetPlannedMealQuery`: masa proprie cu valorile curente.
- `GetPlanQuery(from, to)`: mesele proprii din interval, cu alimentele și rețetele (inclusiv ingrediente), grupate pe zile și mese, ordonate după `CreatedAtUtc`; ținta din `ProfileLoader.CalculateTargets`; toate zilele intervalului apar, și cele fără mese.
- Accesul este centralizat în `PlannerAccess` (`LoadOwnAsync`, aplicarea cantității pe tip), după modelul `DiaryAccess`. Regula cantității pentru alimente refolosește `DiaryQuantity.Resolve`.

---

## 4. Persistență

Migrarea `Planner`, pe tabela `PlannedMeals` (goală până acum: nu exista API și seed-ul nu o populează):
- `QuantityGrams` devine nullable; `AddColumn Servings` (`double precision`, nullable); `DropColumn IsTransferredToDiary`.
- `AddColumn DiaryEntryId` (`uuid`, nullable), cheie străină spre `DiaryEntries` cu `ON DELETE SET NULL`, index unic (o intrare de jurnal provine din cel mult o masă planificată).
- Cheile străine spre `FoodItems` și `Recipes` trec de la `SET NULL` la `RESTRICT`: cu regula „exact o cheie”, `SET NULL` ar încălca constrângerea; alimentele și rețetele se arhivează, nu se șterg (ca în jurnal, ADR-006).
- Constrângeri `CHECK`:
  ```sql
  CK_PlannedMeals_FoodXorRecipe:  ("FoodItemId" IS NULL) <> ("RecipeId" IS NULL)
  CK_PlannedMeals_QuantityByKind: ("FoodItemId" IS NOT NULL AND "QuantityGrams" IS NOT NULL)
                               OR ("RecipeId" IS NOT NULL AND "QuantityGrams" IS NULL AND "Servings" IS NOT NULL)
  ```
- Indexul existent `(UserId, PlanDate)` servește interogarea pe interval.

---

## 5. Contract API

Toate rutele sunt `[Authorize]`, cu id-ul utilizatorului din claim-ul `sub`.

| Metodă și rută | Succes | Erori |
|---|---|---|
| `GET /api/plan?from=yyyy-MM-dd&to=yyyy-MM-dd` | 200 `PlanDto` | 400, 401 |
| `GET /api/plan/items/{id}` | 200 `PlannedMealDto` | 401, 404 |
| `POST /api/plan/items` | 201 `PlannedMealDto` | 400, 401, 404 |
| `PUT /api/plan/items/{id}` | 200 `PlannedMealDto` | 400, 401, 404 |
| `DELETE /api/plan/items/{id}` | 204 | 401, 404 |
| `POST /api/plan/items/{id}/eat` | 201 `DiaryEntryDto` | 400, 401, 404, 409 |

```jsonc
// SavePlannedMealRequest (POST); la PUT fără foodId / recipeId
{ "recipeId": "…", "date": "2026-10-19", "mealType": "lunch", "servings": 2 }

// PlannedMealDto
{ "id": "…", "kind": "recipe", "date": "2026-10-19", "mealType": "lunch",
  "quantityGrams": null, "servings": 2,
  "food": null,
  "recipe": { "id": "…", "name": "Ovăz cu lapte", "isArchived": false },
  "nutrients": { "calories": 389, "proteins": 16.9, "carbs": 66.3, "fats": 6.9 },
  "diaryEntryId": null }

// PlanDto
{ "from": "2026-10-19", "to": "2026-10-25",
  "days": [
    { "date": "2026-10-19",
      "meals": [ { "mealType": "breakfast", "items": [ /* PlannedMealDto */ ], "totals": { … } }, … ],
      "totals": { "calories": 1650, … },
      "targets": { "calories": 1700, … },
      "status": "within" } ],
  "isProfileComplete": true }
```

`food` și `recipe` refolosesc formele din jurnal (`DiaryFoodDto`, `DiaryRecipeDto`). Valorile nutriționale sunt rotunjite la 1 zecimală în răspuns, din valori nerotunjite; starea zilei se calculează din totalul nerotunjit.

---

## 6. Testare

| Nivel | Ce acoperă |
|---|---|
| `Domain.UnitTests` | `PlanDayStatus` (limitele 1614,9 / 1615 / 1785 / 1785,1 pentru ținta 1700), `PlannedMeal.Nutrients` pentru aliment și rețetă |
| `Application.UnitTests` | validatoarele (§3.4), handlerele (aliment invizibil, rețetă arhivată după planificare, transfer dublu, transfer în viitor, `PUT` cu grame pe rețetă), interogarea pe interval (zile goale, ordonare, totaluri, stare) |
| `WebApi.IntegrationTests` | câte un test pe AC, pe PostgreSQL real, inclusiv `ON DELETE SET NULL` la ștergerea intrării de jurnal (AC5) și cele două `CHECK`-uri (AC9, prin SQL direct) |

Verificare numerică: rețeta SRS (200 g ovăz, 4 porții, 194,5 kcal/porție) planificată pentru 2 porții → 389 kcal; transferată → intrare de jurnal cu 389 kcal și `servings = 2`.

---

## 7. Documentație livrată

- ADR-007 (deciziile D1–D8).
- `architecture_and_contracts.md`: secțiunea Planificator, ERD-ul `PLANNED_MEALS` actualizat.
- `ROADMAP.md`: rețetar și planificator bifate; pasul următor: lista de cumpărături (FR-12).
