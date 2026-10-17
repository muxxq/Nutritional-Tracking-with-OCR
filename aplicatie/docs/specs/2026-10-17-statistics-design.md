# Spec: Statistici agregate pe interval (backend)

**Data:** 2026-10-17
**Branch:** `feature/NUT-statistics`
**Cerințe SRS:** UC06, FR-08 (totaluri zilnice și statistici agregate), NFR-03 (statistica lunară ≤ 500 ms)
**Faza din roadmap:** Faza 2 (Backend core)
**Depinde de:** profil și ținte (ADR-003), jurnalul alimentar (ADR-005), starea zilei din planificator (ADR-007 D4)

---

## 1. Scop

Totalurile zilnice față de țintă există deja (`GET /api/diary/days/{date}`, ADR-005). Lipsește partea a doua din FR-08: **statistici agregate săptămânal și lunar (medii, abateri față de țintă)**, din care clientul desenează grafice.

### User story
> Ca utilizator NutriScan, vreau să văd cum am mâncat într-o săptămână sau într-o lună (media zilnică, cât m-am abătut de la țintă și în câte zile am fost în țintă), ca să înțeleg dacă îmi respect obiectivul.

### Criterii de acceptanță
- **AC1:** `GET /api/stats?from=&to=` întoarce 200 cu **fiecare zi din interval**, în ordine, cu totalurile ei și `isLogged` (zilele fără intrări au totaluri 0 și `isLogged = false`).
- **AC2:** media zilnică se calculează **doar pe zilele cu cel puțin o intrare**. Exemplu: în 7 zile, 3 zile completate cu 1500, 1700 și 1900 kcal → media 1700 kcal, `loggedDays = 3`, `totalDays = 7`.
- **AC3:** abaterea = media − ținta, pentru calorii și cele trei macronutriente, plus abaterea calorică în procente. Exemplu: ținta 1700 kcal, media 1785 kcal → `+85 kcal`, `+5%`.
- **AC4:** fiecare zi completată are starea `under` / `within` / `over` după aceeași regulă ca planificatorul (±5% din ținta calorică), iar răspunsul numără zilele din fiecare stare.
- **AC5:** fără nicio zi completată, `average`, `deviation` și `caloriesDeviationPercent` sunt `null`, iar contoarele sunt 0 (nu 400: un interval gol este un răspuns valid).
- **AC6:** intrările-rețetă și intrările-aliment contează la fel (snapshot-ul nutrițional al intrării, ADR-005 D1).
- **AC7:** intervalul are cel mult 366 de zile, `from ≤ to`, date între 01.01.2000 și un an de acum înainte; altfel 400.
- **AC8:** fiecare utilizator vede doar statisticile lui; ruta cere access token (401).

### În afara scopului
Istoricul țintelor (statisticile folosesc ținta curentă, ca jurnalul: ADR-005 D5), evoluția greutății, statistici pe micronutrienți, gruparea pe săptămâni/luni făcută de server, exportul datelor, graficele din client.

---

## 2. Decizii (rezumat pentru ADR-009)

