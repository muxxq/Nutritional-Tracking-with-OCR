# Spec: Contractul `IOcrService` și endpoint-ul de scanare (backend, stub)

**Data:** 2026-10-17
**Branch:** `feature/NUT-ocr-contract`
**Cerințe SRS:** UC01 (pașii 3–6, A1, A3), FR-03, FR-05 (pragul de încredere), NFR-09 (fișiere încărcate), NFR-12 (degradare controlată)
**Faza din roadmap:** Faza 2 (pregătire pentru Faza 4, conform replanificării din 16.10.2026)

---

## 1. Scop

OCR-ul real se implementează în Faza 4. Până atunci, clientul (Faza 3) trebuie să poată construi deja ecranul de scanare și formularul *Human-in-the-Loop* pe un contract stabil. Faza 2 livrează:
1. portul `IOcrService` în Application (principiul inversării dependențelor, ADR-001);
2. o implementare stub în Infrastructure, care răspunde „OCR indisponibil” (exact calea de degradare NFR-12);
3. endpoint-ul `POST /api/scans`, cu toate validările de securitate ale fișierului, care rămân neschimbate în Faza 4.

În Faza 4 se înlocuiește **doar** înregistrarea din DI: `StubOcrService` → `OcrServiceClient` (HTTP + Polly). Controller-ul, comanda, validatorul și DTO-ul rămân.

### User story
> Ca utilizator NutriScan, vreau să trimit fotografia unei etichete și să primesc formularul de aliment precompletat, cu câmpurile nesigure marcate; dacă recunoașterea nu este disponibilă, vreau să pot completa formularul manual, fără eroare.

### Criterii de acceptanță
- **AC1:** `POST /api/scans` (multipart, câmpul `file`) cu o imagine JPEG, PNG sau WebP validă întoarce 200 `ScanResultDto`.
- **AC2 (stub, NFR-12):** cât timp OCR-ul nu este implementat, răspunsul are `status = "unavailable"`, un mesaj pentru utilizator și toate câmpurile `null`; clientul afișează formularul gol.
- **AC3 (FR-05):** un câmp cu încredere sub **0,80** are `needsReview = true` (exemplul SRS: proteine cu 0,42); la 0,80 sau peste, `false`.
- **AC4 (NFR-09, UC01 A1):** tipul fișierului se verifică după **semnătura binară (magic bytes)**, nu după extensie sau `Content-Type`; un text redenumit `.jpg` întoarce 400.
- **AC5 (NFR-09):** un fișier gol sau lipsă întoarce 400; un fișier mai mare de 10 MB întoarce 413.
- **AC6:** imaginea nu se salvează nicăieri (SRS §6.4): este procesată în memorie și eliberată.
- **AC7:** ruta cere access token (401).

### În afara scopului
Clientul HTTP real, Polly, maparea `sodium → sare` (toate în Faza 4); salvarea metadatelor scanării; HEIC (vezi D5); limitarea numărului de scanări per utilizator.

---

## 2. Decizii (rezumat pentru ADR-010)

| # | Întrebare | Decizie | Alternative respinse |
|---|---|---|---|
| D1 | În ce limbaj vorbește portul? | **În limbajul formularului de aliment**: aceleași câmpuri ca `FoodItem` (calorii, proteine, carbohidrați, zaharuri, grăsimi, grăsimi saturate, fibre, sare), fiecare cu valoare și încredere. Traducerea din JSON-ul serviciului Python (de ex. sodiu → sare × 2,5) este treaba adaptorului din Infrastructure | Portul copiază JSON-ul serviciului Python: orice schimbare a microserviciului s-ar propaga în Application și în client (lipsa unui *anti-corruption layer*) |
| D2 | Ce întoarce stub-ul? | **`Unavailable`** cu câmpuri goale: este exact calea de degradare (NFR-12), deci clientul o implementează de la început | Valori inventate: clientul ar fi testat pe date false, iar demo-ul ar fi înșelător. `NotImplementedException`: 500 |
| D3 | OCR indisponibil: ce cod HTTP? | **200 cu `status = "unavailable"`** și mesaj | 503: un rezultat gol este un răspuns valid al ecranului de scanare (formular manual), nu o eroare; clientul ar trata două căi pentru același ecran |
| D4 | Unde se verifică fișierul? | **Dimensiunea în controller (413)**, plus o limită de cerere în Kestrel; **tipul real în validatorul comenzii (400)**, prin semnătura binară | Doar `Content-Type` / extensie: sunt controlate de client și pot fi falsificate (OWASP: *Unrestricted File Upload*) |
| D5 | Ce formate? | **JPEG, PNG, WebP**, ca în contractul OCR înghețat (§4 din `architecture_and_contracts.md`) | HEIC (menționat în SRS NFR-09): OpenCV nu îl decodează fără biblioteci suplimentare, iar clientul comprimă oricum imaginea în JPEG înainte de upload (NFR-02). Abaterea de la SRS este asumată și documentată |
| D6 | Pragul pentru `needsReview` | **0,80, o singură constantă în Application**, expusă ca `needsReview` | Pragul în client: regula FR-05 s-ar duplica în fiecare client și n-ar putea fi calibrată din serverul care știe motorul OCR |

---

## 3. Contract

| Metodă și rută | Succes | Erori |
|---|---|---|
| `POST /api/scans` (`multipart/form-data`, câmp `file`) | 200 `ScanResultDto` | 400, 401, 413 |

```jsonc
// ScanResultDto (stub)
{ "status": "unavailable", "message": "Recunoașterea automată nu este disponibilă acum. Completează valorile manual.",
  "servingBase": null, "overallConfidence": null,
  "fields": { "calories": null, "proteins": null, "carbohydrates": null, "sugars": null,
              "fats": null, "saturatedFats": null, "fiber": null, "saltGrams": null } }

// ScanResultDto (Faza 4)
{ "status": "processed", "message": null, "servingBase": "per100g", "overallConfidence": 0.86,
  "fields": { "calories": { "value": 389, "confidence": 0.95, "needsReview": false },
              "proteins": { "value": 16.9, "confidence": 0.42, "needsReview": true }, … } }
```

Semnături acceptate: JPEG `FF D8 FF`; PNG `89 50 4E 47 0D 0A 1A 0A`; WebP `RIFF????WEBP`.

---

## 4. Testare

| Nivel | Ce acoperă |
|---|---|
| `Application.UnitTests` | detectarea formatului (semnături valide, GIF, PDF, text, fișiere scurte); validatorul (gol, > 10 MB, tip greșit); handler-ul cu `IOcrService` simulat (`needsReview` la 0,42 / 0,80 / 0,95; calea `unavailable`; formatul detectat transmis portului) |
| `Infrastructure.UnitTests` | stub-ul întoarce `Unavailable` |
| `WebApi.IntegrationTests` | PNG valid → 200 `unavailable`; text redenumit `.jpg` → 400; fără fișier → 400; > 10 MB → 413; fără token → 401 |
