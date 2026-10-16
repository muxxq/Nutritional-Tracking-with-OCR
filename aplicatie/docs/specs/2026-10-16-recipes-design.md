# Spec: Rețetarul și rețetele în jurnal (backend)

**Data:** 2026-10-16
**Branch:** `feature/NUT-recipes`
**Cerințe SRS:** UC03, FR-09 (rețete și calcul nutrițional automat); FR-07 completat: intrări de jurnal din rețete
**Faza din roadmap:** Faza 2 (Backend core), ciclul 1 din 3 al zonei Rețetar / Planificator / Listă de cumpărături
**Depinde de:** autentificare (ADR-002), catalogul de alimente (ADR-004), jurnalul alimentar (ADR-005)

---

## 1. Scop

### User story
> Ca utilizator NutriScan, vreau să-mi salvez rețetele din alimentele din catalog și să văd automat valorile totale și per porție, apoi să adaug o porție de rețetă în jurnal dintr-o singură acțiune, fără să introduc fiecare ingredient.

### Criterii de acceptanță
- **AC1:** `POST /api/recipes` cu 200 g fulgi de ovăz (389 kcal/100 g) și 4 porții întoarce 201 (`Location: /api/recipes/{id}`), cu `total.calories = 778` și `perServing.calories = 194.5` (criteriul FR-09 din SRS).
- **AC2:** `PUT /api/recipes/{id}` înlocuiește integral câmpurile și lista de ingrediente (200); valorile se recalculează.
- **AC3 (calcul la citire):** după editarea unui aliment din catalog, următorul `GET` al rețetei care îl conține întoarce valorile noi. Intrările de jurnal deja salvate din acea rețetă **nu** se schimbă (snapshot).
- **AC4 (arhivare):** `DELETE /api/recipes/{id}` întoarce 204. Rețeta nu mai apare în listă și primește 404 la `GET`, `PUT`, `DELETE` și la adăugarea în jurnal. Intrările de jurnal existente rămân în zi, cu `recipe.isArchived = true`, și pot fi editate sau șterse.
- **AC5:** `POST /api/diary/entries` cu `recipeId` și `servings = 1.5` creează o intrare `kind = "recipe"` cu valorile `perServing × 1.5`. Ziua include intrarea în totalul mesei și al zilei.
- **AC6:** rețeta altui utilizator primește 404 la `GET`, `PUT`, `DELETE` și la adăugarea în jurnal (NFR-10). Un ingredient cu un aliment invizibil (al altui utilizator sau arhivat, dacă nu era deja în rețetă) primește 404.
- **AC7:** cererile invalide (§3.4) primesc 400 ProblemDetails.
- **AC8:** baza de date respinge, prin constrângeri `CHECK`, o intrare de jurnal cu ambele chei (sau niciuna) și o intrare-rețetă cu `QuantityGrams` sau fără `Servings`, chiar dacă scrierea ocolește API-ul.
- **AC9:** toate rutele `/api/recipes` cer access token valid (401).

### În afara scopului
Planificatorul săptămânal și transferul „Am mâncat asta” (FR-10, FR-11, ciclul 2), lista de cumpărături (FR-12, ciclul 3), rețete recente, cantitate în grame pentru rețete (necesită greutatea gătită), rețete globale administrate, imagini, alte nutrimente decât energia și cele 3 macronutrimente, ecranele din client.

---

## 2. Decizii (rezumat pentru ADR-006)

