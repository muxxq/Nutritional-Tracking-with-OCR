# ADR-006: Rețetarul: Valori Calculate la Citire și Rețete în Jurnal prin Intrări Polimorfe

* **Status:** Acceptat (Accepted)
* **Data:** 2026-10-16
* **Autor:** Maximilian Andrei (Student)
* **Coordonator:** Dr. Ing. Mocanu Lucian-Mihai
* **Cerințe SRS:** FR-09; FR-07 (partea „aliment sau rețetă”)
* **Spec:** [2026-10-16-recipes-design.md](../specs/2026-10-16-recipes-design.md)

---

## 1. Context și Enunțul Problemei
Utilizatorul gătește aceleași mâncăruri des. Fără rețetar, ar introduce în jurnal fiecare ingredient, la fiecare masă. FR-09 cere ca o rețetă, compusă din alimente din catalog, să-și calculeze singură valorile totale și per porție. Criteriul de acceptanță din SRS: 200 g fulgi de ovăz (389 kcal/100 g) în 4 porții dau **194,5 kcal per porție**. FR-07 cere ca jurnalul să primească „un aliment sau o rețetă”.

Întrebările de proiectare:
1. Se stochează valorile rețetei sau se calculează?
2. Cum intră o rețetă în jurnal, care până acum referea doar alimente?
3. În ce unitate se adaugă o rețetă în jurnal?
4. Ce înseamnă ștergerea unei rețete deja mâncate?

---

## 2. Deciziile Luate

| # | Decizie | Alternative respinse și motivul |
|---|---|---|
| D1 | **Valorile se calculează la citire**, din valorile curente ale alimentelor, cu o funcție pură din Domain (`RecipeNutrition`). Pe `Recipe` nu se stochează nimic | Totaluri stocate la salvare: devin inconsistente când se corectează un aliment. Totaluri stocate plus recalcul prin evenimente de domeniu: propagare și cod în plus fără o cerință care să le justifice |
| D2 | **Intrare de jurnal polimorfă:** `DiaryEntry` are exact una dintre cheile `FoodItemId` / `RecipeId`, garantată de un `CHECK` în PostgreSQL | Explodarea rețetei în intrări per ingredient: se pierde gruparea („Ciorbă”, nu 9 rânduri). Un `FoodItem` ascuns generat din rețetă: alimente „fantomă” în catalog, sincronizate la fiecare editare |
| D3 | **Rețetele se adaugă doar în porții**; `QuantityGrams` rămâne NULL la intrările-rețetă | Grame raportate la greutatea crudă a ingredientelor: greșit pentru mâncăruri gătite (orezul fiert cântărește de aproximativ 3 ori mai mult decât crud). Grame raportate la o greutate gătită introdusă de utilizator: rămâne dezvoltare viitoare |
| D4 | **`DELETE` arhivează**, ca la alimente (ADR-004 D2) | Ștergere fizică: intrările din jurnal ar rămâne fără aliment și fără rețetă. Interzicerea ștergerii rețetelor folosite (409): utilizatorul nu mai poate scăpa de o rețetă mâncată o dată |
| D5 | **Ingredientele se editează prin agregat:** `PUT` pe toată rețeta înlocuiește lista | Endpoint-uri separate per ingredient: mai multe request-uri pentru un singur formular și stări intermediare incomplete |
| D6 | **Un aliment apare o singură dată într-o rețetă** (400 și index unic `(RecipeId, FoodItemId)`) | Duplicate permise: listă confuză în UI; lista de cumpărături ar trebui oricum să le unească |
| D7 | **Ultima scriere câștigă** la editări simultane | Token de concurență optimistă și 409: justificat la refresh token-uri (ADR-002), nu la o rețetă editată rar, de un singur utilizator |

**De ce D1 nu contrazice snapshot-ul din jurnal (ADR-005 D1).** Rețeta este un *șablon*: când se corectează un aliment, rețeta trebuie să arate valorile corecte. Intrarea din jurnal este un *fapt istoric*: arată ce s-a mâncat atunci. La adăugarea în jurnal se face snapshot din valorile rețetei din acel moment (`perServing × servings`, nerotunjit), iar o editare ulterioară a alimentului schimbă rețeta, dar nu și intrarea (test de integrare `EditFood_ChangesRecipeButNotLoggedEntry`).

