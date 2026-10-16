# ADR-002: Strategia de Autentificare: JWT de Scurtă Durată și Refresh Token Rotativ în Cookie HttpOnly

* **Status:** Acceptat (Accepted)
* **Data:** 2026-10-16
* **Autor:** Maximilian Andrei (Student)
* **Coordonator:** Dr. Ing. Mocanu Lucian-Mihai
* **Spec:** [2026-10-16-auth-jwt-design.md](../specs/2026-10-16-auth-jwt-design.md)

---

## 1. Context și Enunțul Problemei
NutriScan stochează date personale de sănătate (greutate, jurnal alimentar), deci fiecare endpoint trebuie să știe cine face cererea. Clientul este un PWA React, folosit în paralel pe telefon și pe laptop. Cerințele:
1. Utilizatorul rămâne logat zile întregi fără să reintroducă parola.
2. Sesiunile de pe dispozitive diferite sunt independente.
3. Un token furat trebuie să fie cât mai puțin util atacatorului (OWASP Top 10: A01 Broken Access Control, A07 Identification and Authentication Failures).

Schema inițială ținea un singur câmp `RefreshToken` pe utilizator, iar autentificarea nu era configurată.

---

## 2. Deciziile Luate

| # | Decizie | Alternative respinse și motivul |
|---|---|---|
| D1 | Tabel separat `RefreshTokens`; fiecare login pornește o **familie** de tokenuri (`FamilyId`) | Un singur token pe `User`: logarea pe un dispozitiv l-ar deloga pe celălalt |
| D2 | **Access token** JWT în corpul răspunsului, ținut de client doar în memorie; **refresh token** în cookie `HttpOnly; Secure; SameSite=Strict; Path=/api/auth` | Ambele în `localStorage`: o singură vulnerabilitate XSS fură o sesiune de 7 zile. Ambele în cookie: access token-ul ar pleca automat la fiecare cerere, deci ar fi necesare tokenuri anti-CSRF |
| D3 | Înregistrare minimală (email, parolă, nume); profilul fizic se completează separat (onboarding), semnalat prin `isProfileComplete` | Formular complet la înregistrare: cuplează autentificarea de calculele BMR/TDEE și e greoi pe mobil |
| D4 | Parole: `PasswordHasher<User>` din ASP.NET Core Identity (PBKDF2-HMAC-SHA512, **210.000** iterații, OWASP 2023), în spatele interfeței `IPasswordHasher` | Argon2id: dependență terță, parametri reglați manual (poate înlocui implementarea fără a atinge Application). BCrypt: trunchiază parola la 72 de octeți. Identity complet: tabele proprii, conflict cu entitatea `User` |
| D5 | Refresh token: 32 de octeți aleatori; în DB se salvează doar **SHA-256**, cu index unic | Hash lent (bcrypt): inutil pentru 256 de biți de entropie și ar împiedica căutarea după index |
| D6 | JWT semnat **HS256**, valabil 15 minute, `ClockSkew` 30 s, algoritm fixat la validare | RS256: util doar când alte servicii validează tokenul; serviciul OCR nu primește JWT |
| D7 | Anti brute-force: rate limit 10 cereri/minut/IP pe `/api/auth`, blocare cont 5 minute după 5 eșecuri, verificare cu hash fals pentru email inexistent (timp de răspuns constant) | Blocare lungă sau permanentă: un atacator ar putea bloca victime la nesfârșit (DoS) |

**Detectarea reutilizării (RFC 9700, OAuth 2.0 Security BCP):** la fiecare refresh, tokenul vechi este revocat și înlocuit cu unul nou din aceeași familie. Dacă un token deja revocat este prezentat din nou, înseamnă că a fost copiat, așa că întreaga familie este revocată: atât atacatorul, cât și utilizatorul legitim trebuie să se relogheze.

---

## 3. Consecințe

### Pozitive:
* **Furtul access token-ului** are efect cel mult 15 minute; **refresh token-ul nu poate fi citit din JavaScript**.
* **Baza de date furată** nu permite autentificarea: conține doar hash-uri de parole și de tokenuri.
* **Clean Architecture demonstrată:** handlerele MediatR (Register, Login, Refresh, Logout, GetCurrentUser) depind doar de interfețe; criptografia stă în Infrastructure, cookie-urile în WebApi.
* **CORS restrâns** la originile configurate (înlocuiește `AllowAnyOrigin`); toate erorile sunt uniforme (ProblemDetails, RFC 7807).
* **Concurență optimistă:** `RevokedAtUtc` și `FailedLoginAttempts` sunt *concurrency tokens* EF Core. Două refresh-uri simultane cu același token nu pot crea două sesiuni valide (cererea care pierde cursa e tratată ca reutilizare), iar parolele greșite trimise în paralel sunt toate numărate, deci blocarea nu poate fi ocolită.
* **Testare:** 13 teste de domeniu, 38 de teste pe Application, 13 pe Infrastructure și 22 de teste de integrare pe PostgreSQL real (Testcontainers).

### Riscuri asumate (datorie tehnică):
* **Două refresh-uri simultane** cu același token (de exemplu două tab-uri) sunt tratate drept reutilizare și deloghează utilizatorul. Clientul (Faza 3) va trimite un singur refresh odată.
* **`POST /register` răspunde 409** pentru un email existent, deci dezvăluie existența contului. Alternativa (confirmare pe email) a fost amânată; rate limiting-ul limitează enumerarea.
* **Tokenurile expirate sau revocate rămân în tabel.** Este nevoie de un job periodic de curățare.
* **Sesiunile nu au durată absolută:** fiecare rotație acordă încă 7 zile, deci un utilizator activ rămâne logat oricât. O limită pe familie (de exemplu 30 de zile) poate fi adăugată ulterior.
* **Rate limiting-ul folosește IP-ul conexiunii.** În spatele unui reverse proxy trebuie configurat `ForwardedHeaders`, altfel toți clienții împart aceeași limită.