| # | Întrebare | Decizie | Alternative respinse |
|---|---|---|---|
| D1 | Unde se calculează valorile rețetei? | **La citire**, din valorile curente ale alimentelor, cu o funcție pură din Domain; nimic stocat pe `Recipe`. Rețeta e un șablon care urmează catalogul; faptul istoric (intrarea din jurnal) e înghețat prin snapshot (D1 din ADR-005) | Totaluri stocate la salvarea rețetei (devin inconsistente când se corectează un aliment); totaluri stocate plus recalcul prin evenimente de domeniu la editarea alimentului (propagare și cod în plus fără o cerință care să le justifice) |
| D2 | Cum intră o rețetă în jurnal? | **Intrare polimorfă:** `DiaryEntry` are exact una dintre cheile `FoodItemId` / `RecipeId`, garantată de un `CHECK` în PostgreSQL | Explodarea rețetei în intrări per ingredient (se pierde gruparea); generarea unui `FoodItem` ascuns din rețetă (alimente „fantomă” în catalog, sincronizare la fiecare editare) |
| D3 | În ce unitate se adaugă o rețetă în jurnal? | **Doar porții**; `QuantityGrams` rămâne NULL la intrările-rețetă | Grame raportate la greutatea crudă a ingredientelor (greșit nutrițional pentru mâncăruri gătite: orezul fiert cântărește de aproximativ 3 ori mai mult); grame raportate la o greutate gătită introdusă de utilizator (câmp, reguli și teste în plus; rămâne dezvoltare viitoare) |
| D4 | Ce face `DELETE` pe o rețetă? | **Arhivare**, ca la alimente (D2 din ADR-004): rețeta e referită de intrări de jurnal (și, în ciclul 2, de mese planificate) | Ștergere fizică cu numele copiat în intrare (intrarea ar rămâne fără aliment și fără rețetă, regula „exact una” ar trebui slăbită); interzicerea ștergerii rețetelor folosite, 409 (utilizatorul nu mai poate scăpa de o rețetă mâncată o dată) |
| D5 | Cum se editează ingredientele? | **Prin agregat:** `PUT` pe toată rețeta înlocuiește lista de ingrediente. Rețeta e rădăcina agregatului (*aggregate root*), ingredientele nu au sens independent | Endpoint-uri separate `/api/recipes/{id}/ingredients/{ingredientId}` (mai multe request-uri pentru un singur formular, stări intermediare incomplete) |
| D6 | Același aliment de mai multe ori într-o rețetă? | **Interzis** (400 și index unic `(RecipeId, FoodItemId)`) | Permis (listă confuză în UI; lista de cumpărături ar trebui oricum să le unească) |
| D7 | Editări simultane ale aceleiași rețete | **Ultima scriere câștigă**; limitare asumată: o rețetă e editată de un singur utilizator, rar | Token de concurență optimistă și 409 (justificat la refresh token-uri, ADR-002, nu aici) |

---

## 3. Domain și Application

### 3.1 `Recipe`
- Câmpuri noi: `PrepTimeMinutes` (`int?`, afișat de ecranul existent din client) și `ArchivedAtUtc` (`DateTime?`, setter privat).
- `SearchText`: nume fără diacritice, cu litere mici, recalculat la setarea numelui (`TextNormalizer.ForSearch`), ca la `FoodItem`.
- `Archive(DateTime nowUtc)` și `IsVisibleTo(Guid userId) => ArchivedAtUtc is null && UserId == userId`, după modelul `FoodItem`.

### 3.2 `RecipeNutrition` (Domain/Nutrition)
Funcție pură, fără rotunjire:
```
total      = Σ ingredient: aliment.per100g × grame / 100     (Calories, Proteins, Carbohydrates, Fats)
perServing = total / ServingsCount
```
Rezultatul conține și contribuția fiecărui ingredient, pentru afișare.

Exemplu SRS: 200 g ovăz × 389 / 100 = 778 kcal; 778 / 4 = 194,5 kcal per porție.

### 3.3 `DiaryEntry`
| Câmp | Intrare aliment (`kind = food`) | Intrare rețetă (`kind = recipe`) |
|---|---|---|
| `FoodItemId` (devine nullable) | obligatoriu | NULL |
| `RecipeId` (nou, nullable) | NULL | obligatoriu |
| `QuantityGrams` (devine nullable) | obligatoriu | NULL |
| `Servings` | opțional (porțiile alimentului) | obligatoriu |

- `RecalculateNutrients(Recipe recipe)`: snapshot = `RecipeNutrition(recipe).perServing × Servings`, nerotunjit (D4 din ADR-005). Rețeta trebuie încărcată cu ingredientele și alimentele lor.
- `RecalculateNutrients(FoodItem food)` rămâne neschimbată.
- Tipul intrării nu se poate schimba la editare (consecvent cu D7 din ADR-005).

### 3.4 Reguli de validare

| Câmp | Regulă |
|---|---|
| `name` | obligatoriu după trim, ≤ 200 caractere |
| `description` | opțional, ≤ 1000 caractere |
| `instructions` | opțional, ≤ 10 000 caractere |
| `servingsCount` | 1–50 |
| `prepTimeMinutes` | opțional, 0–1440 |
| `ingredients` | 1–50 elemente; `foodId` distincte; `0 < quantityGrams ≤ 5000` |
| jurnal: `foodId` / `recipeId` | exact unul |
| jurnal, cu `recipeId` | `servings` obligatoriu, `0 < servings ≤ 50`; `quantityGrams` interzis |
| jurnal, cu `foodId` | regulile din spec-ul jurnalului (§3.3), neschimbate |
| `q`, `page`, `pageSize` (listă) | ca la căutarea alimentelor (`SearchFoodsQueryValidator`) |

