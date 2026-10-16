# Spec: Jurnalul alimentar (backend)

**Data:** 2026-10-16
**Branch:** `feature/NUT-food-diary`
**Cerințe SRS:** UC02, FR-07 (gestiunea intrărilor, doar alimente); FR-08 doar parțial: totalurile zilnice față de ținte; statisticile săptămânale și lunare au spec separat
**Faza din roadmap:** Faza 2 (Backend core), Sprint 2
**Depinde de:** autentificare (ADR-002), profil și ținte (ADR-003), catalogul de alimente (ADR-004)

---

## 1. Scop

### User story
> Ca utilizator NutriScan, vreau să notez ce mănânc la fiecare masă, în grame sau în porții, și să văd cât am consumat azi față de țintele mele, ca să știu cât mai pot mânca.

### Criterii de acceptanță
- **AC1:** `POST /api/diary/entries` cu un aliment vizibil și o cantitate în grame creează intrarea (201, `Location: /api/diary/entries/{id}`). Valorile nutriționale ale intrării sunt `valoare_per_100g × grame / 100`.
- **AC2:** cu `servings`, gramele sunt `servings × ServingSizeGrams`, iar intrarea păstrează și numărul de porții. Dacă alimentul nu are porție definită, răspunsul este 400.
- **AC3:** `GET /api/diary/days/{date}` întoarce mereu cele 4 mese în ordinea Breakfast, Lunch, Dinner, Snack (și cele goale), fiecare cu intrările și subtotalul ei, plus totalul zilei, țintele, restul (`ținte − total`, poate fi negativ) și `isProfileComplete`. O zi fără intrări întoarce totaluri zero.
- **AC4 (snapshot):** editarea unui aliment din catalog nu modifică intrările deja salvate. Editarea cantității unei intrări recalculează valorile din alimentul curent.
- **AC5:** `PUT /api/diary/entries/{id}` modifică cantitatea, masa și data (200). `DELETE` întoarce 204, iar intrarea nu mai apare nici în zi, nici la `GET` (404).
- **AC6:** intrarea altui utilizator primește 404 la `GET`, `PUT` și `DELETE` (NFR-10). Un aliment al altui utilizator sau unul arhivat primește 404 la `POST`. O intrare existentă al cărei aliment a fost arhivat rămâne în zi, cu `food.isArchived = true`, și poate fi editată sau ștearsă.
- **AC7:** cereri invalide (§3.3) primesc 400 ProblemDetails.
- **AC8:** `GET /api/diary/recent-foods` întoarce alimentele distincte folosite recent de utilizator, ordonate după ultima folosire, fără cele arhivate, fiecare cu ultima cantitate folosită.
- **AC9:** toate rutele `/api/diary` cer access token valid (401).

### În afara scopului
Rețetele în jurnal (vin odată cu modulul de rețete, FR-09), statisticile săptămânale și lunare (FR-08, spec separat), istoricul țintelor, transferul din planificator (FR-11), copierea unei mese sau a unei zile, ecranele din client.

---

## 2. Decizii (rezumat pentru ADR-005)

