# Spec: Lista de cumpărături agregată (backend)

**Data:** 2026-10-16
**Branch:** `feature/NUT-shopping-list`
**Cerințe SRS:** UC05, FR-12 (generarea listei de cumpărături agregate)
**Faza din roadmap:** Faza 2 (Backend core), ciclul 3 din 3 al zonei Rețetar / Planificator / Listă de cumpărături
**Depinde de:** catalogul de alimente (ADR-004), rețetarul (ADR-006), planificatorul (ADR-007)

---

## 1. Scop

### User story
> Ca utilizator NutriScan, vreau să generez din planul săptămânii lista de cumpărături, cu ingredientele identice adunate și grupate pe raioane, ca să le bifez în magazin și să adaug ce mai lipsește.

### Criterii de acceptanță
- **AC1 (criteriul SRS):** un plan cu două rețete care conțin 150 g și 250 g de piept de pui (câte o porție planificată din fiecare, rețete de o porție) generează un singur articol „Piept de pui — 400 g”.
- **AC2 (scalare):** o rețetă de 4 porții cu 200 g de ovăz, planificată pentru 2 porții, contribuie cu 100 g de ovăz; un aliment planificat direct contribuie cu gramele lui.
- **AC3:** `POST /api/shopping-list/generate` cu `{ from, to }` întoarce 200 cu lista; articolele generate sunt grupate pe categorii, în ordinea fixă a categoriilor, iar în categorie după nume.
- **AC4:** generarea înlocuiește articolele generate anterior și păstrează articolele manuale. Mesele deja trecute în jurnal (`diaryEntryId` setat) nu intră în listă.
- **AC5 (UC05 A1):** dacă intervalul nu are nicio masă planificată nemâncată, generarea întoarce 400 cu mesajul `ShoppingListErrors.NothingPlanned`, iar lista existentă rămâne neschimbată.
- **AC6:** un articol se bifează și se debifează (`PATCH`), un articol manual se adaugă (`POST`) cu nume, cantitate text liberă și categorie, orice articol se șterge (`DELETE`), iar `DELETE /api/shopping-list/purchased` șterge toate articolele bifate.
- **AC7:** alimentele au o categorie (`category`, opțională la creare și editare, implicit `other`), vizibilă în `FoodDto`. Alimentele globale din seed primesc categorii.
- **AC8:** lista și articolele altui utilizator nu sunt accesibile (404 pe articol); toate rutele cer access token (401); cererile invalide primesc 400.
- **AC9:** baza de date respinge, prin `CHECK`, un articol generat fără grame sau cu cantitate text și un articol manual cu grame.

### În afara scopului
Conversii de unități (UC05 A2: toate cantitățile din catalog sunt în grame, deci nu există unități incompatibile de agregat; articolele manuale au cantitate text și nu se agregă), mai multe liste per utilizator, partajarea listei, prețuri, păstrarea bifelor la regenerare, ecranele din client.

---

## 2. Decizii (rezumat pentru ADR-008)