Regulile care nu depind de baza de date stau în validatoare FluentValidation. Vizibilitatea alimentelor și a rețetelor se verifică în handler (404).

### 3.5 Comenzi și interogări (MediatR)
- `CreateRecipeCommand`: validează, încarcă toate alimentele ingredientelor într-un singur query, verifică vizibilitatea fiecăruia (404 la primul invizibil), creează rețeta, întoarce `RecipeDto`.
- `UpdateRecipeCommand`: încarcă rețeta proprie cu ingredientele (404 altfel). Un aliment e acceptat dacă e vizibil **sau dacă era deja ingredient al rețetei** (aliment arhivat după adăugare). Înlocuiește câmpurile și ingredientele; ștergerea celor vechi și inserarea celor noi au loc în același `SaveChanges`, deci în aceeași tranzacție.
- `ArchiveRecipeCommand`: încarcă rețeta vizibilă (404 altfel, inclusiv dacă e deja arhivată), o arhivează.
- `GetRecipeQuery`: rețeta vizibilă cu ingredientele, contribuțiile, totalul și valorile per porție.
- `SearchRecipesQuery`: rețetele proprii nearhivate, filtrate opțional după nume (fără diacritice, ca la alimente), ordonate după nume, paginate cu `PagedResult`. Valorile per porție se calculează în SQL, prin proiecție (`Sum(food.Calories * grams / 100) / ServingsCount`), fără să încarce ingredientele în memorie.
- `AddDiaryEntryCommand`: cu `foodId`, comportament neschimbat; cu `recipeId`, încarcă rețeta vizibilă (404 altfel) cu ingredientele și calculează snapshot-ul.
- `UpdateDiaryEntryCommand`: la o intrare-rețetă se pot schimba porțiile, data și masa; recalculul folosește rețeta curentă, chiar arhivată.
- `GetDiaryDayQuery` și `GetDiaryEntryQuery` includ intrările-rețetă. `GetRecentFoodsQuery` ia în calcul doar intrările-aliment.
- Accesul la rețete este centralizat în `RecipeAccess` (`LoadOwnAsync`, `LoadVisibleAsync`), după modelul `FoodAccess`. Aplicarea cantității în `DiaryAccess` se separă pe tip: aliment → `DiaryQuantity.Resolve`, rețetă → porții.

---

## 4. Persistență

Migrarea `Recipes`:
- `Recipes`: `AddColumn PrepTimeMinutes` (`integer`, nullable), `AddColumn ArchivedAtUtc` (`timestamp with time zone`, nullable), `AddColumn SearchText` (`character varying(200)`, cu index; tabelul e gol până acum, deci valoarea implicită `''` nu afectează date reale); `Description` ≤ 1000.
- `RecipeIngredients`: index unic `(RecipeId, FoodItemId)`.
- `DiaryEntries`: `FoodItemId` și `QuantityGrams` devin nullable; `AddColumn RecipeId` (`uuid`, nullable) cu cheie străină spre `Recipes`, `ON DELETE RESTRICT`, și index.
- Constrângeri `CHECK` pe `DiaryEntries`:
  ```sql
  CK_DiaryEntries_FoodXorRecipe: ("FoodItemId" IS NULL) <> ("RecipeId" IS NULL)
  CK_DiaryEntries_QuantityByKind:
      ("FoodItemId" IS NOT NULL AND "QuantityGrams" IS NOT NULL)
   OR ("RecipeId" IS NOT NULL AND "QuantityGrams" IS NULL AND "Servings" IS NOT NULL)
  ```
- Intrările existente sunt toate intrări-aliment cu `QuantityGrams` completat, deci respectă constrângerile; migrarea nu transformă date.
- `RESTRICT` pe `RecipeId` nu se declanșează în funcționarea normală (rețetele se arhivează), dar blochează o ștergere accidentală care ar lăsa intrări orfane. Ștergerea utilizatorului rămâne în cascadă, prin `UserId`.

---

## 5. Contract API

Toate rutele sunt `[Authorize]`, cu id-ul utilizatorului din claim-ul `sub`.

| Metodă și rută | Succes | Erori |
|---|---|---|
| `GET /api/recipes?q=&page=1&pageSize=20` | 200 `PagedResult<RecipeSummaryDto>` | 400, 401 |
| `GET /api/recipes/{id}` | 200 `RecipeDto` | 401, 404 |
| `POST /api/recipes` | 201 `RecipeDto` | 400, 401, 404 |
| `PUT /api/recipes/{id}` | 200 `RecipeDto` | 400, 401, 404 |
| `DELETE /api/recipes/{id}` | 204 | 401, 404 |

