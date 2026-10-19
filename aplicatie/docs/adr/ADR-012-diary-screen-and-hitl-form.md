# ADR-012: Ecranul Jurnalului și Formularul „Adaugă aliment” ca Ecran Human-in-the-Loop

* **Status:** Acceptat (Accepted)
* **Data:** 2026-10-17
* **Autor:** Maximilian Andrei (Student)
* **Coordonator:** Dr. Ing. Mocanu Lucian-Mihai
* **Cerințe SRS:** FR-03, FR-05, FR-07, FR-08 (partea zilnică), NFR-12
* **Jira:** NUT-30 (jurnal), NUT-47 (formularul „Adaugă aliment”)
* **Legat de:** [ADR-005](ADR-005-food-diary.md), [ADR-010](ADR-010-ocr-port-and-stub.md), [ADR-011](ADR-011-client-architecture.md)

---

## 1. Context
Primul ecran mutat de pe date mock pe API-ul real este jurnalul, cel mai folosit ecran al aplicației. Replanificarea din 16.10.2026 cere ca formularul manual „Adaugă aliment” să fie chiar ecranul *Human-in-the-Loop* din Faza 4. OCR-ul îl precompletează, deci regula FR-05 trebuie respectată de la început: câmpurile nesigure se confirmă explicit.

## 2. Deciziile Luate

| # | Decizie | Alternative respinse și motivul |
|---|---|---|
| D1 | **Ziua stă în URL** (`/diary?date=yyyy-MM-dd`); lipsa parametrului înseamnă azi, calculat în fusul orar al utilizatorului | Ziua în starea componentei: se pierde la reîncărcare și nu poate fi deschisă dintr-un link. `toISOString()`: dă data UTC, adică „ieri” după miezul nopții în România |
| D2 | **Un singur dialog cu pași**: căutare/recente → (aliment nou \| scanare → formular) → cantitate. Butonul global „Scanare OCR” deschide același dialog direct la scanare | Dialoguri separate pentru scanare și adăugare: aceeași logică de cantitate și salvare scrisă de două ori |
| D3 | **FR-05 în formular**: un câmp precompletat cu `needsReview` e evidențiat și blochează salvarea până e bifat „Am verificat valoarea” sau modificat. Butonul spune câte câmpuri mai trebuie confirmate | Doar evidențiere vizuală: utilizatorul poate salva fără să se uite (SRS cere confirmare explicită). Confirmare globală „Am verificat tot”: se bifează reflex, fără verificarea câmpului nesigur |
| D4 | **Valorile afișate înainte de salvare sunt doar o previzualizare**; valorile intrării sunt cele întoarse de server (snapshot, ADR-005) | Calcul în client trimis la server: două surse de adevăr pentru aceeași regulă |
| D5 | **Câmpurile numerice sunt text cu tastatură zecimală** (`inputMode="decimal"`), iar formularul acceptă atât „,” cât și „.” | `<input type="number">`: respinge „6,9”, forma pe care o folosesc utilizatorii români și etichetele din UE |
| D6 | **După orice modificare a jurnalului se invalidează toate zilele din cache, alimentele recente și statisticile**; un `DELETE` cu 404 e tratat ca reușit | Invalidarea doar a zilei curente: o intrare mutată pe altă zi ar lăsa ziua aceea învechită. 404 ca eroare: un dublu-tap ar afișa o eroare pentru o ștergere reușită |

## 3. Calea „OCR indisponibil” (NFR-12)
Cât timp `POST /api/scans` răspunde `status = "unavailable"` (stub-ul din ADR-010), dialogul trece la formularul gol și afișează mesajul primit de la server. Când în Faza 4 răspunsul devine `processed`, același formular primește `fields` și aplică D3, fără nicio modificare în client. Testul `while OCR is unavailable…` acoperă calea de azi; `FR-05: a prefilled field with low confidence…` acoperă exemplul SRS (proteine cu încrederea 0,42).

## 4. Consecințe
* **Pozitive:** jurnalul rulează pe date reale, cu editare, ștergere, navigare pe zile și scanare; regula FR-05 e impusă de interfață și testată înainte să existe OCR-ul.
* **Riscuri asumate:** ecranele Planificator, Rețetar și Cumpărături rulează încă pe mock. Acțiunile lor „trece în jurnal” scriu în starea mock, nu în jurnalul real, până la migrarea lor în ciclurile următoare.
