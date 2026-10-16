# ADR-005: Jurnalul Alimentar: Snapshot Nutrițional, Grame sau Porții și Ziua ca Model de Citire

* **Status:** Acceptat (Accepted)
* **Data:** 2026-10-16
* **Autor:** Maximilian Andrei (Student)
* **Coordonator:** Dr. Ing. Mocanu Lucian-Mihai
* **Cerințe SRS:** UC02, FR-07; FR-08 (parțial: totalurile zilnice)
* **Spec:** [2026-10-16-food-diary-design.md](../specs/2026-10-16-food-diary-design.md)

---

## 1. Context și Enunțul Problemei
Jurnalul este ecranul principal al aplicației: utilizatorul notează ce a mâncat la fiecare dintre cele 4 mese și vede imediat cât a consumat față de ținte (inelul caloric și barele de macronutrienți). Trei întrebări de proiectare au decis forma modulului:
1. Ce se întâmplă cu istoricul când valorile unui aliment se corectează ulterior?
2. Cum introduce utilizatorul cantitatea: în grame (ce scrie pe cântar) sau în porții (ce scrie pe ambalaj)?
3. Cum ajunge clientul la totalurile zilei fără ca serverul și clientul să calculeze diferit?

---

## 2. Deciziile Luate

| # | Decizie | Alternative respinse și motivul |
|---|---|---|
| D1 | **Snapshot la salvare:** intrarea păstrează valorile calculate în momentul adăugării; se recalculează din alimentul curent doar când se editează intrarea | Calcul la citire din aliment: corectarea unui aliment ar rescrie retroactiv istoricul. Propagare opțională: cod în plus fără o cerință |
| D2 | **Grame sau porții** la intrare; se stochează gramele (sursa de adevăr) și, opțional, numărul de porții | Doar grame: clientul n-ar mai putea reafișa „1,5 porții (60 g)” la editare |
| D3 | **Un endpoint agregat pe zi** (`GET /api/diary/days/{date}`), în forma ecranului | Endpoint-uri separate pentru intrări și sumar: două request-uri care pot da rezultate inconsistente. Agregare SQL: prematură pentru o zi; rămâne pentru statisticile lunare |
| D4 | **Valori nerotunjite în baza de date**, rotunjire la 1 zecimală doar în răspuns | Rotunjire pe intrare: eroarea se acumulează în total (3 × 0,04 g ar da 0 în loc de 0,1) |
| D5 | Zilele trecute se compară cu **țintele curente** | Istoric al țintelor: necesar abia pentru statistici (FR-08) |
| D6 | **Ștergere fizică** a intrărilor | Arhivare: nimic nu referă o intrare de jurnal (spre deosebire de alimente, ADR-004 D2) |
| D7 | **Alimentul unei intrări nu se schimbă la editare** | `foodId` în `PUT`: două operații într-una și o regulă de snapshot ambiguă |

**Analogie pentru D1:** un bon fiscal nu se schimbă când crește prețul produsului. Jurnalul este un document istoric: arată ce știa utilizatorul atunci. Denormalizarea (valorile calculate stocate pe intrare) este deliberată și are și un efect de performanță: totalul zilei nu mai face join cu alimentele.

**Validare:**
- exact una dintre cantități; `0 < grame ≤ 5000`; `0 < porții ≤ 50`; după conversie `≤ 5000 g`;
- data între 01.01.2000 și ziua de mâine (UTC). Ziua în plus acoperă fusurile orare: serverul nu știe ora locală a clientului;
- porțiile cer un aliment cu porție definită (`ServingSizeGrams`). Regula depinde de baza de date, așa că e verificată în handler, tot cu 400.

**Proprietate și vizibilitate:** intrarea altui utilizator primește 404 (NFR-10). Se pot adăuga doar alimente vizibile în catalog (globale sau proprii, nearhivate). O intrare al cărei aliment a fost arhivat rămâne în zi și poate fi editată.

---

## 3. Consecințe

### Pozitive:
* **Istoric stabil:** corectarea unui aliment nu modifică zilele trecute (test de integrare `EditFood_DoesNotChangeExistingEntry`).
* **Un singur request pentru dashboard**, cu toate cele 4 mese mereu prezente, în ordine fixă; clientul nu mai calculează totaluri.
* **Totaluri exacte:** suma se face din valorile nerotunjite (`GetDay_SumsBeforeRounding`).
* **Timp controlabil în teste:** marcajele `CreatedAtUtc`/`UpdatedAtUtc` vin acum din `IDateTimeProvider` și în `ApplicationDbContext`. Până acum erau suprascrise cu ceasul sistemului, iar problema a ieșit la iveală prin testele TDD ale jurnalului.
* **Testare:** suita backend are 326 de teste.

### Riscuri asumate:
* **Țintele zilelor trecute sunt cele de azi** (D5). Statisticile săptămânale și lunare (FR-08) vor avea nevoie de un istoric al țintelor sau de un snapshot zilnic.
* **Rețetele în jurnal** (FR-07 menționează „aliment sau rețetă”) vin odată cu modulul de rețete. Intrarea va primi atunci o referință opțională la rețetă, cu același mecanism de snapshot.
* **„Recente” scanează ultimele 200 de intrări:** un aliment folosit ultima dată acum mult timp poate lipsi din listă. Este acceptabil pentru scopul listei (acces rapid la alimentele uzuale).