```jsonc
// SaveRecipeRequest (POST și PUT)
{ "name": "Ovăz cu lapte", "description": null, "instructions": "Se fierbe 5 minute.",
  "servingsCount": 4, "prepTimeMinutes": 10,
  "ingredients": [ { "foodId": "…", "quantityGrams": 200 } ] }

// RecipeDto
{ "id": "…", "name": "Ovăz cu lapte", "description": null, "instructions": "Se fierbe 5 minute.",
  "servingsCount": 4, "prepTimeMinutes": 10, "isArchived": false,
  "ingredients": [
    { "food": { "id": "…", "name": "Fulgi de ovăz", "brand": "Generic", "isArchived": false },
      "quantityGrams": 200,
      "nutrients": { "calories": 778, "proteins": 33.8, "carbs": 132.6, "fats": 13.8 } } ],
  "total":      { "calories": 778,   "proteins": 33.8, "carbs": 132.6, "fats": 13.8 },
  "perServing": { "calories": 194.5, "proteins": 8.5,  "carbs": 33.2,  "fats": 3.5 } }

// RecipeSummaryDto
{ "id": "…", "name": "Ovăz cu lapte", "servingsCount": 4, "prepTimeMinutes": 10,
  "ingredientCount": 1, "perServing": { "calories": 194.5, "proteins": 8.5, "carbs": 33.2, "fats": 3.5 } }
```

Schimbări în contractul jurnalului:
```jsonc
// AddDiaryEntryRequest: exact unul dintre foodId / recipeId
{ "recipeId": "…", "date": "2026-10-16", "mealType": "lunch", "servings": 1 }

// UpdateDiaryEntryRequest: neschimbat (date, mealType, quantityGrams, servings);
// la o intrare-rețetă, quantityGrams trebuie să lipsească

// DiaryEntryDto: câmp nou "kind" ("food" | "recipe"); exact unul dintre "food" / "recipe" e completat
{ "id": "…", "kind": "recipe", "date": "2026-10-16", "mealType": "lunch",
  "quantityGrams": null, "servings": 1,
  "food": null,
  "recipe": { "id": "…", "name": "Ovăz cu lapte", "isArchived": false },
  "nutrients": { "calories": 194.5, "proteins": 8.5, "carbs": 33.2, "fats": 3.5 } }
```

`kind` permite clientului TypeScript o uniune discriminată (`entry.kind === "recipe"` → `entry.recipe` nu e null). Schimbarea e incompatibilă (`food` și `quantityGrams` devin nullable), dar acum nu are consumatori: clientul folosește încă date mock (Faza 3).

Toate valorile nutriționale din DTO sunt rotunjite la 1 zecimală, din valori nerotunjite; `perServing` se calculează din totalul nerotunjit (D4 din ADR-005).

---

## 6. Testare

| Nivel | Ce acoperă |
|---|---|
| `Domain.UnitTests` | `RecipeNutrition` (exemplul SRS, mai multe ingrediente, împărțirea la porții), `DiaryEntry.RecalculateNutrients(Recipe)`, `Recipe.IsVisibleTo` și `Archive` |
| `Application.UnitTests` | validatoarele (§3.4), handlerele rețetarului (aliment invizibil, aliment arhivat păstrat la `PUT`, arhivare repetată), ramurile noi din handlerele jurnalului |
| `WebApi.IntegrationTests` | câte un test pe AC, pe PostgreSQL real (Testcontainers), inclusiv migrarea `Recipes` și cele două constrângeri `CHECK` (AC8, prin SQL direct) |

Testele existente ale jurnalului trebuie să treacă neschimbate, cu excepția aserțiunilor despre forma `DiaryEntryDto` (`kind`, `recipe`, `food` nullable).

Verificare numerică pentru AC1 și AC5: fulgi de ovăz (389 kcal, 16,9 P, 66,3 C, 6,9 G per 100 g), 200 g, 4 porții → total 778 kcal; per porție 194,5 kcal. O intrare de 1,5 porții → 291,75 kcal, afișat 291,8. Testele verifică energia exact și macronutrimentele cu toleranță de 0,1, pentru că valori ca 8,45 g nu au reprezentare binară exactă și rotunjirea la o zecimală poate cădea pe oricare parte.

---

## 7. Documentație livrată

- ADR-006 (deciziile D1–D7).
- `architecture_and_contracts.md`: contractul rețetarului și schimbările din jurnal.
- `ROADMAP.md`: progresul pe FR-09; bifarea regulii energetice (implementată deja în `EnergyCheck`, ca avertisment în Domain, nu ca eroare FluentValidation).
