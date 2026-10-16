# ADR-008: Lista de Cumpărături: Snapshot Salvat al Planului, Agregare după Aliment și Categorii pe Aliment

* **Status:** Acceptat (Accepted)
* **Data:** 2026-10-16
* **Autor:** Maximilian Andrei (Student)
* **Coordonator:** Dr. Ing. Mocanu Lucian-Mihai
* **Cerințe SRS:** UC05, FR-12
* **Spec:** [2026-10-16-shopping-list-design.md](../specs/2026-10-16-shopping-list-design.md)

---

## 1. Context și Enunțul Problemei
FR-12 cere generarea listei de cumpărături pentru un interval de zile din plan. Ingredientele identice se agregă, adunând cantitățile scalate la porțiile planificate, apoi se grupează pe categorii. Articolele trebuie să poată fi bifate, iar utilizatorul trebuie să poată adăuga articole manuale. Criteriul de acceptanță din SRS: două rețete cu 150 g și 250 g de piept de pui dau un singur articol, „Piept de pui — 400 g”.

Planificatorul (ADR-007) a pregătit datele: rețetele se planifică în porții. Rămâneau trei întrebări:
1. Lista se calculează la citire, ca planul, sau se salvează?
2. Ce face o nouă generare cu bifele și cu articolele manuale?
3. De unde vin categoriile? Alimentele nu aveau niciuna.

---

## 2. Deciziile Luate

| # | Decizie | Alternative respinse și motivul |
|---|---|---|
| D1 | **Lista se salvează** (o listă curentă per utilizator) | Calcul la citire, ca rețetele și planul: bifele n-ar avea unde să stea și s-ar pierde la orice editare a planului |
| D2 | **Generarea înlocuiește articolele generate și păstrează articolele manuale** | Adăugare la listă: ingrediente duble. Ștergere totală: se pierd articolele scrise de mână. Păstrarea bifelor: o bifă pusă pe 200 g ar ascunde o cantitate nouă de 400 g |
| D3 | **Agregare după aliment, în grame**; rețeta contribuie cu `grame × porții planificate / porțiile rețetei` | Agregare după nume: ar uni alimente diferite cu același nume și ar separa același aliment cu nume diferite |
| D4 | **Intră doar mesele nemâncate din interval** (cel mult 31 de zile) | Toate mesele: ce s-a mâncat a fost deja cumpărat. Doar mesele viitoare: masa de azi încă nemâncată trebuie cumpărată |
| D5 | **Categorie pe aliment** (`FoodCategory`, 7 raioane, implicit `other`) | Categorii deduse din nume: fragil. Text liber: „Lactate” ≠ „lactate”. Tabel administrat de categorii: nicio cerință nu îl justifică |
| D6 | **Gramele se rotunjesc în sus la gram întreg**, doar în răspuns | Rotunjire la zecimală: „33,3 g” nu are sens la raft; la cumpărături contează să nu lipsească |
| D7 | **Numele alimentului se copiază la generare** (snapshot), cu referința păstrată | Doar referința: lista s-ar schimba singură la redenumirea alimentului |
| D8 | **Plan gol → 400, lista rămâne neschimbată** (UC05 A1) | 200 cu listă goală: ar șterge lista existentă fără să dea nimic în schimb |

**De ce de data aceasta snapshot, după două module calculate la citire.** Rețeta (ADR-006) și planul (ADR-007) sunt descrieri ale unei intenții: trebuie să urmeze catalogul. Lista de cumpărături este un *document de lucru*: utilizatorul îl modifică în magazin (bifează, adaugă, șterge). Starea lui nu se poate reconstrui din plan. De aceea se salvează, iar „Generează” este acțiunea explicită care îl reface din plan.

**Agregarea (D3)** stă în Domain, ca funcție pură (`ShoppingListBuilder`), testată fără bază de date:
```
aliment planificat → (aliment, grame)
rețetă planificată → pentru fiecare ingredient: (aliment, grame × porții planificate / porțiile rețetei)
grupare după aliment, suma gramelor
```
Exemple: 150 + 250 = 400 g (criteriul SRS); 200 g ovăz într-o rețetă de 4 porții, planificată pentru 2 porții → 100 g; 100 g într-o rețetă de 3 porții, planificată pentru o porție → 33,33 g, afișat 34 g.

**Unități (UC05 A2).** Catalogul exprimă totul în grame, deci nu există unități incompatibile de agregat. Articolele manuale au cantitate text liber („2 bucăți”) și nu se agregă. Tabelul de conversie din SRS nu este necesar în această versiune.

**Integritatea în baza de date.** Un `CHECK` garantează cele două forme ale unui articol: generat (aliment + grame, fără text) sau manual (fără aliment, fără grame). Testul de integrare `CheckConstraint_RejectsDirectSqlInserts` verifică cele trei combinații interzise (cod `23514`).

**Categoria alimentului (D5).** `category` este opțională la creare și editare (implicit `other`) și apare în `FoodDto`. Migrarea pune categorii celor 10 alimente globale din seed, prin `UPDATE` după nume, iar seed-ul le setează și pentru bazele noi. O capcană evitată: un `HasDefaultValue(Other)` în modelul EF ar fi făcut ca valoarea 0 (`fruitsVegetables`, valoarea CLR implicită) să fie tratată drept „nesetată” și înlocuită cu `other`. Valoarea implicită stă de aceea în inițializatorul entității și în coloana creată de migrare, nu în model.

---

## 3. Consecințe

### Pozitive:
* **Criteriul SRS este verificat la toate nivelurile:** Domain (`Build_SrsExample_ChickenFromTwoRecipes_Is400`), Application și integrare pe PostgreSQL real.
* **Regenerarea este sigură:** articolele scrise de mână nu se pierd, iar un plan gol nu golește lista.
* **O singură regulă de interval** pentru plan și listă (`PlanRangeValidator`), extrasă din planificator în loc să fie copiată.
* **Testare:** suita backend are 509 teste (83 Domain, 13 Infrastructure, 281 Application, 132 de integrare).

### Riscuri asumate:
* **Bifele generate se pierd la regenerare** (D2). Utilizatorul generează de regulă o dată pe săptămână, înainte de cumpărături.
* **Lista nu se actualizează singură** când se schimbă planul; este nevoie de o nouă generare (consecința lui D1).
* **O singură listă per utilizator:** liste separate (de exemplu pe magazine) rămân dezvoltare viitoare.