| # | Întrebare | Decizie | Alternative respinse |
|---|---|---|---|
| D1 | Ce se întâmplă cu intrările vechi când alimentul se schimbă? | **Snapshot la salvare:** intrarea păstrează valorile calculate în momentul adăugării; se recalculează din alimentul curent doar când se editează cantitatea intrării | Calcul la citire din aliment (istoricul se schimbă retroactiv, coloanele `Calculated*` devin inutile); snapshot cu propagare opțională (cod și teste în plus fără o cerință care să le justifice) |
| D2 | Grame sau porții? | **Ambele la intrare**, stocate ca grame (sursa de adevăr) plus numărul opțional de porții, pentru afișare | Doar grame (UI-ul nu poate reafișa „2 porții” la editare) |
| D3 | Forma citirii | **Un endpoint agregat pe zi** (model de citire în forma ecranului, CQRS) | Endpoint-uri separate pentru intrări și sumar (2 request-uri, două surse care pot diverge); agregare SQL `GROUP BY` (prematură pentru o zi; rămâne pentru statisticile lunare) |
| D4 | Rotunjirea | **Valori nerotunjite în baza de date**, rotunjire la 1 zecimală doar în DTO; totalurile se adună din valorile nerotunjite | Rotunjire pe intrare (eroarea se acumulează în total) |
| D5 | Ținte pentru zilele trecute | **Țintele curente**, calculate din profilul de azi (`CalculateBestEffort`); limitare asumată | Tabel de istoric al țintelor (necesar abia pentru statistici, FR-08) |
| D6 | Ștergerea | **Fizică** (nimic nu referă o intrare de jurnal) | Arhivare (complexitate fără beneficiu; diferă de D2 din ADR-004 pentru că alimentele sunt referite, intrările nu) |
| D7 | Schimbarea alimentului unei intrări | **Nu se permite**; se șterge intrarea și se adaugă alta | `foodId` în `PUT` (ar amesteca două operații și ar complica regula de snapshot) |

---

## 3. Domain și Application

### 3.1 `DiaryEntry`
- Câmp nou: `Servings` (`double?`).
- `RecalculateNutrients(FoodItem food)` primește alimentul explicit și **nu mai rotunjește**:
  ```
  ratio = QuantityGrams / 100
  CalculatedCalories = food.Calories × ratio   (la fel pentru Proteins, Carbs, Fats)
  ```
- Metoda nu mai depinde de proprietatea de navigare `FoodItem` încărcată (fără `if (FoodItem == null) return;` tăcut).

### 3.2 `DiaryQuantity` (Domain/Nutrition)
Funcție pură care transformă cererea în grame:
```
Resolve(grams?, servings?, servingSizeGrams?) → (QuantityGrams, Servings?) sau eroare

grams și servings ambele prezente sau ambele lipsă → eroare „exact una dintre cantități”
grams prezent                                       → (grams, null)
servings prezent, servingSizeGrams lipsă            → eroare „alimentul nu are porție definită”
servings prezent                                    → (servings × servingSizeGrams, servings)
```
Limita de 5000 g se verifică după conversie.

### 3.3 Reguli de validare

| Câmp | Regulă |
|---|---|
| `quantityGrams` / `servings` | exact unul; `0 < grame ≤ 5000`; `0 < porții ≤ 50`; după conversie `≤ 5000 g` |
| `date` | `2000-01-01 ≤ date ≤ azi (UTC) + 1 zi`; ziua în plus acoperă fusurile orare, serverul nu știe ora locală a clientului |
| `mealType` | `Breakfast`, `Lunch`, `Dinner` sau `Snack` (text); altfel 400 |
| `limit` (recente) | `1–30`, implicit 10 |

Regulile care nu depind de baza de date stau în validatoare FluentValidation. Porția lipsă depinde de aliment, deci e verificată în handler și produce tot 400 ProblemDetails, prin aceeași excepție de validare.

### 3.4 Comenzi și interogări (MediatR)
- `AddDiaryEntryCommand`: încarcă alimentul cu `LoadVisibleAsync` (404 dacă nu e vizibil), rezolvă cantitatea, calculează snapshot-ul.
- `UpdateDiaryEntryCommand`: încarcă intrarea utilizatorului (404 altfel) și alimentul ei **chiar dacă este arhivat**, rezolvă cantitatea, recalculează.
- `DeleteDiaryEntryCommand`: încarcă intrarea utilizatorului (404 altfel), o șterge.
- `GetDiaryEntryQuery`, `GetDiaryDayQuery`, `GetRecentFoodsQuery`.
- Accesul la intrări este centralizat în `DiaryAccess.LoadOwnAsync`, după modelul `FoodAccess`.

---

## 4. Persistență

