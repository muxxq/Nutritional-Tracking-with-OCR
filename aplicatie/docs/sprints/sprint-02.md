# 🏃‍♂️ Sprint 2 — Backend Core: Autentificare
**Faza din roadmap:** Faza 2: Backend core (săptămânile 3–7)
**Branch:** `feature/NUT-auth-jwt`
**Status:** 🟡 În desfășurare (autentificarea și profilul sunt finalizate în backend)

---

## 🎯 Obiectivul Sprintului
Primul modul al backend-ului real: conturi de utilizator și sesiuni securizate. Toate modulele următoare (Profil, Jurnal, Alimente) se bazează pe identitatea stabilită aici.

---

## 📋 Story: Autentificare JWT cu refresh token rotativ — ✅ Backend finalizat

> Ca utilizator NutriScan, vreau să îmi creez un cont și să mă autentific, ca datele mele nutriționale să fie private și să rămân logat pe telefon și pe laptop în același timp, fără să reintroduc parola la fiecare deschidere a aplicației.

**Documente:** [spec](../specs/2026-10-16-auth-jwt-design.md) · [plan de implementare](../plans/2026-10-16-auth-jwt.md) · [ADR-002](../adr/ADR-002-authentication-strategy.md)

### Criterii de acceptanță

| AC | Descriere | Acoperit de |
|---|---|---|
| AC1 | Register → 201, utilizator logat imediat | `Register_ValidRequest_Returns201WithTokenAndSecureCookie` |
| AC2 | Email duplicat (orice majuscule) → 409 | `Register_DuplicateEmailDifferentCase_Returns409` |
| AC3 | Login corect → 200; greșit → 401 generic | `Login_ValidCredentials_Returns200WithCookie`, `Login_WrongPassword_Returns401Generic` |
| AC4 | 5 eșecuri → cont blocat 5 minute | `Login_AfterFiveFailures_CorrectPasswordStillReturns401` |
| AC5 | Refresh rotește tokenul | `Refresh_WithValidCookie_Returns200AndRotatesCookie` |
| AC6 | Reutilizare → 401 și familia revocată | `Refresh_WithReusedToken_Returns401AndClearsCookie` |
| AC7 | Logout doar pe dispozitivul curent | `Logout_RevokesSessionButNotOtherDevice` |
| AC8 | `/me` protejat | `Me_WithoutToken_Returns401`, `Me_WithAccessToken_Returns200` |
| AC9 | Rate limit → 429 | `AuthEndpoints_EleventhRequestInWindow_Returns429` |
| AC10 | Pornire refuzată fără secret JWT de minimum 32 de octeți | `JwtOptionsValidatorTests` |
| AC11 | CORS doar pentru originile configurate | `CorsTests` |

### Realizări
* Entitatea `RefreshToken` și regulile de blocare a contului pe `User`; migrarea `AddRefreshTokensAndLockout`.
* Primele handlere MediatR reale: `Register`, `Login`, `Refresh`, `Logout`, `GetCurrentUser`, cu validatoare FluentValidation.
* `PasswordHasherService` (PBKDF2 210k), `JwtTokenService` (HS256), validarea configurării la pornire.
* `AuthController`, erori ProblemDetails globale, rate limiting, CORS cu origini explicite.
* Proiecte de test noi: `Infrastructure.UnitTests` și `WebApi.IntegrationTests` (Testcontainers PostgreSQL). **86 de teste automate, toate trec**, inclusiv scenarii de cereri concurente (refresh dublu, parole greșite în paralel).

### Rularea testelor de integrare
Necesită un motor de containere. Cu Docker: utilizatorul trebuie să fie în grupul `docker`. Cu Podman rootless:
```bash
systemctl --user start podman.socket
export DOCKER_HOST=unix:///run/user/$(id -u)/podman/podman.sock TESTCONTAINERS_RYUK_DISABLED=true
dotnet test
```

---

## 📋 Story: Profil și ținte nutriționale (FR-02) — ✅ Backend finalizat

> Ca utilizator NutriScan, vreau să îmi completez profilul fizic și să primesc automat ținte zilnice de calorii și macronutrienți potrivite obiectivului meu, pe care să le pot ajusta manual.

**Documente:** [spec](../specs/2026-10-16-profile-targets-design.md) · [plan](../plans/2026-10-16-profile-targets.md) · [ADR-003](../adr/ADR-003-nutrition-target-calculation.md)

| AC | Descriere | Acoperit de |
|---|---|---|
| AC1 | Profil nou → ținte `default` | `GetProfile_AfterRegister_ReturnsDefaults` |
| AC2 | Profil R1 → 1700 kcal, 124 / 195 / 47 g | `UpdateProfile_R1_ReturnsCalculatedTargets`, `Calculate_R1_WomanLosingWeight` |
| AC3 | `isProfileComplete` și în `/api/auth/me` | `UpdateProfile_MakesAuthMeProfileComplete` |
| AC4 | Suprascriere și revenire la calculat | `UpdateTargets_ManualProteins_RecalculatesCarbs` |
| AC5 | Schimbarea greutății păstrează țintele manuale | `UpdateProfile_KeepsManualTargets` |
| AC6 | Date invalide → 400 | `UpdateProfile_InvalidValues_Returns400` + validatoare |
| AC7 | Suprascrieri imposibile → 400 | `UpdateTargets_Impossible_Returns400` |
| AC8 | Doar propriul profil, cu token | `Profile_WithoutToken_Returns401`, `Profile_TwoUsers_SeeOnlyTheirOwn` |
| AC9 | Enum-uri ca text | `UpdateProfile_R1_ReturnsCalculatedTargets` |

**Realizări:** `NutritionTargetCalculator` (funcție pură în Domain), migrarea `ProfileTargetOverrides`, `GetProfile` / `UpdateProfile` / `UpdateTargets`, `ProfileController`, enum-uri ca text la nivel global. **158 de teste backend, toate trec.**

## ⏭️ Următorii pași din sprint
* Modulul Catalog Alimente: CRUD `FoodItem` cu introducere manuală și căutare după nume.
* Ecranele de login, înregistrare și onboarding din client (Faza 3), cu un singur refresh în zbor.
