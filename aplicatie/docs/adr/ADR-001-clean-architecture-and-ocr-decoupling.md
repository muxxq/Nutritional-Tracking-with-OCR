# ADR-001: Decuplarea Microserviciului OCR și Adoptarea Clean Architecture pentru Backend

* **Status:** Acceptat (Accepted)
* **Data:** 2026-10-14
* **Autor:** Maximilian Andrei (Student)
* **Coordonator:** Dr. Ing. Mocanu Lucian-Mihai
* **Tichet asociat:** [NUT-12](https://maximilianandrei2101.atlassian.net/browse/NUT-12)

---

## 1. Context și Enunțul Problemei
Aplicația NutriScan necesită două capacități fundamentale diferite ca profil computațional:
1. **Core Business & Web API:** operațiuni CRUD cu latență scăzută, tranzacții ACID, autentificare securizată JWT, calcul nutrițional bazat pe formule matematice și interogări relaționale.
2. **Computer Vision & OCR:** preprocesare intensivă de matrici de pixeli cu OpenCV, filtrare zgomot și inferență neuronală (Deep Learning) pentru recunoașterea textului de pe etichete.

Dacă am fi inclus procesarea de imagini direct în monolit .NET, am fi întâmpinat dificultăți majore cu dependențele native de Python (ecosistemul de referință pentru Computer Vision / PyTorch / EasyOCR) și riscul ca o inferență OCR intensivă să blocheze thread pool-ul API-ului principal.

---

## 2. Decizia Luată
1. **Arhitectură pe microservicii decuplate:**
   * Backend-ul principal este dezvoltat în **ASP.NET Core 8 Web API** organizat riguros după principiile **Clean Architecture** (Domain, Application, Infrastructure, WebApi) și modelul **CQRS** (MediatR).
   * Subsistemul de viziune artificială este decuplat într-un microserviciu separat dezvoltat în **Python 3.12 (FastAPI + OpenCV)**.
2. **Comunicare sincronă rezilientă prin HTTP REST:**
   * Contractul dintre Backend și OCR este strict formalizat prin specificația OpenAPI (`POST /api/v1/ocr/process`).
   * Backend-ul apelează microserviciul OCR printr-un client HTTP configurat cu politici de reziliență **Polly** (Retry cu exponential backoff, Circuit Breaker).
3. **Persistență Relațională:**
   * Baza de date este **PostgreSQL 16**, administrată exclusiv prin Entity Framework Core din stratul de Infrastructure.

---

## 3. Consecințe și Beneficii pentru Teza de Licență

### Pozitive:
* **Separarea responsabilităților (Separation of Concerns):** Logica de business nu cunoaște detaliile de Computer Vision, ci primește doar un DTO structurat cu valorile nutriționale și gradul de încredere (*confidence score*).
* **Scalabilitate independentă:** Microserviciul OCR poate fi replicat sau mutat pe mașini virtuale cu accelerare GPU fără a modifica backend-ul.
* **Degradare grațioasă (Graceful Degradation):** Dacă containerul OCR este temporar oprit sau supraîncărcat, utilizatorii pot continua să folosească jurnalul alimentar manual, fără crash al întregului sistem.
* **Valoare academică:** Demonstrează competențe avansate de inginerie software, respectarea standardelor IEEE și arhitectură enterprise.

### Riscuri asumate și atenuare:
* *Latență de rețea între containere:* Rețeaua bridge Docker Compose (`nutriscan-network`) funcționează pe loopback virtual cu latențe sub 1ms, impactul fiind neglijabil în raport cu timpul de inferență OCR.
