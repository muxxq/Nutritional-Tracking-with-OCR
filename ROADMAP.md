# 🗺️ Master Roadmap — Lucrare de Licență NutriScan
**Universitatea din Craiova | Facultatea de Automatică, Calculatoare și Electronică (ACE)**  
**Domeniul:** Calculatoare și Tehnologia Informației (CTI)  
**Student:** Maximilian Andrei  
**Coordonator Științific:** Dr. Ing. Mocanu Lucian-Mihai  
**Titlu Proiect:** *Sistem de Monitorizare Nutrițională cu Extragere Automată a Datelor prin OCR*

---

## 🎯 Obiectiv și Viziune

Dezvoltarea unei soluții complete end-to-end compuse din:
1. **Produs Software Enterprise (NutriScan)**: Arhitectură decuplată (PWA React + Backend ASP.NET Core 8 Clean Architecture + Microserviciu Python OCR + PostgreSQL), axat pe detecția automată a etichetelor nutriționale și jurnalizare inteligentă cu asistență umană (*Human-in-the-Loop*).
2. **Lucrare Scrisă Academică (Teză de Licență)**: Document de nivel înalt structurat conform standardelor IEEE și metodologiei Facultății ACE, documentând riguros stadiul actual, proiectarea matematică/arhitecturală, inovația Computer Vision și validarea experimentală.

---

## 🔀 Replanificare (16.10.2026): Aplicație Full-Stack Întâi, OCR Ulterior

**Decizie:** Subsistemul OCR (fosta Fază 2) se mută după integrarea completă Backend ↔ Client. Până la implementarea OCR, utilizatorul introduce manual alimentele și valorile nutriționale.

**Motivație:**
* Este necesară devreme o aplicație full-stack funcțională (autentificare, jurnal, rețete, planificator, listă de cumpărături), utilizabilă și la alte discipline din facultate.
* OCR-ul este componenta cu cel mai mare risc și cea mai lungă durată de dezvoltare; dacă ar fi rămas primul, aplicația ar fi devenit funcțională foarte târziu.
* Formularul manual „Adaugă aliment” este reutilizat ca ecran *Human-in-the-Loop*: OCR-ul doar îl precompletează și marchează câmpurile cu încredere redusă. Nu se pierde muncă.
* Contractul `IOcrService` este definit din faza Backend (cu implementare stub), deci integrarea ulterioară înlocuiește doar implementarea (principiul inversării dependențelor).

**Măsuri de control al riscului** (OCR-ul rămâne contribuția științifică principală a tezei):
* ⏰ Implementarea OCR începe **cel târziu în Săptămâna 11**, indiferent de stadiul finisajelor din client.
* 📸 Colectarea dataset-ului de etichete (≥ 100 fotografii) începe **imediat, în paralel** (3–5 fotografii/zi).

---

## 📊 Strategia de Dezvoltare pe Două Piste (Dual-Track)

Fiecare etapă practică din software alimentează direct capitolele și anexele lucrării de licență:

```text
               TRACK SOFTWARE (Implementare)                 TRACK ACADEMIC (Redactare Teză)
               ─────────────────────────────                 ───────────────────────────────
Faza 1 ──► Scaffold & Arhitectură (Clean Arch, Docker) ──► Cap 3: SRS & Analiză Cerințe (UML, ERD)
Faza 2 ──► Backend API (.NET 8, CQRS, MediatR, JWT)    ──► Cap 4: Arhitectură Sistem & Cap 5.1: Backend
Faza 3 ──► Client PWA integrat (React, API real)       ──► Cap 5.3: Interfață & Experiență Utilizator
Faza 4 ──► Pipeline OCR (OpenCV, EasyOCR/Tesseract)    ──► Cap 5.2: Subsistem Computer Vision & AI
Faza 5 ──► Testare, Securitate & Benchmarks OCR/Load   ──► Cap 6: Testare, Evaluare & Rezultate
Faza 6 ──► Polish, Deployment & Demo Pregătire         ──► Cap 1, 2, 7: Introducere, Stadiu Actual, Concluzii

Pistă paralelă (Săpt. 3 – 11): 📸 colectare dataset etichete nutriționale pentru Faza 4
```