| # | Întrebare | Decizie | Alternative respinse |
|---|---|---|---|
| D1 | Lista se calculează la citire sau se salvează? | **Se salvează** (o listă curentă per utilizator, ca articole): bifele și articolele manuale trebuie să persiste între deschideri (postcondiția UC05: „lista este salvată”) | Calcul la citire din plan, ca la rețete (ADR-006 D1): bifele n-ar avea unde să stea și s-ar pierde la orice editare a planului |
| D2 | Ce face o nouă generare? | **Înlocuiește articolele generate și păstrează articolele manuale.** Articolele generate sunt un snapshot al planului din momentul generării | Adăugarea la lista existentă: aceleași ingrediente ar apărea de două ori. Ștergerea totală: utilizatorul pierde articolele scrise de mână. Păstrarea bifelor pe aliment: o bifă pusă pe 200 g ar ascunde cantitatea nouă de 400 g |
| D3 | Cum se agregă? | **După aliment (`FoodItemId`), în grame.** Aliment planificat → gramele mesei; rețetă planificată → pentru fiecare ingredient `grame × porții planificate / porțiile rețetei` | Agregare după nume: „Piept de pui” global și unul propriu cu același nume s-ar uni greșit, iar două denumiri ale aceluiași aliment n-ar fi unite |
| D4 | Ce mese intră? | **Mesele planificate din interval care nu au fost încă trecute în jurnal.** Intervalul are aceleași limite ca planul (cel mult 31 de zile) | Toate mesele: o masă deja mâncată a fost deja cumpărată. Doar mesele viitoare: o masă de azi încă nemâncată trebuie totuși cumpărată |
| D5 | De unde vine categoria? | **Câmp nou pe aliment: `FoodCategory`** (enum: `fruitsVegetables`, `meatFish`, `dairy`, `grains`, `pantry`, `beverages`, `other`), opțional, implicit `other`; seed-ul primește categorii prin migrare | Categorii deduse din nume: fragil. Categorii libere (text): „Lactate” și „lactate” ar fi raioane diferite. Tabel separat de categorii administrat: nu există cerință pentru administrarea lor |
| D6 | Cum se rotunjesc gramele? | Valoarea nerotunjită se stochează; **în răspuns se rotunjește în sus la gram întreg** (pentru cumpărături contează să nu lipsească: 33,3 g → 34 g) | Rotunjire la o zecimală ca la nutrimente: „33,3 g de ovăz” nu are sens la raft |
| D7 | Ce nume are un articol generat? | **Numele alimentului la generare** (snapshot), plus referința `FoodItemId` | Doar referința: lista s-ar schimba singură dacă alimentul e redenumit, deși e un snapshot (D2) |
| D8 | Plan gol (UC05 A1) | **400 cu mesaj, lista neschimbată** | 200 cu listă goală: ar șterge articolele generate anterior fără ca utilizatorul să primească ceva în schimb |

---

## 3. Domain și Application

### 3.1 `FoodItem`
- `Category` (`FoodCategory`, implicit `Other`). Seed: pui și somon → `MeatFish`; ovăz și orez → `Grains`; ou și iaurt → `Dairy`; banană și broccoli → `FruitsVegetables`; ulei și migdale → `Pantry`.
- `IFoodFields` primește `FoodCategory? Category` (null la cerere → `Other`); `FoodDto` primește `Category`.

### 3.2 `ShoppingListItem`
| Câmp | Articol generat | Articol manual |
|---|---|---|
| `FoodItemId` (nou, nullable, FK `RESTRICT`) | obligatoriu | NULL |
| `ItemName` (≤ 200) | numele alimentului la generare | introdus de utilizator (1–200 după trim) |
| `QuantityGrams` (devine nullable) | obligatoriu, nerotunjit | NULL |
| `QuantityText` (nou, ≤ 50) | NULL | opțional („2 bucăți”) |
| `Category` (nou) | categoria alimentului | aleasă de utilizator (implicit `Other`) |
| `IsPurchased` | `false` la generare | `false` la adăugare |

- `IsManual => FoodItemId is null`.

### 3.3 Agregarea (Domain/Nutrition: `ShoppingListBuilder`)
Funcție pură:
```
Build(meals) → listă de (FoodItem food, double grams):
    pentru fiecare masă:
        aliment → (food, QuantityGrams)
        rețetă  → pentru fiecare ingredient: (ingredient.FoodItem, ingredient.QuantityGrams × meal.Servings / recipe.ServingsCount)
    grupare după food.Id, suma gramelor
```
Exemplu AC2: 200 × 2 / 4 = 100 g. Exemplu AC1: 150 + 250 = 400 g.

### 3.4 Reguli de validare

| Câmp | Regulă |
|---|---|
| `from`, `to` (generare) | ca la plan: date între 01.01.2000 și azi + 366, `from ≤ to`, cel mult 31 de zile |
| articol manual: `name` | obligatoriu după trim, ≤ 200 |
| articol manual: `quantity` | opțional, ≤ 50 |
| `category` (articol manual, aliment) | una dintre valorile enum-ului |
| `PATCH`: `isPurchased` | obligatoriu |

