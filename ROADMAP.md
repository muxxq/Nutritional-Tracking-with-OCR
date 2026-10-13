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

## 📊 Strategia de Dezvoltare pe Două Piste (Dual-Track)

Fiecare etapă practică din software alimentează direct capitolele și anexele lucrării de licență:

```text
               TRACK SOFTWARE (Implementare)                 TRACK ACADEMIC (Redactare Teză)
               ─────────────────────────────                 ───────────────────────────────
Faza 1 ──► Scaffold & Arhitectură (Clean Arch, Docker) ──► Cap 3: SRS & Analiză Cerințe (UML, ERD)
Faza 2 ──► Pipeline OCR (OpenCV, EasyOCR/Tesseract)    ──► Cap 5.2: Subsistem Computer Vision & AI
Faza 3 ──► Backend API (.NET 8, CQRS, MediatR, JWT)    ──► Cap 4: Arhitectură Sistem & Cap 5.1: Backend
Faza 4 ──► Frontend PWA (React, Vite, shadcn/ui)       ──► Cap 5.3: Interfață & Experiență Utilizator
Faza 5 ──► Testare, Securitate & Benchmarks OCR/Load   ──► Cap 6: Testare, Evaluare & Rezultate
Faza 6 ──► Polish, Deployment & Demo Pregătire         ──► Cap 1, 2, 7: Introducere, Stadiu Actual, Concluzii
```

---

## ⏱️ Etapele Roadmap-ului (20 Săptămâni / 6 Fazele Cheie)

### 🔹 Faza 1: Fundație Tehnică, Arhitectură & Infrastructură (Săpt. 1 – 2)
> **Obiectiv principal:** Stabilirea mediului de lucru izolat, scheletul Clean Architecture, containerele Docker și modelul de date.

* [ ] **Software:**
  * [ ] Inițializare repository Git, ramuri `develop` / `main`, configurare git hooks & convenții commit (`feat:`, `fix:`).
  * [ ] Scaffold soluție .NET 8 Clean Architecture (`Domain`, `Application`, `Infrastructure`, `WebApi`).
  * [ ] Scaffold serviciu Python 3.12 FastAPI cu structură modulară (`preprocessor`, `ocr_engine`, `parser`).
  * [ ] Scaffold client React 18/19 cu TypeScript, Vite, Tailwind CSS și componente de bază shadcn/ui.
  * [ ] Configurare `docker-compose.yml` cu healthchecks, volume persistente și rețea internă dedicată.
  * [ ] Modelare entități EF Core (`User`, `FoodItem`, `DiaryEntry`, `MealType`, `Recipe`, `PlannedMeal`, `ShoppingListItem`).
  * [ ] Rulare migrare inițială EF Core pe PostgreSQL și script seed date de test.
  * [ ] Contract OpenAPI între Backend și Serviciul OCR.
* [ ] **Academic / Documentație:**
  * [ ] Finalizare document SRS (Specificația Cerințelor Software v2).
  * [ ] Diagrame UML: Cazuri de Utilizare generale (UC01–UC08), Diagrama de Componente a sistemului.
  * [ ] Diagrama Entitate-Relație (ERD) detaliată pentru capitolul de proiectare a bazei de date.
* 🏁 **Milestone M1:** *Toate containerele pornesc cu `docker compose up -d`, baza de date este migrată, Swagger și FastAPI Docs sunt accesibile.*

---

### 🔹 Faza 2: Microserviciul AI & Computer Vision (OCR) (Săpt. 3 – 6)
> **Obiectiv principal:** Realizarea pipeline-ului complet de procesare a imaginii și extragerea structurată a valorilor nutriționale per 100g.

* [ ] **Software:**
  * [ ] Colectare și etichetare dataset de test (≥ 100 fotografii reale de ambalaje alimentare: cutii, pungi, sticle, unghiuri oblice, iluminări diverse).
  * [ ] Dezvoltare pipeline preprocesare OpenCV: grayscale, Gaussian blur, corecție perspectivă/rotire, binarizare adaptivă (Otsu / adaptive threshold).
  * [ ] Integrare motoare OCR: EasyOCR (Deep Learning) și Tesseract 5; comparație preliminară a acurateței.
  * [ ] Dezvoltare parser semantic regex cu toleranță la erori comune OCR (ex: `O` în loc de `0`, virgule vs puncte, identificare rânduri/coloane per 100g).
  * [ ] Calcul scor de încredere (*confidence score*) per câmp extras (Energie/Kcal, Proteine, Glucide, Grăsimi, Fibre, Sodiu).
  * [ ] Creare endpoint FastAPI `POST /api/v1/ocr/process` cu validare strictă (dimensiune max 10MB, verificare MIME real prin magic bytes).
  * [ ] Suită de teste Pytest pe dataset-ul de referință cu măsurători automate de acuratețe.