---

## ⏱️ Etapele Roadmap-ului (20 Săptămâni / 6 Fazele Cheie)

### 🔹 Faza 1: Fundație Tehnică, Arhitectură & Infrastructură (Săpt. 1 – 2) — ✅ Finalizată
> **Obiectiv principal:** Stabilirea mediului de lucru izolat, scheletul Clean Architecture, containerele Docker și modelul de date.

* [x] **Software:**
  * [x] Inițializare repository Git, ramuri `develop` / `main`, convenții commit (`feat:`, `fix:`).
    * [ ] Git hooks pentru validarea mesajelor de commit (neimplementat, opțional).
  * [x] Scaffold soluție .NET 8 Clean Architecture (`Domain`, `Application`, `Infrastructure`, `WebApi`).
  * [x] Scaffold serviciu Python 3.12 FastAPI cu structură modulară (`preprocessor`, `ocr_engine`, `parser`).
  * [x] Scaffold client React 18/19 cu TypeScript, Vite, Tailwind CSS și componente de bază.
  * [x] Configurare `docker-compose.yml` cu healthchecks, volume persistente și rețea internă dedicată.
  * [x] Modelare entități EF Core (`User`, `FoodItem`, `DiaryEntry`, `MealType`, `Recipe`, `RecipeIngredient`, `PlannedMeal`, `ShoppingListItem`).
  * [x] Rulare migrare inițială EF Core pe PostgreSQL și script seed date de test.
  * [x] Contract OpenAPI între Backend și Serviciul OCR.
* [x] **Academic / Documentație:**
  * [x] Finalizare document SRS (Specificația Cerințelor Software v2).
  * [x] Diagrame UML: Cazuri de Utilizare generale (UC01–UC08), Diagrama de Componente a sistemului.
  * [x] Diagrama Entitate-Relație (ERD) detaliată pentru capitolul de proiectare a bazei de date.
* 🏁 **Milestone M1 (atins):** *Toate containerele pornesc cu `docker compose up -d`, baza de date este migrată, Swagger și FastAPI Docs sunt accesibile.*

---

### 🔹 Faza 2: Backend Core API & Business Logic (Săpt. 3 – 7) — ✅ Software finalizat
> **Obiectiv principal:** Implementarea logicii de afaceri conform Clean Architecture & CQRS, securitate JWT și agregări nutriționale. Alimentele se introduc manual.

* [x] **Software:**
  * [x] Sistem de autentificare & autorizare: JWT Access Token (durată scurtă) + Refresh Token rotativ salvat securizat în PostgreSQL.
  * [x] Hashing parole folosind PBKDF2 / Argon2id; prevenire atacuri Brute Force.
  * [x] Modul Profil Utilizator: calcul automat rată metabolică bazală (BMR) și necesar caloric zilnic (TDEE) prin formula Mifflin-St Jeor; distribuție macronutrienți conform obiectivului (slăbire, menținere, hipertrofie).
  * [x] Modul Catalog Alimente: CRUD `FoodItem` cu **introducere manuală** a valorilor nutriționale per 100g și căutare după nume.
  * [x] Modul Jurnal Alimentar: Comenzi & Interogări MediatR pentru adăugare, editare, ștergere și vizualizare pe cele 4 mese principale, cu totaluri zilnice față de ținte.
  * [x] Statistici agregate (FR-08): `GET /api/stats` pe orice interval de până la un an, media pe zilele completate, abaterea față de țintă și numărul zilelor sub / în / peste țintă, agregate cu `GROUP BY` în PostgreSQL (ADR-009).
  * [x] Reguli de validare energetică (avertisment dacă diferența dintre calorii și formula `4P + 4C + 9G` > 15%): implementată ca avertisment în Domain (`EnergyCheck`), nu ca eroare `FluentValidation`, pentru că fibrele, alcoolul și poliolii pot explica legitim diferența.
  * [x] Modul Rețetar & Planificator Săptămânal: agregare ingrediente, recalcul nutrienți per porție, planificare pe zile. **Rețetar** (FR-09: CRUD cu arhivare, valori totale și per porție calculate la citire, rețete în jurnal în porții, ADR-006); **planificator** (FR-10, FR-11: mese planificate din alimente și rețete, starea zilei față de țintă ±5%, „Am mâncat asta” cu snapshot în jurnal, ADR-007).
  * [x] Generator automat Listă de Cumpărături (agregare cantități identice din mesele planificate): FR-12, agregare după aliment cu scalarea rețetelor la porțiile planificate, categorii pe aliment, bifare și articole manuale (ADR-008).
  * [x] Contract `IOcrService` în Application cu implementare stub în Infrastructure (pregătire pentru Faza 4): portul vorbește limbajul formularului de aliment, stub-ul întoarce „OCR indisponibil” (calea NFR-12), iar `POST /api/scans` validează fișierul după semnătura binară și dimensiune (400/413) (ADR-010).
  * [x] Teste unitare (xUnit, FluentAssertions, Moq) pentru Handlers & Domain Rules; Teste de integrare cu Testcontainers Postgres: 558 de teste; acoperire din teste unitare 87,8% Domain și 95,9% Application (Coverlet).
  * [x] CI GitHub Actions (NFR-17): build și toate testele la fiecare push, cu prag de acoperire de 70% pentru Domain și Application; Swagger documentat integral din comentariile XML, verificat de un test.
  * Amânat conștient: catalogul global administrat (FR-13, P3) și istoricul țintelor nutriționale.