### 3.5 Comenzi și interogări (MediatR)
- `GenerateShoppingListCommand(UserId, From, To)`: încarcă mesele nemâncate din interval cu alimentele și rețetele (ingrediente incluse); dacă nu există, 400 `NothingPlanned`; altfel șterge articolele generate ale utilizatorului, adaugă articolele noi și salvează într-un singur `SaveChanges`; întoarce lista.
- `GetShoppingListQuery(UserId)`: articolele utilizatorului, grupate pe categorii (ordinea enum-ului), în categorie după nume, apoi după `Id`.
- `AddShoppingListItemCommand(UserId, Name, Quantity, Category)`: articol manual; întoarce articolul.
- `SetShoppingListItemPurchasedCommand(UserId, ItemId, IsPurchased)`: articolul propriu (404 altfel).
- `DeleteShoppingListItemCommand(UserId, ItemId)`: articolul propriu (404 altfel).
- `ClearPurchasedItemsCommand(UserId)`: șterge articolele bifate ale utilizatorului.

---

## 4. Persistență

Migrarea `ShoppingList`:
- `FoodItems`: `AddColumn Category` (`integer`, implicit 6 = `Other`; 0 este `FruitsVegetables`), plus `UPDATE` pe numele celor 10 alimente globale din seed (§3.1). `ApplicationDbContextSeed` setează aceleași categorii pentru bazele noi.
- `ShoppingListItems` (goală până acum: nu exista API): `QuantityGrams` devine nullable; `AddColumn FoodItemId` (FK `RESTRICT`, index), `QuantityText` (`character varying(50)`), `Category` (`integer`); index `(UserId, Category)`.
- Constrângere `CHECK`:
  ```sql
  CK_ShoppingListItems_GeneratedOrManual:
      ("FoodItemId" IS NOT NULL AND "QuantityGrams" IS NOT NULL AND "QuantityText" IS NULL)
   OR ("FoodItemId" IS NULL AND "QuantityGrams" IS NULL)
  ```
- Enum-urile se stochează ca `integer`, ca `MealType`.

---

## 5. Contract API

Toate rutele sunt `[Authorize]`.

| Metodă și rută | Succes | Erori |
|---|---|---|
| `GET /api/shopping-list` | 200 `ShoppingListDto` | 401 |
| `POST /api/shopping-list/generate` | 200 `ShoppingListDto` | 400, 401 |
| `POST /api/shopping-list/items` | 201 `ShoppingListItemDto` | 400, 401 |
| `PATCH /api/shopping-list/items/{id}` | 200 `ShoppingListItemDto` | 400, 401, 404 |
| `DELETE /api/shopping-list/items/{id}` | 204 | 401, 404 |
| `DELETE /api/shopping-list/purchased` | 204 | 401 |

```jsonc
// POST /generate
{ "from": "2026-10-19", "to": "2026-10-25" }

// POST /items
{ "name": "Hârtie de copt", "quantity": "1 rolă", "category": "other" }

// PATCH /items/{id}
{ "isPurchased": true }

// ShoppingListItemDto
{ "id": "…", "name": "Piept de pui", "quantityGrams": 400, "quantityText": null,
  "category": "meatFish", "isPurchased": false, "isManual": false, "foodId": "…" }

// ShoppingListDto
{ "categories": [ { "category": "meatFish", "items": [ /* ShoppingListItemDto */ ] }, … ],
  "totalCount": 7, "purchasedCount": 2 }
```

`quantityGrams` este rotunjit în sus la gram întreg (D6). Doar categoriile cu articole apar în `categories`. `SaveFoodRequest` și `FoodDto` primesc `category`.

---

## 6. Testare

| Nivel | Ce acoperă |
|---|---|
| `Domain.UnitTests` | `ShoppingListBuilder` (AC1: 150 + 250 = 400; AC2: scalare 200 × 2 / 4 = 100; aliment direct; aliment comun între o rețetă și o masă-aliment) |
| `Application.UnitTests` | generarea (înlocuire, păstrarea manualelor, excluderea meselor mâncate, plan gol fără modificări), comenzile pe articole (proprietate, bifare, ștergerea celor bifate), validatoarele, categoria alimentului la creare/editare |
| `WebApi.IntegrationTests` | câte un test pe AC, pe PostgreSQL real, inclusiv `CHECK`-ul (AC9) și categoriile seed-ului după migrare |

---

## 7. Documentație livrată

- ADR-008 (deciziile D1–D8).
- `architecture_and_contracts.md`: secțiunea Listă de cumpărături, `category` în contractul alimentelor, ERD actualizat.
- `ROADMAP.md`: lista de cumpărături bifată; pasul următor.
