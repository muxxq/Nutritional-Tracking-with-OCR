# Proiect Licență — NutriScan: Sistem de Monitorizare Nutrițională cu OCR

* **Universitatea:** Universitatea din Craiova  
* **Facultatea:** Facultatea de Automatică, Calculatoare și Electronică (ACE)  
* **Departamentul:** Calculatoare
* **Student:** Maximilian Andrei  
* **Coordonator Științific:** Dr. Ing. Mocanu Lucian-Mihai  

---

## 📌 Stadiul actual

| Componentă | Stadiu |
|---|---|
| Backend API (ASP.NET Core 8, Clean Architecture, CQRS) | ✅ Funcțional: autentificare JWT, profil cu ținte calculate (BMR/TDEE), catalog alimente, jurnal, rețetar, planificator săptămânal, listă de cumpărături, statistici |
| Client PWA (React, TypeScript) | ✅ Funcțional pe API-ul real, toate ecranele de mai sus; temă dark/light; instalabil ca PWA |
| Microserviciul OCR (Python, FastAPI, OpenCV) | 🚧 Schelet; pipeline-ul real de recunoaștere este în lucru (Faza 4) |
| Lucrarea scrisă | 🚧 SRS finalizat; capitolele în redactare |

Până la finalizarea OCR-ului, alimentele se introduc manual. Butonul de scanare răspunde „OCR indisponibil” și deschide formularul gol de adăugare aliment, la fel ca atunci când serviciul OCR nu este disponibil.

Planul complet: [ROADMAP.md](ROADMAP.md).

---

## 🧪 Ghid de testare (rulare pe alt calculator)

### 1. Cerințe

* **Docker** cu **Docker Compose v2**: [Docker Desktop](https://www.docker.com/products/docker-desktop/) pe Windows / macOS, sau Docker Engine pe Linux.
* **Git**.
* Porturi libere: `3000`, `5000`, `8000` și `5432`.

Nu este nevoie de .NET, Node.js, Python sau PostgreSQL instalate local: totul rulează în containere.

### 2. Pornire

```bash
git clone <adresa-repository-ului>
cd <folderul-clonat>/aplicatie
cp .env.example .env        # Windows (cmd): copy .env.example .env
docker compose up -d --build
```

Fișierul `.env` conține parola bazei de date și secretul JWT. Valorile implicite din `.env.example` sunt suficiente pentru testare locală.

Prima pornire durează câteva minute (se descarcă imaginile de bază și se compilează backend-ul). La pornire, backend-ul creează automat schema bazei de date și încarcă un catalog inițial de alimente.

Starea containerelor se verifică cu `docker compose ps`: toate patru (`nutriscan-db`, `nutriscan-ocr`, `nutriscan-backend`, `nutriscan-client`) trebuie să fie `Up`.

### 3. Adrese

| Ce | Adresă |
|---|---|
| **Aplicația (interfața web)** | http://localhost:3000 |
| Documentația API (Swagger) | http://localhost:5000/swagger |
| Documentația serviciului OCR | http://localhost:8000/docs |

### 4. Scenariu de test sugerat

1. Deschideți http://localhost:3000 și creați un cont (**Înregistrare**). Parola trebuie să aibă minimum 8 caractere.
2. Completați profilul fizic (data nașterii, înălțime, greutate, nivel de activitate, obiectiv). Aplicația calculează necesarul caloric (BMR, TDEE) și țintele de macronutrienți, vizibile în pagina **Profil**.
3. **Jurnal:** adăugați alimente la una dintre cele 4 mese, căutând în catalog (ex. „ou”, „ovăz”, „banană”). Inelul caloric și barele de macronutrienți se actualizează față de ținte.
4. **Aliment nou:** din fereastra de adăugare în jurnal, creați manual un aliment cu valorile nutriționale per 100 g de pe o etichetă. Dacă energia declarată diferă cu peste 15% de `4P + 4C + 9G`, aplicația afișează un avertisment la pasul de cantitate.
5. **Rețete:** creați o rețetă din mai multe ingrediente. Valorile per porție se calculează automat, iar rețeta se poate adăuga în jurnal în porții.
6. **Planificator:** planificați mese pe săptămână. Fiecare zi arată dacă se încadrează în țintă, iar „Am mâncat asta” trece masa în jurnal.
7. **Listă de cumpărături:** generați lista din planul săptămânii; cantitățile identice sunt agregate, iar articolele se pot bifa.
8. **Statistici:** mediile săptămânale/lunare și caloriile pe zi față de țintă.

Interfața este adaptivă: pe un ecran îngust (sau cu instrumentele de dezvoltare ale browserului în modul mobil) apare bara de navigare de jos, ca pe telefon.

### 5. Oprire

```bash
docker compose down        # oprește containerele, păstrează datele
docker compose down -v     # oprește containerele și șterge baza de date
```

### 6. Probleme frecvente

* **Portul 5432 este ocupat** (există deja un PostgreSQL local): în `aplicatie/.env` setați `POSTGRES_PORT=5433` și reporniți cu `docker compose up -d`.
* **Pagina nu se încarcă imediat după pornire:** backend-ul are nevoie de câteva secunde ca să aplice migrările. Jurnalele se văd cu `docker compose logs backend`.
* **Datele vechi incompatibile după o actualizare a repository-ului:** `docker compose down -v`, apoi `docker compose up -d --build`.

### 7. Rularea testelor automate (opțional)

Necesită [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) și [Node.js 22](https://nodejs.org/). Testele de integrare pornesc singure un PostgreSQL temporar prin Docker (Testcontainers).

```bash
# Backend: teste unitare și de integrare
cd aplicatie/backend
dotnet test

# Client: teste de componente
cd aplicatie/client
npm ci
npm test
```

Aceleași teste rulează automat în GitHub Actions la fiecare push (`.github/workflows/`).

---

## 📁 Structura repository-ului

```text
├── README.md                      # Acest fișier
├── ROADMAP.md                     # Planul pe faze și stadiul fiecăreia
│
├── aplicatie/                     # Codul sursă al aplicației
│   ├── docker-compose.yml         # Pornește baza de date, backend-ul, serviciul OCR și clientul
│   ├── .env.example               # Model pentru configurația locală (.env)
│   ├── README.md                  # Detalii tehnice despre arhitectură și module
│   ├── backend/                   # ASP.NET Core 8 Web API (Domain, Application, Infrastructure, WebApi)
│   ├── client/                    # React + TypeScript PWA
│   ├── ocr-service/               # Microserviciu Python (FastAPI + OpenCV)
│   └── docs/                      # Decizii de arhitectură (ADR), specificații, rezumate de sprint
│
└── documentatie/                  # Materiale academice
    ├── design/mockup.html         # Prototip vizual interactiv (Desktop + Mobil)
    └── srs/                       # Specificația cerințelor software (SRS, IEEE 830)
```

Deciziile tehnice sunt documentate în [aplicatie/docs/adr/](aplicatie/docs/adr/), iar ce s-a livrat în fiecare sprint în [aplicatie/docs/sprints/](aplicatie/docs/sprints/).