| # | Întrebare | Decizie | Alternative respinse |
|---|---|---|---|
| D1 | Endpoint pe perioadă (`?period=week\|month`) sau pe interval? | **Un endpoint pe interval** (`from`, `to`, cel mult 366 de zile); clientul alege săptămâna sau luna | `period=week`: serverul ar trebui să știe ziua de început a săptămânii și fusul orar al utilizatorului (aceeași problemă ca la plan, ADR-007 D3). Endpoint-uri separate pentru săptămână și lună: aceeași logică de două ori |
| D2 | Media se face pe toate zilele sau doar pe cele completate? | **Doar pe zilele cu cel puțin o intrare.** Se raportează și `loggedDays` / `totalDays` | Pe toate zilele: o zi necompletată înseamnă „nu am notat”, nu „am mâncat 0 kcal”; media ar fi artificial mică și ar arăta un deficit fals |
| D3 | Ce este „abaterea față de țintă”? | **Media − ținta** (semn inclus) pe toate cele patru valori, plus **procentul caloric** `(medie − țintă) / țintă × 100` | Abaterea medie absolută `mean(|zi − țintă|)`: mai greu de explicat utilizatorului și ascunde direcția (deficit vs surplus). Abaterea standard: măsoară variabilitatea, nu respectarea țintei |
| D4 | Cum se clasifică zilele? | **Aceeași regulă ca planul: ±5% din ținta calorică** (`PlanDayStatusCalculator`), numărate în `statusCounts` | O regulă nouă pentru jurnal: planul (intenția) și jurnalul (faptul) ar fi judecate diferit pentru aceeași zi |
| D5 | Ce țintă se folosește? | **Ținta curentă**, ca în jurnal și plan | Istoric de ținte: necesită un tabel nou și versionare; documentat ca limitare |
| D6 | Unde se agregă? | **În PostgreSQL**: `GROUP BY ConsumedDate` cu `SUM`, proiecție `AsNoTracking` direct în rânduri mici (o linie pe zi), pe indexul existent `(UserId, ConsumedDate)` | Încărcarea tuturor intrărilor în memorie: pentru un an, mii de rânduri cu alimente incluse, în loc de cel mult 366. SRS §2 cere explicit proiecții `AsNoTracking` pentru raportare |
| D7 | Zilele fără intrări apar în răspuns? | **Da, toate zilele intervalului**, cu `isLogged = false` și `status = null` | Doar zilele completate: clientul ar completa singur golurile pentru axa graficului |
| D8 | Calculul statisticilor | **Funcție pură în Domain** (`NutritionStatistics.Summarize`), testată fără bază de date | Calcul în handler: amestecă interogarea cu regula de business și se testează doar prin EF |

---

## 3. Domain

`Domain/Nutrition/NutritionStatistics.cs`:
```
Summarize(loggedDayTotals: Macros[], target: Macros) →
    LoggedDays     = număr zile
    Average        = sumă / LoggedDays            (null dacă 0 zile)
    Deviation      = Average − target            (null dacă 0 zile)
    CaloriesDeviationPercent = Deviation.Calories / target.Calories × 100   (null dacă 0 zile sau țintă 0)
    StatusCounts   = numărul zilelor under / within / over (PlanDayStatusCalculator)
```

---

## 4. Contract API

| Metodă și rută | Succes | Erori |
|---|---|---|
| `GET /api/stats?from=yyyy-MM-dd&to=yyyy-MM-dd` | 200 `StatisticsDto` | 400, 401 |

```jsonc
{
  "from": "2026-10-12", "to": "2026-10-18",
  "totalDays": 7, "loggedDays": 3,
  "targets":   { "calories": 1700, "proteins": 124, "carbs": 195, "fats": 47 },
  "average":   { "calories": 1700, "proteins": 110.5, "carbs": 200, "fats": 50 },   // null fără zile completate
  "deviation": { "calories": 0, "proteins": -13.5, "carbs": 5, "fats": 3 },         // medie − țintă
  "caloriesDeviationPercent": 0,
  "statusCounts": { "under": 1, "within": 1, "over": 1 },
  "days": [
    { "date": "2026-10-12", "isLogged": true,  "totals": { … }, "status": "under" },
    { "date": "2026-10-13", "isLogged": false, "totals": { "calories": 0, … }, "status": null }
  ],
  "isProfileComplete": true
}
```
Valorile sunt rotunjite la o zecimală. Abaterea se calculează din valorile afișate (media rotunjită − ținta rotunjită), ca `remaining` din jurnal, deci media − abaterea = ținta afișată.

---

## 5. Testare

| Nivel | Ce acoperă |
|---|---|
| `Domain.UnitTests` | `NutritionStatistics`: media pe zile completate (AC2), abaterea și procentul (AC3), contoarele de stare (AC4), fără zile (AC5) |
| `Application.UnitTests` | handler: zile completate cu zero (AC1), intrări-rețetă (AC6), izolarea pe utilizator (AC8); validatorul (AC7) |
| `WebApi.IntegrationTests` | pe PostgreSQL real: traducerea `GROUP BY` / `SUM`, AC2–AC4 end-to-end, 400, 401, izolare |

---

## 6. Documentație livrată
- ADR-009 (D1–D8); `architecture_and_contracts.md`: secțiunea Statistici; `ROADMAP.md`: FR-08 bifat.
