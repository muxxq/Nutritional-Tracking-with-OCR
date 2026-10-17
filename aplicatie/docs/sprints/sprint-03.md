# 🏃‍♂️ Sprint 3 — Client PWA Integrat (Faza 3)
**Faza din roadmap:** Faza 3: Client PWA integrat pe API-ul real (săptămânile 7–11)
**Status:** ✅ Software finalizat (17.10.2026); rămân capturile de ecran și Capitolul 5.3

---

## 🎯 Obiectivul Sprintului
Conectarea clientului React la API-ul real și eliminarea datelor mock: o aplicație full-stack funcțională cu introducere manuală (Milestone M3), pregătită pentru OCR-ul din Faza 4.

## 📋 Livrări

| Jira | Ce s-a livrat | Decizii |
|---|---|---|
| NUT-27, NUT-46 | React Router 7, TanStack Query, sesiune Zustand doar în memorie, client tipizat generat din `backend/openapi.json`, un singur refresh în zbor pentru cererile concurente, proxy Vite pentru `/api` | [ADR-011](../adr/ADR-011-client-architecture.md) |
| NUT-29 | Login, înregistrare, onboarding imediat după înregistrare, `/profile` cu BMR/TDEE și suprascrierea țintelor | ADR-003, ADR-011 |
| NUT-30, NUT-47 | Jurnalul pe API (ziua în URL, adăugare, editare, ștergere), formularul „Adaugă aliment” cu confirmarea explicită a câmpurilor nesigure (FR-05), scanarea prin `POST /api/scans` pe calea „OCR indisponibil” | [ADR-012](../adr/ADR-012-diary-screen-and-hitl-form.md) |
| NUT-32 | Rețetar (previzualizare per porție, rețete în jurnal), catalog (editare și arhivare a alimentelor proprii), planificator (săptămâna în URL, starea zilei, „Am mâncat asta”), lista de cumpărături (bifare optimistă, articole manuale, generare din plan) | ADR-006 – ADR-008 |
| NUT-48 | Statistici săptămânale și lunare: medii pe zilele notate, abatere, zile sub/în/peste țintă, grafic cu tooltip și vedere tabelară | [ADR-009](../adr/ADR-009-statistics.md) |
| NUT-28, NUT-33 | Temă consecventă și persistentă (corectură Tailwind v4), PWA instalabilă: manifest, iconițe *maskable*, precache, catalog offline, cache șters la deconectare | [ADR-013](../adr/ADR-013-theme-and-pwa.md) |

## 🐞 Defecte găsite și corectate pe parcurs
* `OcrScanModal` apela hook-uri după un `return` condiționat (eroare React la deschidere).
* Regula `lib/` din `.gitignore` ascundea `client/src/lib`: un checkout curat nu se putea construi.
* `dark:` din Tailwind v4 urma setarea sistemului, nu butonul de temă.
* `<input type="number">` respingea virgula zecimală („6,9”).
* Swagger nu publica nulabilitatea: tipurile generate ar fi marcat totul opțional.

## ✅ Calitate
* **46 de teste client** (Vitest + Testing Library) pentru: refresh-ul unic în zbor, gărzile de rută, login, onboarding, jurnal, formularul HitL, rețetar, planificator, lista de cumpărături, statistici, temă și deconectare.
* **CI client** (`.github/workflows/client-ci.yml`): verificarea tipurilor generate față de contract, lint, build, teste.
* Backend: **562 de teste**, inclusiv testul care compară `openapi.json` cu API-ul real.
* Fiecare flux a fost verificat și prin proxy, pe API-ul real cu PostgreSQL (profilul R1 → 1700 kcal; rețeta SRS → 194,5 kcal/porție).

## ⏭️ Următorii pași
* Capturi Desktop vs. Mobile și **Capitolul 5.3**; verificarea instalării PWA pe iPhone fizic (cere HTTPS).
* **Faza 4 (OCR)**, cel târziu în săptămâna 11: dataset (NUT-13, încă neînceput), pipeline OpenCV, motoare OCR, adaptorul `OcrServiceClient` cu Polly (NUT-22), ecranul de scanare cu cameră (NUT-31).