- Migrarea `DiaryServings`: `AddColumn Servings` (`double precision`, nullable). Rândurile existente rămân cu `Servings = null` și valorile rotunjite vechi, care rămân corecte la 0,05.
- Indexul existent `(UserId, ConsumedDate)` acoperă citirea zilei.
- „Recente”: intrările utilizatorului cu alimente nearhivate, grupate după `FoodItemId`, ordonate descrescător după ultimul `CreatedAtUtc`. Pentru fiecare aliment se întoarce cantitatea (și porțiile) din cea mai nouă intrare.

---

## 5. Contract API

Toate rutele sunt `[Authorize]`, cu id-ul utilizatorului din claim-ul `sub`.

| Metodă și rută | Succes | Erori |
|---|---|---|
| `GET /api/diary/days/{date}` (format `yyyy-MM-dd`) | 200 `DiaryDayDto` | 400, 401 |
| `GET /api/diary/entries/{id}` | 200 `DiaryEntryDto` | 401, 404 |
| `POST /api/diary/entries` | 201 `DiaryEntryDto` | 400, 401, 404 |
| `PUT /api/diary/entries/{id}` | 200 `DiaryEntryDto` | 400, 401, 404 |
| `DELETE /api/diary/entries/{id}` | 204 | 401, 404 |
| `GET /api/diary/recent-foods?limit=10` | 200 `RecentFoodDto[]` | 400, 401 |

```jsonc
// AddDiaryEntryRequest (POST); UpdateDiaryEntryRequest (PUT) are aceleași câmpuri fără foodId
{ "foodId": "…", "date": "2026-10-16", "mealType": "Breakfast", "quantityGrams": null, "servings": 1.5 }

// DiaryEntryDto
{ "id": "…", "date": "2026-10-16", "mealType": "Breakfast",
  "quantityGrams": 60, "servings": 1.5,
  "food": { "id": "…", "name": "Fulgi de ovăz", "brand": "Generic", "isArchived": false },
  "nutrients": { "calories": 233.4, "proteins": 10.1, "carbs": 39.8, "fats": 4.1 } }

// DiaryDayDto
{ "date": "2026-10-16",
  "meals": [ { "mealType": "Breakfast", "entries": [ /* DiaryEntryDto */ ], "totals": { /* nutrients */ } },
             { "mealType": "Lunch", … }, { "mealType": "Dinner", … }, { "mealType": "Snack", … } ],
  "totals":    { "calories": 1240.5, "proteins": 81.2, "carbs": 140.0, "fats": 38.9 },
  "targets":   { "calories": 1700,   "proteins": 124,  "carbs": 195,   "fats": 47 },
  "remaining": { "calories": 459.5,  "proteins": 42.8, "carbs": 55.0,  "fats": 8.1 },
  "isProfileComplete": true }

// RecentFoodDto
{ "food": { "id": "…", "name": "…", "brand": null, "isArchived": false },
  "lastQuantityGrams": 60, "lastServings": 1.5, "lastUsedAtUtc": "2026-10-16T07:30:00Z" }
```

Toate valorile nutriționale din DTO sunt rotunjite la 1 zecimală; totalurile se calculează înainte de rotunjire (D4). Intrările dintr-o masă sunt ordonate după `CreatedAtUtc`.

---

## 6. Testare

| Nivel | Ce acoperă |
|---|---|
| `Domain.UnitTests` | `DiaryQuantity.Resolve` (toate ramurile), `RecalculateNutrients` fără rotunjire |
| `Application.UnitTests` | handlerele (snapshot, aliment arhivat, ordinea meselor, zi goală, recente), validatoarele (§3.3) |
| `WebApi.IntegrationTests` | câte un test pe AC, pe PostgreSQL real (Testcontainers), inclusiv migrarea `DiaryServings` |

Exemplu de verificare numerică pentru AC1–AC3: fulgi de ovăz (389 kcal, 16,9 P, 66,3 C, 6,9 G per 100 g), porție 40 g, `servings = 1,5` → 60 g → 233,4 kcal, 10,1 P, 39,8 C, 4,1 G.