* [ ] **Academic / Documentație:**
  * [ ] Redactare **Capitolul 4 (Arhitectura și Proiectarea Sistemului)**: Diagrame de secvență pentru fluxul de autentificare și jurnal, schema CQRS, decizii de design patterns.
  * [ ] Redactare **Capitolul 5.1 (Implementarea Backend-ului ASP.NET Core)**.
* 🏁 **Milestone M2 (atins pe partea software, 17.10.2026):** *API-ul acoperă toate cerințele funcționale P1 (fără OCR), documentat 100% în Swagger, acoperire teste unitare ≥ 70%.*

---

### 🔹 Faza 3: Client PWA Integrat & Experiență Utilizator (Săpt. 7 – 11)
> **Obiectiv principal:** Conectarea clientului React la API-ul real, înlocuirea datelor mock și livrarea unei aplicații full-stack funcționale cu introducere manuală.

* [ ] **Software:**
  * [x] Arhitectură frontend: React Router 7, TanStack Query pentru server-state, Zustand pentru sesiune; tipuri generate din contractul OpenAPI salvat în repo (ADR-011, NUT-27).
  * [x] Înlocuire `mockData.ts` cu apeluri reale către API (client HTTP tipizat, interceptor JWT + refresh automat) (NUT-46): toate ecranele rulează pe API-ul real; `mockData.ts` a fost șters.
  * [ ] Design System & Layout adaptiv: Dark/Light mode, Desktop split layout vs. Mobile bottom navigation bar.
  * [x] Ecrane de Onboarding & Autentificare: Login, Înregistrare, Formular configurare profil fizic și obiective (NUT-29): onboarding imediat după înregistrare, pagina `/profile` cu țintele calculate (BMR, TDEE) și suprascrierea lor manuală.
  * [x] Ecranul Principal (Dashboard & Jurnal): Inel caloric SVG interactiv, progress bars pentru macro-nutrienți, selector de date, listă mese zilnice (NUT-30, ADR-012): pe API-ul real, cu adăugare, editare, ștergere și ziua în URL.
  * [x] Formular „Adaugă aliment” (introducere manuală), proiectat pentru reutilizare ca ecran de validare *Human-in-the-Loop* în Faza 4 (NUT-47): câmpurile cu `needsReview` blochează salvarea până la confirmare (FR-05); scanarea trece prin `POST /api/scans` și, cât timp OCR-ul e stub, deschide formularul gol (NFR-12).
  * [x] Ecrane Rețetar & Planificator Săptămânal: adăugare ingrediente cu căutare instantanee, transfer rapid masă planificată în jurnalul zilei. **Rețetarul** e pe API-ul real (NUT-32): rețete cu previzualizare per porție, rețete în jurnal în porții, catalogul de alimente cu editarea și arhivarea alimentelor proprii. **Planificatorul** e pe API-ul real: săptămâna în URL, starea fiecărei zile față de țintă, mese din rețete sau alimente, „Am mâncat asta” și generarea listei de cumpărături pentru săptămână.
  * [x] Ecranul Listă de Cumpărături: articole agregate, marcare interactivă checkbox, ștergere elemente bifate (NUT-32): grupare pe raioane, bifare optimistă cu revenire la eroare, articole manuale, generare din planul săptămânii.
  * [ ] Configurare PWA: `manifest.json`, iconițe adaptive, Service Worker pentru caching resurse statice și funcționare fără conexiune pentru vizualizare catalog.
