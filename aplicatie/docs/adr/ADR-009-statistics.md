# ADR-009: Statistici pe Interval, Medii pe Zilele Completate și Agregare în Baza de Date

* **Status:** Acceptat (Accepted)
* **Data:** 2026-10-17
* **Autor:** Maximilian Andrei (Student)
* **Coordonator:** Dr. Ing. Mocanu Lucian-Mihai
* **Cerințe SRS:** UC06, FR-08, NFR-03
* **Spec:** [2026-10-17-statistics-design.md](../specs/2026-10-17-statistics-design.md)

---

## 1. Context și Enunțul Problemei
FR-08 are două părți: totalurile zilnice față de țintă (livrate cu jurnalul, ADR-005) și **statistici agregate săptămânal și lunar (medii, abateri față de țintă) sub formă de grafice**. NFR-03 cere ca statistica lunară să răspundă în cel mult 500 ms. Arhitectura din SRS (§2) prevede ca interogările de raportare să folosească proiecții `AsNoTracking` direct în DTO-uri, separat de modelul de scriere: este partea de „Q” din CQRS.

Întrebările de proiectare:
1. Cum alege clientul perioada?
2. Ce înseamnă „media” când utilizatorul nu a completat jurnalul în fiecare zi?
3. Ce este „abaterea față de țintă”?
4. Unde se face agregarea?

---

## 2. Deciziile Luate

| # | Decizie | Alternative respinse și motivul |
|---|---|---|
| D1 | **Un endpoint pe interval**, `GET /api/stats?from&to`, cel mult 366 de zile; săptămâna sau luna le alege clientul | `?period=week\|month`: serverul ar trebui să știe ziua de început a săptămânii și fusul orar al utilizatorului (ca la plan, ADR-007 D3) |
| D2 | **Media se face doar pe zilele cu cel puțin o intrare**; se raportează `loggedDays` și `totalDays` | Media pe toate zilele: o zi necompletată ar însemna „0 kcal” și ar arăta un deficit fals |
| D3 | **Abaterea = media − ținta** (cu semn), pe calorii și macronutrienți, plus procentul caloric | Abaterea medie absolută: ascunde direcția (deficit sau surplus). Abaterea standard: măsoară variabilitatea, nu respectarea țintei |
| D4 | **Zilele se clasifică după regula planului**: ±5% din ținta calorică, numărate în `statusCounts` | O regulă separată: planul (intenția) și jurnalul (faptul) ar fi judecate diferit |
| D5 | **Ținta curentă**, ca în jurnal și plan | Istoric de ținte: tabel nou și versionare, fără cerință explicită |
| D6 | **Agregare în PostgreSQL**: `GROUP BY ConsumedDate` cu `SUM`, `AsNoTracking`, pe indexul `(UserId, ConsumedDate)` | Încărcarea intrărilor în memorie: pentru un an, mii de rânduri în loc de cel mult 366 |
| D7 | **Toate zilele intervalului apar în răspuns**; cele fără intrări au `isLogged = false` și `status = null` | Doar zilele completate: clientul ar completa golurile pe axa graficului |
| D8 | **Statisticile sunt o funcție pură în Domain** (`NutritionStatistics.Summarize`) | Calculul în handler: se testează doar prin EF și amestecă interogarea cu regula de business |

**De ce contează D2 (exemplu numeric).** Ținta este 1700 kcal. În 7 zile, utilizatorul notează doar 3 zile: 1500, 1700 și 1900 kcal. Media pe zilele completate este 1700 kcal, adică exact ținta. Media pe toate cele 7 zile ar fi 5100 / 7 ≈ 729 kcal, un deficit de 57% care nu a existat. Răspunsul include `loggedDays = 3` din `totalDays = 7`, deci clientul poate afișa și cât de complet este jurnalul.

**Formulele (D3, D4):**
```
average   = Σ total_zi / loggedDays                          (doar zilele completate)
deviation = average − target                                 (pe valorile afișate, rotunjite la 0,1)
caloriesDeviationPercent = (average.kcal − target.kcal) / target.kcal × 100
status_zi = under  dacă kcal < 0,95 × țintă
            over   dacă kcal > 1,05 × țintă
            within altfel
```

**Interogarea (D6).** EF Core traduce proiecția în:
```sql
SELECT d."ConsumedDate", SUM(d."CalculatedCalories"), SUM(d."CalculatedProteins"),
       SUM(d."CalculatedCarbs"), SUM(d."CalculatedFats")
FROM "DiaryEntries" AS d
WHERE d."UserId" = @userId AND d."ConsumedDate" >= @from AND d."ConsumedDate" <= @to
GROUP BY d."ConsumedDate"
```
Sumele folosesc snapshot-urile nerotunjite ale intrărilor (ADR-005 D1), deci intrările-rețetă și intrările-aliment contează la fel, iar editarea ulterioară a unui aliment nu rescrie statisticile trecute.

---

## 3. Consecințe

### Pozitive
* **Statisticile sunt oneste:** zilele necompletate nu falsifică media, iar `loggedDays` arată cât de sigură este media.
* **Aceeași regulă de țintă în tot sistemul:** jurnalul, planul și statisticile folosesc aceeași țintă curentă și același prag de ±5%.
* **Cost mic și constant:** cel mult 366 de rânduri pe cerere, indiferent câte intrări are o zi; testul de integrare verifică traducerea `GROUP BY` pe PostgreSQL real.
* **Testabil:** regula de calcul este testată în Domain fără bază de date (5 teste), handler-ul în Application (9 teste), contractul pe PostgreSQL (6 teste).

### Riscuri asumate
* **Ținta curentă se aplică retroactiv** (D5): după o schimbare de obiectiv, zilele vechi sunt comparate cu noua țintă. Istoricul țintelor rămâne dezvoltare viitoare.
* **Zilele se grupează după data calendaristică trimisă de client** (`ConsumedDate`), nu după fusul orar; este aceeași convenție ca în jurnal.
* **Gruparea pe săptămâni sau luni într-un an de date** se face în client, din seria zilnică.
