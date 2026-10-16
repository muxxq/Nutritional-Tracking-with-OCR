# ADR-007: Planificatorul Săptămânal: Plan Calculat la Citire și „Am mâncat asta” ca Trecere de la Intenție la Fapt

* **Status:** Acceptat (Accepted)
* **Data:** 2026-10-16
* **Autor:** Maximilian Andrei (Student)
* **Coordonator:** Dr. Ing. Mocanu Lucian-Mihai
* **Cerințe SRS:** UC04, FR-10, FR-11
* **Spec:** [2026-10-16-meal-planner-design.md](../specs/2026-10-16-meal-planner-design.md)

---

## 1. Context și Enunțul Problemei
FR-10 cere asignarea rețetelor sau alimentelor pe zilele și mesele unei săptămâni, iar pentru fiecare zi compararea totalului planificat cu ținta calorică, în trei stări: sub, în interval (±5%) și peste. FR-11 cere ca o masă planificată să ajungă în jurnal „cu o singură acțiune”. FR-12 (lista de cumpărături, ciclul următor) va scala ingredientele „la numărul de porții planificate”.

Entitatea `PlannedMeal` exista din Faza 1, dar nu putea susține aceste cerințe:
- `QuantityGrams` era obligatoriu și nu avea porții, deci o rețetă nu putea fi planificată corect;
- booleanul `IsTransferredToDiary` nu știa dacă intrarea din jurnal mai există;
- cheile spre aliment și rețetă aveau `ON DELETE SET NULL`, care putea lăsa o masă fără aliment și fără rețetă.

Întrebările de proiectare:
1. Ce formă are o masă planificată?
2. Se stochează valorile planului sau se calculează?
3. Ce înseamnă „Am mâncat asta” și cum se evită dublarea?
4. Ce se întâmplă când un aliment sau o rețetă planificate sunt arhivate?

---

## 2. Deciziile Luate

| # | Decizie | Alternative respinse și motivul |
|---|---|---|
| D1 | **Masă planificată polimorfă, ca intrarea de jurnal**: exact una dintre cheile aliment / rețetă; alimentul în grame sau porții, rețeta doar în porții; aceleași `CHECK`-uri | Doar grame (entitatea inițială): o rețetă în grame crude este greșită nutrițional (ADR-006 D3), iar FR-12 cere porții |
| D2 | **Valorile se calculează la citire** din alimentele și rețetele curente | Snapshot la planificare: o corectare a alimentului n-ar ajunge în plan, deși masa n-a fost încă mâncată |
| D3 | **Un endpoint pe interval** (`GET /api/plan?from&to`, cel mult 31 de zile), în forma ecranului | Săptămână fixă luni–duminică: impune clientului începutul săptămânii și nu servește intervalul din FR-12. Endpoint-uri separate pentru mese și sumar: inconsistente (ADR-005 D3) |
| D4 | **Starea zilei pe calorii, ±5% din ținta curentă** | Stare pe fiecare macronutrient: FR-10 cere doar comparația calorică |
| D5 | **„Am mâncat asta” creează o intrare de jurnal** la data și masa planificate, cu aceeași cantitate și snapshot din valorile curente | Mutarea mesei din plan: utilizatorul pierde planul și nu mai vede ce a respectat. Data de azi: o masă planificată ieri și consemnată azi ar ajunge în ziua greșită |
| D6 | **Referință `DiaryEntryId`** cu `ON DELETE SET NULL` și index unic; al doilea transfer primește 409 | Boolean `IsTransferredToDiary`: rămâne `true` după ștergerea intrării. Fără verificare: două intrări la dublu-tap |
| D7 | **Masa al cărei aliment sau rețetă a fost arhivat rămâne și se poate transfera**; planificarea nouă cere elemente vizibile | Blocarea transferului: arhivarea alimentului nu schimbă faptul că masa a fost mâncată |
| D8 | **Tipul nu se schimbă la editare; ștergerea este fizică** și nu atinge intrarea din jurnal | Arhivarea meselor planificate: nimic nu le referă |

**Plan, rețetă și jurnal: trei roluri distincte.** Rețeta este un șablon (ADR-006 D1), planul este o intenție, iar jurnalul este un fapt. Primele două se calculează la citire și urmează catalogul. Jurnalul păstrează snapshot-ul (ADR-005 D1). Momentul „Am mâncat asta” este exact trecerea de la intenție la fapt: valorile se îngheață atunci, nu la planificare.

**Starea zilei (D4):**
```
total < 0,95 × țintă → under      total > 1,05 × țintă → over      altfel → within
```
Pentru ținta de 1700 kcal, intervalul `within` este 1615–1785 kcal. Starea se calculează din totalul nerotunjit.

**Transferul (D5, D6).** Intrarea nouă și referința `DiaryEntryId` se scriu în același `SaveChanges`, deci în aceeași tranzacție. Data trebuie să respecte regula jurnalului (cel mult ziua de mâine), altfel 400: o masă planificată săptămâna viitoare nu poate fi deja mâncată. Exemplu: rețeta SRS (194,5 kcal/porție) planificată pentru 2 porții are 389 kcal; transferată, devine o intrare de jurnal cu 389 kcal și `servings = 2`.

**Integritatea în baza de date (D1, D6).**
- Două constrângeri `CHECK`, ca la jurnal: `CK_PlannedMeals_FoodXorRecipe` și `CK_PlannedMeals_QuantityByKind`.
- Cheile spre aliment și rețetă trec de la `SET NULL` la `RESTRICT`: `SET NULL` ar fi încălcat regula „exact o cheie”, iar alimentele și rețetele se arhivează, nu se șterg.
- Cheia spre jurnal are `SET NULL`, așa că o intrare ștearsă eliberează masa pentru un nou transfer.

Testul de integrare `CheckConstraints_RejectDirectSqlInserts` verifică toate cele patru combinații respinse (cod PostgreSQL `23514`), iar `Eat_AfterDiaryEntryDeleted_Returns201` verifică `SET NULL` pe PostgreSQL real.

---

## 3. Consecințe

### Pozitive:
* **Un singur model mental:** masa planificată și intrarea de jurnal au aceeași formă și aceleași reguli de cantitate (`DiaryQuantity`), deci transferul este o copiere plus un snapshot.
* **FR-12 este pregătit:** rețetele planificate au porții, deci ingredientele se pot scala direct (`grame × porții planificate / porțiile rețetei`).
* **Un request pentru ecranul săptămânii,** cu toate zilele intervalului (și cele goale) și cu starea calculată pe server.
* **Fără dublări secvențiale:** al doilea „Am mâncat asta” primește 409, iar ștergerea intrării din jurnal permite un nou transfer.
* **Testare:** suita backend are 466 de teste (79 Domain, 13 Infrastructure, 255 Application, 119 de integrare).

### Riscuri asumate:
* **Două cereri `eat` simultane** pe aceeași masă pot crea două intrări (nu există token de concurență), iar masa o referă doar pe ultima. Utilizatorul o poate șterge pe cea în plus din jurnal. Clientul dezactivează butonul după prima apăsare.
* **Toate zilele folosesc ținta curentă** (moștenit din ADR-005 D5).
* **Calculul la citire încarcă alimentele și rețetele intervalului** (cel mult 31 de zile); pentru o lună de plan înseamnă câteva zeci de mese, acceptabil fără agregare SQL.