* [ ] **Academic / Documentație:**
  * [ ] Redactare **Capitolul 5.3 (Dezvoltarea Interfeței cu Utilizatorul și Arhitectura Client PWA)**.
  * [ ] Capturi de ecran comparative Desktop vs. Mobile.
* 🏁 **Milestone M3:** *Aplicație full-stack funcțională (fără OCR): înregistrare, profil, jurnal, rețete, planificator și listă de cumpărături pe date reale; instalabilă ca PWA pe telefon.*

---

### 🔹 Faza 4: Microserviciul AI & Computer Vision (OCR) (Săpt. 11 – 15)
> **Obiectiv principal:** Realizarea pipeline-ului complet de procesare a imaginii, extragerea structurată a valorilor nutriționale per 100g și integrarea în fluxul *Human-in-the-Loop*.

* [ ] **Software — Microserviciu OCR:**
  * [ ] Dataset de test (≥ 100 fotografii reale de ambalaje alimentare: cutii, pungi, sticle, unghiuri oblice, iluminări diverse) — **colectat în paralel din Săpt. 3**.
  * [ ] Dezvoltare pipeline preprocesare OpenCV: grayscale, Gaussian blur, corecție perspectivă/rotire, binarizare adaptivă (Otsu / adaptive threshold).
  * [ ] Integrare motoare OCR: EasyOCR (Deep Learning) și Tesseract 5; comparație preliminară a acurateței.
  * [ ] Dezvoltare parser semantic regex cu toleranță la erori comune OCR (ex: `O` în loc de `0`, virgule vs puncte, identificare rânduri/coloane per 100g).
  * [ ] Calcul scor de încredere (*confidence score*) per câmp extras (Energie/Kcal, Proteine, Glucide, Grăsimi, Fibre, Sodiu).
  * [ ] Endpoint FastAPI `POST /api/v1/ocr/process` cu validare strictă (dimensiune max 10MB, verificare MIME real prin magic bytes).
  * [ ] Suită de teste Pytest pe dataset-ul de referință cu măsurători automate de acuratețe.
* [ ] **Software — Integrare:**
  * [ ] Implementare reală `OcrServiceClient` (în locul stub-ului) folosind `HttpClientFactory`, Polly (Retry cu exponential backoff, Circuit Breaker) și degradare grațioasă către introducerea manuală.
  * [ ] Modulul Scanner OCR în client:
    * [ ] Acces la camera dispozitivului cu ghidaj vizual (overlay dreptunghiular pentru etichetă).
    * [ ] Animație de scanare în timp ce microserviciul procesează imaginea.
    * [ ] Ecran de Validare Asistată (*Human-in-the-Loop*): formularul „Adaugă aliment” precompletat, câmpurile cu încredere redusă evidențiate în galben/roșu.
* [ ] **Academic / Documentație:**
  * [ ] Redactare **Capitolul 5.2 (Subsistemul de Procesare a Imaginii și Recunoaștere Optică a Caracterelor)**.
  * [ ] Documentarea algoritmilor de preprocesare OpenCV cu imagini „înainte și după”.
  * [ ] Diagrame de stare/secvență pentru fluxul de scanare OCR; actualizare `docs/adr/ADR-001-ocr-engine-selection.md` cu rezultatele comparației.
* 🏁 **Milestone M4:** *O imagine de etichetă este procesată în < 3 secunde, iar valorile apar precompletate în ecranul HitL; MVP complet testat pe dispozitiv fizic.*

---