* [ ] **Academic / Documentație:**
  * [ ] Redactare **Capitolul 5.2 (Subsistemul de Procesare a Imaginii și Recunoaștere Optică a Caracterelor)**.
  * [ ] Documentarea algoritmilor de preprocesare OpenCV cu imagini „înainte și după”.
  * [ ] Salvarea primelor decizii de arhitectură în `docs/adr/ADR-001-ocr-engine-selection.md`.
* 🏁 **Milestone M2:** *Endpoint-ul OCR procesează o imagine de etichetă și returnează JSON valid cu macronutrienți și scor de încredere în < 3 secunde.*

---

### 🔹 Faza 3: Backend Core API & Business Logic (Săpt. 5 – 9)
> **Obiectiv principal:** Implementarea logicii de afaceri conform Clean Architecture & CQRS, securitate JWT și agregări nutriționale.

* [ ] **Software:**
  * [ ] Sistem de autentificare & autorizare: JWT Access Token (durată scurtă) + Refresh Token rotativ salvat securizat în PostgreSQL.
  * [ ] Hashing parole folosind PBKDF2 / Argon2id; prevenire atacuri Brute Force.
  * [ ] Modul Profil Utilizator: calcul automat rată metabolică bazală (BMR) și necesar caloric zilnic (TDEE) prin formula Mifflin-St Jeor; distribuție macronutrienți conform obiectivului (slăbire, menținere, hipertrofie).
  * [ ] Modul Jurnal Alimentar: Comenzi & Interogări MediatR pentru adăugare, editare, ștergere și vizualizare pe cele 4 mese principale.
  * [ ] Client integrat pentru OCR (`OcrServiceClient`) folosind `HttpClientFactory`, Polly (Retry cu exponential backoff, Circuit Breaker) și degradare grațioasă.
  * [ ] Reguli de validare energetică în `FluentValidation` (ex: avertisment dacă diferența dintre calorii și formula `4P + 4C + 9G` > 15%).
  * [ ] Modul Rețetar & Planificator Săptămânal: agregare ingrediente, recalcul nutrienți per porție, planificare pe zile.
  * [ ] Generator automat Listă de Cumpărături (agregare cantități identice din mesele planificate).
  * [ ] Teste unitare (xUnit, FluentAssertions, Moq) pentru Handlers & Domain Rules; Teste de integrare cu Testcontainers Postgres.
* [ ] **Academic / Documentație:**
  * [ ] Redactare **Capitolul 4 (Arhitectura și Proiectarea Sistemului)**: Diagrame de secvență pentru fluxul de scanare și autentificare, schema CQRS, decizii de design patterns.
  * [ ] Redactare **Capitolul 5.1 (Implementarea Backend-ului ASP.NET Core)**.
* 🏁 **Milestone M3:** *API-ul acoperă toate cerințele funcționale P1, documentat 100% în Swagger, acoperire teste unitare ≥ 70%.*

---

### 🔹 Faza 4: Frontend Client PWA & Experiență Utilizator (Săpt. 9 – 14)
> **Obiectiv principal:** Construirea unei aplicații web progresive (PWA) fluide, responsive pentru mobil și desktop, cu flux OCR Human-in-the-Loop.