**Formula (D1):**
```
total      = Σ ingredient: aliment.per100g × grame / 100     (kcal, proteine, carbohidrați, grăsimi)
perServing = total / ServingsCount
```
Exemplul SRS: 200 × 389 / 100 = 778 kcal; 778 / 4 = 194,5 kcal per porție. O intrare de 1,5 porții stochează 291,75 kcal și afișează 291,8. Rotunjirea la o zecimală se face doar în răspuns, iar `perServing` se calculează din totalul nerotunjit (ADR-005 D4).

**Integritatea în baza de date (D2, D3).** Două constrângeri `CHECK` pe `DiaryEntries` resping o intrare incoerentă chiar dacă scrierea ocolește API-ul:
```sql
CK_DiaryEntries_FoodXorRecipe:  ("FoodItemId" IS NULL) <> ("RecipeId" IS NULL)
CK_DiaryEntries_QuantityByKind: ("FoodItemId" IS NOT NULL AND "QuantityGrams" IS NOT NULL)
                             OR ("RecipeId" IS NOT NULL AND "QuantityGrams" IS NULL AND "Servings" IS NOT NULL)
```
Testul `CheckConstraints_RejectDirectSqlInserts` inserează direct prin SQL și verifică codul PostgreSQL `23514` (`check_violation`).

**Validare (FluentValidation, 400):** nume 1–200 caractere după trim; descriere ≤ 1000; instrucțiuni ≤ 10 000; `servingsCount` 1–50; `prepTimeMinutes` 0–1440; 1–50 ingrediente, cu alimente distincte și `0 < quantityGrams ≤ 5000`. În jurnal: exact unul dintre `foodId` / `recipeId`; la rețetă `0 < servings ≤ 50`, iar `quantityGrams` e interzis.

**Proprietate și vizibilitate (404, NFR-10):** rețetele sunt private. Rețeta altui utilizator și rețeta arhivată primesc 404 la citire, editare, ștergere și adăugare în jurnal. Un ingredient trebuie să fie un aliment vizibil (global sau propriu, nearhivat). Excepție: un aliment propriu arhivat *după* ce a intrat în rețetă poate rămâne în ea la `PUT`, altfel utilizatorul n-ar mai putea edita rețeta. Un *alt* aliment arhivat nu poate fi adăugat.

---

## 3. Consecințe

### Pozitive:
* **Rețete mereu corecte:** corectarea unui aliment se vede imediat în toate rețetele care îl conțin, fără cod de propagare.
* **Istoric stabil:** intrările din jurnal păstrează snapshot-ul, la fel ca intrările-aliment.
* **Lista de rețete nu încarcă ingredientele:** valorile per porție se calculează în SQL, prin proiecție (`SUM(kcal × grame / 100)`), apoi se împart la porții și se rotunjesc în memorie.
* **Editarea ingredientelor păstrează rândurile existente:** lista se modifică pe loc, după aliment (cantitate schimbată, ingrediente eliminate, ingrediente noi), deci reordonarea nu lovește indexul unic (testat pe PostgreSQL real, `Update_ChangesQuantityOfExistingIngredient_Returns200`).
* **Client TypeScript sigur:** `DiaryEntryDto` primește câmpul `kind` (`"food"` / `"recipe"`), care permite o uniune discriminată.
* **Ordinea ingredientelor se păstrează:** `RecipeIngredient.Position` reține locul din listă la fiecare salvare. Id-urile sunt GUID-uri aleatoare, deci fără poziție ordinea la citire ar fi întâmplătoare. Problema a fost găsită la code review-ul final (`Get_KeepsIngredientOrderOfLastSave`).
* **Testare:** suita backend are 404 teste (72 Domain, 13 Infrastructure, 216 Application, 103 de integrare).

### Riscuri asumate:
* **Ultima scriere câștigă** (D7): două tab-uri care editează aceeași rețetă se suprascriu.
* **Zilele trecute folosesc țintele curente** (moștenit din ADR-005 D5).
* **Rețetele nu se pot adăuga în grame** (D3): pentru o porție inegală, utilizatorul introduce porții fracționare (de exemplu 1,5).
* **Contract incompatibil în jurnal:** `food` și `quantityGrams` din `DiaryEntryDto` devin nullable. Schimbarea nu afectează pe nimeni acum, pentru că clientul folosește încă date mock (Faza 3).