### 🔹 Faza 5: Testare E2E, Evaluare Experimentală & Securitate (Săpt. 15 – 17)
> **Obiectiv principal:** Validarea riguroasă a sistemului, benchmark-uri științifice pentru lucrarea de licență și audit de securitate.

* [ ] **Software & QA:**
  * [ ] Testare automată End-to-End cu **Playwright**: flux complet Înregistrare ➡️ Fotografiere etichetă ➡️ Validare HitL ➡️ Salvare în Jurnal ➡️ Verificare recalculare total zilnic.
  * [ ] Testare de sarcină și performanță cu **Locust** sau **k6**: măsurare timpi de răspuns (p50, p90, p95) pentru API (target: < 300ms) și serviciul OCR (target: < 3s).
  * [ ] Evaluare experimentală a modulului OCR:
    * [ ] Rulare benchmark comparativ: Acuratețe EasyOCR vs. Tesseract pe setul de 100+ etichete.
    * [ ] Calculul metricilor: Rata de detecție exactă, rata de eroare per macronutrient, matrice de confuzie pentru caractere numerice.
    * [ ] Măsurarea degradării acurateței în funcție de zgomot, unghi și calitatea iluminării.
  * [ ] Audit de Securitate conform OWASP Top 10:
    * [ ] Verificare protecție SQL Injection (parametri EF Core).
    * [ ] Verificare Cross-Site Scripting (XSS) și sanitizare input-uri.
    * [ ] Audit Cross-Site Request Forgery (CSRF) și securitate antete HTTP (CORS, CSP).
    * [ ] Verificare izolare date utilizatori (acces neautorizat pe ID străin ➡️ 404/403).
* [ ] **Academic / Documentație:**
  * [ ] Redactare **Capitolul 6 (Testare, Evaluare Experimentală și Analiza Performanței)**:
    * [ ] Grafice și tabele comparative pentru acuratețea OCR.
    * [ ] Grafice de încărcare și latență API din rapoartele Locust/k6.
    * [ ] Matricea de trasabilitate a cerințelor (verificarea fiecărui FR și NFR din SRS).
* 🏁 **Milestone M5:** *Toate testele trec, raportul experimental de acuratețe și performanță este generat și inclus în teză.*

---

### 🔹 Faza 6: Redactare Finală a Tezei, Polish & Pregătirea Susținerii (Săpt. 18 – 20)
> **Obiectiv principal:** Finalizarea tezei de licență, formatare academică impecabilă și pregătirea discursului și a demonstrației practice.

* [ ] **Academic & Lucrare Scrisă:**
  * [ ] Redactare **Capitolul 1 (Introducere)**: Context general, obezitate & stil de viață modern, importanța monitorizării nutriționale, obiectivele proiectului.
  * [ ] Redactare **Capitolul 2 (Stadiul Actual al Tehnologiei)**: Analiză comparativă a aplicațiilor existente (MyFitnessPal, Cronometer, Yuka, MacroFactor) și analiza stadiului tehnologiilor OCR aplicate pe etichete nutritionale.
  * [ ] Redactare **Capitolul 7 (Concluzii, Contribuții Proprii și Dezvoltări Viitoare)**.
  * [ ] Colectare și formatare referințe bibliografice conform standardului IEEE/APA (cărți, articole științifice IEEE/ACM, documentații oficiale).
  * [ ] Formatare finală a documentului conform ghidului de redactare al Facultății ACE (font, margini, numerotare ecuații, figuri, tabele).
  * [ ] Verificare anti-plagiat și corectură gramaticală/stilistică.
* [ ] **Prezentare & Susținere:**
  * [ ] Creare set de slide-uri pentru susținere (15–20 slide-uri concentrate pe arhitectură, inovație OCR, contribuții originale și rezultate experimentale).
  * [ ] Pregătire scenariu de demonstrație LIVE cu telefonul fizic și mostre reale de produse alimentare.
  * [ ] Înregistrare video demonstrativ de rezervă (backup video în caz de probleme tehnice la videoproiector/rețea).
* 🏁 **Milestone M6:** *Lucrare predată oficial la secretariat / coordonator + slide-uri și demo pregătite pentru susținere.*

---

