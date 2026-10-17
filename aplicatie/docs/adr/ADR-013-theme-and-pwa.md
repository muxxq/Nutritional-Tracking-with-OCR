# ADR-013: Tema Luminoasă/Întunecată și Configurarea PWA

* **Status:** Acceptat (Accepted)
* **Data:** 2026-10-17
* **Autor:** Maximilian Andrei (Student)
* **Coordonator:** Dr. Ing. Mocanu Lucian-Mihai
* **Cerințe SRS:** NFR-04 (încărcare din cache), NFR-14 (instalare PWA), NFR-15 (utilizabilitate)
* **Jira:** NUT-28 (design system și layout), NUT-33 (PWA)

---

## 1. Context
1. **Tema era inconsecventă.** În Tailwind v4, varianta `dark:` urmează implicit `prefers-color-scheme`, nu clasa `.dark` pusă de butonul din bara de sus. Configurarea `darkMode: ["class"]` era într-un `tailwind.config.js` pe care v4 nu îl mai citește. Efectul: butonul schimba doar fundalul paginii, iar un sistem setat pe „dark” făcea componentele închise pe un fundal deschis. În plus, alegerea se pierdea la reîncărcare.
2. **Aplicația nu era instalabilă:** lipseau manifestul, iconițele și service worker-ul cerute de NFR-14.

## 2. Deciziile Luate

| # | Decizie | Alternative respinse și motivul |
|---|---|---|
| D1 | **`@custom-variant dark (&:where(.dark, .dark *))`**: toate utilitarele `dark:` urmează clasa de pe `<html>`; fișierul de configurare v3, nefolosit, a fost șters | Doar `prefers-color-scheme`: utilizatorul n-ar putea alege tema. Păstrarea config-ului v3: sugerează o setare care nu are efect |
| D2 | **Tema inițială = alegerea salvată, altfel preferința sistemului**; un script de câteva linii din `index.html` o aplică înainte de prima afișare | Aplicarea din React: utilizatorii cu temă închisă ar vedea un flash alb la fiecare pornire |
| D3 | **`vite-plugin-pwa` (Workbox, `generateSW`)**: manifest, iconițe normale și *maskable*, precache pentru interfață, actualizare automată | Service worker scris de mână: logica de versionare și invalidare a precache-ului e ușor de greșit |
| D4 | **În cache-ul service worker-ului intră doar catalogul de alimente** (`GET /api/foods`, *NetworkFirst*, timeout 4 s, 7 zile); jurnalul, profilul și planul nu intră | Cache pentru tot API-ul: date personale pe disc, servite eventual învechite. *CacheFirst* pentru catalog: o corectură a unui aliment n-ar ajunge la utilizator |
| D5 | **La deconectare se șterge cache-ul catalogului**, iar deconectarea reușește și offline | Păstrarea cache-ului: pe un dispozitiv partajat, următorul utilizator ar vedea alimentele personale ale celui anterior |
| D6 | **Navigările offline primesc interfața din cache**; `/api` și `/swagger` sunt excluse din această regulă | Fără regulă de fallback: offline, o rută precum `/diary` ar da eroarea browserului |

## 3. Consecințe
* **Pozitive:** tema e consecventă și persistentă; aplicația se instalează pe ecranul principal, pornește din cache (NFR-04) și catalogul rămâne consultabil fără semnal în timpul unei sesiuni deschise.
* **Riscuri asumate:**
  * Offline, după o **reîncărcare**, sesiunea nu poate fi restaurată (refresh-ul cere rețea), deci catalogul din cache e accesibil doar într-o sesiune deja deschisă.
  * Instalarea pe iPhone cere **HTTPS**: se verifică pe dispozitiv fizic cu un tunel HTTPS sau la găzduire (Faza 6), inclusiv că fotografia pleacă JPEG (ADR-010 D5).
  * Scriptul inline din `index.html` va avea nevoie de un hash în politica CSP la auditul OWASP din Faza 5.
  * Service worker-ul e activ doar în build-ul de producție (`npm run build` + `vite preview`), nu în `npm run dev`.
