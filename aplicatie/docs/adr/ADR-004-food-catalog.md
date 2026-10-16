# ADR-004: Catalogul de Alimente: Global + Personal, Arhivare și Căutare fără Diacritice

* **Status:** Acceptat (Accepted)
* **Data:** 2026-10-16
* **Autor:** Maximilian Andrei (Student)
* **Coordonator:** Dr. Ing. Mocanu Lucian-Mihai
* **Cerințe SRS:** UC01, FR-05, FR-06, FR-13 (parțial)
* **Spec:** [2026-10-16-food-catalog-design.md](../specs/2026-10-16-food-catalog-design.md)

---

## 1. Context și Enunțul Problemei
Jurnalul, rețetarul și fluxul OCR au nevoie de alimente cu valori nutriționale per 100 g. SRS-ul distinge un **catalog global** (administrat, verificat) de un **catalog personal** (alimentele scanate sau introduse de utilizator). Valorile vin de pe etichete UE, care declară **sare**, nu sodiu. Utilizatorii caută de pe telefon, de obicei fără diacritice.

---

## 2. Deciziile Luate

| # | Decizie | Alternative respinse și motivul |
|---|---|---|
| D1 | **Global + personal:** `CreatedByUserId == null` = aliment global, verificat, doar citire; altfel aliment privat al autorului | Comunitar (orice aliment public): necesită moderare și rol Admin. Doar personal: un utilizator nou pornește cu catalogul gol |
| D2 | **Arhivare** (`ArchivedAtUtc`) în loc de ștergere | Ștergere fizică: strică istoricul jurnalului sau e blocată de FK `Restrict`. Refuz cu 409: catalogul nu se mai poate curăța |
| D3 | **`SaltGrams`**, exact ce declară eticheta (Reg. UE 1169/2011); sodiu = sare / 2,5 | `Sodium` în mg: conversie la fiecare citire OCR și sursa unui bug de domeniu cunoscut |
| D4 | Coerența energetică (FR-06) este un **avertisment** calculat la citire | Respingere: fibrele, alcoolul și polialcoolii schimbă legitim totalul |
| D5 | **`SearchText` normalizat** în Domain (litere mici, fără diacritice, prin descompunere Unicode NFD) + `Contains` pe fiecare cuvânt | `unaccent` + `ILIKE`: netestabil în InMemory, neindexabil direct. Full-text `tsvector`: fără prefixe parțiale, complex pentru volum |
| D6 | **404** pentru alimentele altora și cele arhivate; **403** pentru cele globale | 404 peste tot: fals pentru un aliment pe care utilizatorul îl vede în căutare |

**Ordonarea căutării:**
1. potrivirile la începutul numelui;
2. alimentele verificate;
3. alfabetic;
4. după `Id`, pentru o paginare stabilă.

**Validarea fizică** (per 100 g):
- calorii ≤ 900, adică grăsime pură;
- P + C + G ≤ 100 g;
- zaharuri ≤ carbohidrați; saturate ≤ grăsimi.

Validatorul (`FoodFieldsValidator`) e comun pentru creare și editare și va fi refolosit pentru rezultatele OCR, unde aceste reguli prind cifre citite greșit.

### Calea spre catalogul comunitar
Varianta comunitară se poate adăuga peste D1 fără să se schimbe modelul existent:
- un câmp `Visibility` (`Private` / `Public`);
- rolul Admin (FR-13) pentru verificare și moderare;
- filtrul de vizibilitate extins cu `Visibility == Public`.

---

## 3. Consecințe

### Pozitive:
* **Izolarea datelor între utilizatori** (OWASP A01): alimentul altuia nu poate fi citit, modificat sau ghicit.
* **Istoricul jurnalului rămâne corect** după „ștergerea” unui aliment.
* **Valorile de sare corespund etichetei.** Migrarea a convertit seed-ul (65 mg sodiu → 0,1625 g sare), iar conversia a fost verificată pe o bază PostgreSQL populată.
* **Căutare tolerantă:** „piept pui fara piele” găsește „Piept de pui fără piele (crud)”. `%` și `_` sunt tratate literal, ceea ce a fost dovedit pe PostgreSQL.
* **Testare:** suita backend are 246 de teste.

### Riscuri asumate:
* **Copii duplicate:** doi utilizatori care scanează același produs au fiecare propria copie (până la catalogul comunitar).
* **`Contains` cu `%x%` nu folosește indexul B-tree.** La volum mare se adaugă un index trigram (`pg_trgm`), fără schimbări de cod.
* **Normalizarea din migrare** (`translate`) acoperă diacriticele românești. Alte diacritice din datele vechi se normalizează la prima editare a alimentului.