## 📈 Matrice de Sincronizare: Capitole Licență vs. Module Software

| Capitol Lucrare Licență | Denumire Capitol | Modul Software / Artefact Corelat | Stadiu |
|---|---|---|:---:|
| **Cap. 1** | Introducere și Motivarea Temei | Viziune generală, context de domeniu | 📝 În plan |
| **Cap. 2** | Stadiul Actual al Domeniului | Analiză comparativă (MyFitnessPal, Yuka etc.) | 📝 În plan |
| **Cap. 3** | Analiza Cerințelor și Proiectarea Conceptuală | Document SRS IEEE 830, Diagrame Use Case & ERD | ✅ SRS + diagrame finalizate |
| **Cap. 4** | Arhitectura Generală a Sistemului | Clean Architecture, CQRS, Diagrame Componente/Secvență | 📝 În plan |
| **Cap. 5.1** | Implementare: Backend API (.NET 8) | Cod sursă `aplicatie/backend/`, MediatR, EF Core | 🟡 Cod finalizat, redactare în curs |
| **Cap. 5.2** | Implementare: Serviciul Computer Vision (OCR) | Cod sursă `aplicatie/ocr-service/`, OpenCV, EasyOCR | 📝 Faza 4 (schelet existent) |
| **Cap. 5.3** | Implementare: Interfața Client PWA (React) | Cod sursă `aplicatie/client/`, Tailwind, shadcn/ui | 🟡 Ecrane pe date mock (Faza 3) |
| **Cap. 6** | Testare, Verificare și Evaluare Experimentală | Teste xUnit/Pytest/Playwright, Benchmarks OCR & Locust | 📝 În plan |
| **Cap. 7** | Concluzii, Contribuții Proprii și Perspective | Raport final, limitări asumate, dezvoltări viitoare | 📝 În plan |

---

## 🛡️ Matrice de Management al Riscurilor

| Risc Identificat | Probabilitate | Impact | Plan de Atenuare (Mitigation Strategy) |
|---|:---:|:---:|---|
| **Acuratețea OCR este scăzută pe ambalaje lucioase/curbate** | Ridicată | Ridicat | Fluxul **Human-in-the-Loop** permite corecția în 2 secunde; utilizarea filtrelor adaptive OpenCV; raportare onestă în teză ca subiect de cercetare. |
| **Comportament diferit al camerei pe iOS Safari PWA** | Medie | Mediu | Testare pe dispozitiv fizic iPhone încă din Săptămâna 10; implementare fallback `input type="file" capture="environment"`. |
| **Complexitatea CQRS încetinește implementarea frontend-ului** | Medie | Mediu | Generare contracte DTO/OpenAPI din timp; folosire mock API sau stub-uri în frontend pentru dezvoltare paralelă. |
| **Implementarea OCR întârzie din cauza mutării după integrarea full-stack** | Medie | Critic | Termen ferm: OCR începe cel târziu în Săpt. 11; dataset-ul se colectează în paralel din Săpt. 3; contractul `IOcrService` + stub există din Faza 2; finisajele UI se amână înaintea OCR. |
| **Timp insuficient pentru redactarea lucrării scrise** | Medie | Critic | Abordare continuă (*Dual-Track*): fiecare sprint tehnic include obligatoriu redactarea secțiunii corespunzătoare din teză. |

---

## 🚦 Pasul Următor (Sprint 3 — Faza 3: Client Integrat)
Backend-ul din Faza 2 este gata: autentificare, profil și ținte, catalog, jurnal, statistici, rețetar, planificator, listă de cumpărături, contractul de scanare cu OCR stub (ADR-002 – ADR-010), CI cu prag de acoperire.
1. **Faza 3:** clientul integrat pe API-ul real (rutare, TanStack Query, interceptor JWT cu refresh, înlocuirea `mockData.ts`), inclusiv ecranul de scanare pe calea „OCR indisponibil”.
2. **Academic:** Capitolul 4 (diagrame de secvență, schema CQRS) și Capitolul 5.1, din ADR-urile 002–010.
3. **În paralel**: colectarea fotografiilor de etichete pentru dataset-ul OCR (Faza 4 începe cel târziu în săptămâna 11).