* [ ] **Software:**
  * [ ] Arhitectură frontend: Rutare (React Router / TanStack Router), State management (Zustand & TanStack Query pentru caching server-state).
  * [ ] Design System & Layout adaptiv: Dark/Light mode, Desktop split layout vs. Mobile bottom navigation bar bazat pe componente shadcn/ui.
  * [ ] Ecrane de Onboarding & Autentificare: Login, Înregistrare, Formular configurare profil fizic și obiective.
  * [ ] Ecranul Principal (Dashboard & Jurnal): Inel caloric SVG interactiv, progress bars pentru macro-nutrienți, selector de date, listă mese zilnice.
  * [ ] Modulul Scanner OCR (Camera & Upload):
    * [ ] Acces la camera dispozitivului cu ghidaj vizual (overlay dreptunghiular pentru etichetă).
    * [ ] Animație de scanare în timp ce microserviciul procesează imaginea.
    * [ ] Ecran de Validare Asistată (*Human-in-the-Loop*): formular precompletat în care câmpurile cu încredere redusă sunt evidențiate în galben/roșu pentru confirmarea sau corectarea de către utilizator.
  * [ ] Ecrane Rețetar & Planificator Săptămânal: adăugare ingrediente cu căutare instantanee, transfer rapid masă planificată în jurnalul zilei.
  * [ ] Ecranul Listă de Cumpărături: articole agregate, marcare interactivă checkbox, ștergere elemente bifate.
  * [ ] Configurare PWA: `manifest.json`, iconițe adaptive, Service Worker pentru caching resurse statice și funcționare fără conexiune pentru vizualizare catalog.
* [ ] **Academic / Documentație:**
  * [ ] Redactare **Capitolul 5.3 (Dezvoltarea Interfeței cu Utilizatorul și Arhitectura Client PWA)**.
  * [ ] Capturi de ecran comparative Desktop vs. Mobile și diagrame de stare pentru fluxul de scanare OCR.
* 🏁 **Milestone M4:** *MVP complet integrat, instalabil ca PWA pe telefon (iOS / Android), testat pe dispozitiv fizic.*

---

### 🔹 Faza 5: Testare E2E, Evaluare Experimentală & Securitate (Săpt. 14 – 17)
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
| **Cap. 3** | Analiza Cerințelor și Proiectarea Conceptuală | Document SRS IEEE 830, Diagrame Use Case & ERD | ✅ SRS Finalizat |
| **Cap. 4** | Arhitectura Generală a Sistemului | Clean Architecture, CQRS, Diagrame Componente/Secvență | 📝 În plan |
| **Cap. 5.1** | Implementare: Backend API (.NET 8) | Cod sursă `aplicatie/backend/`, MediatR, EF Core | ⏳ Urmează |
| **Cap. 5.2** | Implementare: Serviciul Computer Vision (OCR) | Cod sursă `aplicatie/ocr-service/`, OpenCV, EasyOCR | ⏳ Urmează |
| **Cap. 5.3** | Implementare: Interfața Client PWA (React) | Cod sursă `aplicatie/client/`, Tailwind, shadcn/ui | ⏳ Urmează |
| **Cap. 6** | Testare, Verificare și Evaluare Experimentală | Teste xUnit/Pytest/Playwright, Benchmarks OCR & Locust | 📝 În plan |
| **Cap. 7** | Concluzii, Contribuții Proprii și Perspective | Raport final, limitări asumate, dezvoltări viitoare | 📝 În plan |

---

## 🛡️ Matrice de Management al Riscurilor

| Risc Identificat | Probabilitate | Impact | Plan de Atenuare (Mitigation Strategy) |
|---|:---:|:---:|---|
| **Acuratețea OCR este scăzută pe ambalaje lucioase/curbate** | Ridicată | Ridicat | Fluxul **Human-in-the-Loop** permite corecția în 2 secunde; utilizarea filtrelor adaptive OpenCV; raportare onestă în teză ca subiect de cercetare. |
| **Comportament diferit al camerei pe iOS Safari PWA** | Medie | Mediu | Testare pe dispozitiv fizic iPhone încă din Săptămâna 10; implementare fallback `input type="file" capture="environment"`. |
| **Complexitatea CQRS încetinește implementarea frontend-ului** | Medie | Mediu | Generare contracte DTO/OpenAPI din timp; folosire mock API sau stub-uri în frontend pentru dezvoltare paralelă. |
| **Timp insuficient pentru redactarea lucrării scrise** | Medie | Critic | Abordare continuă (*Dual-Track*): fiecare sprint tehnic include obligatoriu redactarea secțiunii corespunzătoare din teză. |

---

## 🚦 Primul Pas Imediat (Sprint 1)
Pentru a începe efectiv execuția conform regulilor din `AGENTS.md`:
1. **Scaffold-ul tehnic**: Inițializarea soluției .NET 8 Web API, proiectului FastAPI și aplicației Vite React în directoarele corespunzătoare din `aplicatie/`.
2. **Validarea Docker**: Verificarea build-ului complet `docker compose up --build`.
3. **Generarea primelor diagrame Mermaid** pentru Capitolul 4 în `aplicatie/docs/`.
