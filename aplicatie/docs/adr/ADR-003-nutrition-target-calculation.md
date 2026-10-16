# ADR-003: Calculul Țintelor Nutriționale: Valori Derivate cu Suprascrieri Manuale

* **Status:** Acceptat (Accepted)
* **Data:** 2026-10-16
* **Autor:** Maximilian Andrei (Student)
* **Coordonator:** Dr. Ing. Mocanu Lucian-Mihai
* **Cerință SRS:** FR-02 „Profil și Calculul Targetului Nutrițional”
* **Spec:** [2026-10-16-profile-targets-design.md](../specs/2026-10-16-profile-targets-design.md)

---

## 1. Context și Enunțul Problemei
Jurnalul alimentar are sens doar raportat la o țintă zilnică de calorii și macronutrienți. FR-02 cere ca ținta să fie **calculată** din profil (vârstă, sex, înălțime, greutate, activitate, obiectiv) cu ecuația Mifflin–St Jeor și să poată fi **suprascrisă manual**. Schema inițială stoca 4 ținte fixe pe `User` (2000 kcal etc.), fără nicio legătură cu profilul.

---

## 2. Deciziile Luate

| # | Decizie | Alternative respinse și motivul |
|---|---|---|
| D1 | Se stochează doar **suprascrierile opționale** (câte una per țintă); valorile calculate se **derivă** la fiecare citire; fiecare țintă își raportează sursa (`default` / `calculated` / `manual`) | Comutator global „manual”: prea grosier. Valori stocate cu buton „Recalculează”: țintele rămân vechi după o schimbare de greutate |
| D2 | BMR cu **Mifflin–St Jeor** (1990) | Harris–Benedict (1919): supraestimează; meta-analiza Frankenfield (2005) arată că Mifflin e cea mai precisă la adulți |
| D3 | Calorii = TDEE × **0,80 / 1,00 / 1,10** (slăbire / menținere / masă musculară), cu prag de **1200 kcal (F) / 1500 kcal (M)** pentru țintele calculate | Deficit fix (−500 kcal): disproporționat pentru persoanele mici |
| D4 | Proteine **2,0 / 1,6 / 1,8 g/kg** (ISSN 2017), raportate la greutatea limitată la **IMC 25**; grăsimi **25%** din calorii (AMDR 20–35%); carbohidrații primesc restul | Greutatea totală: la obezitate supraestimează proteinele, iar la limitele validării lasă carbohidrați negativi |
| D5 | Calculul e o **funcție pură în Domain** (`NutritionTargetCalculator`), fără bază de date, ceas sau HTTP | În handler: logica de business ar ajunge în Application și n-ar mai putea fi refolosită de jurnal |
| D6 | Endpoint-uri separate: `PUT /api/profile` (profil fizic) și `PUT /api/profile/targets` (ținte) | Un singur endpoint: amestecă două formulare și două seturi de reguli de validare |

### Formulele
```
BMR      = 10·G + 6,25·Î − 5·V + 5 (bărbați) | − 161 (femei)
TDEE     = BMR × {1,2; 1,375; 1,55; 1,725; 1,9}
Calorii  = max(prag, Round10(TDEE × factor_obiectiv))
Proteine = min(G, 25·Î²) × g/kg_obiectiv
Grăsimi  = Calorii × 0,25 / 9
Carbo    = (Calorii − 4·Proteine − 9·Grăsimi) / 4
```

### Combinarea cu suprascrierile
Suprascrierile sunt **intrări** ale calculului, nu înlocuiri la final. Diferența de energie e preluată de primul macronutrient nesuprascris dintre carbohidrați și grăsimi. Dacă ambii sunt manuali, caloriile se derivă din macronutrienți (`4P + 4C + 9G`) sau, când sunt și ele manuale, trebuie să se încadreze în **±15%**. Valorile negative sau inconsistente sunt respinse cu 400. Dacă o schimbare de profil face imposibile suprascrieri existente, acestea revin la valorile calculate. Citirea (`GET`) nu eșuează niciodată din cauza suprascrierilor: dacă trecerea timpului (de exemplu o aniversare care scade BMR-ul) le face imposibile, țintele se afișează calculate, fără a modifica valorile stocate. Pe un profil incomplet, energia macronutrienților setați manual nu poate depăși caloriile (manuale sau implicite) cu peste 15%.

---

## 3. Consecințe

### Pozitive:
* **Țintele nu se pot desincroniza de profil:** valoarea calculată nu este stocată.
* **Testabilitate completă:** 5 cazuri de referință (R1–R5), calculate de mână, sunt verificate exact de teste. Totalul suitei backend: 165 de teste.
* **Transparență pentru utilizator și comisie:** API-ul expune BMR, TDEE și sursa fiecărei ținte.
* **Refolosire:** jurnalul și statisticile (FR-07) vor apela același calculator.

### Riscuri asumate:
* **Formulele populaționale au o eroare individuală de aproximativ ±10%.** Suprascrierea manuală există tocmai pentru ajustarea pe baza evoluției reale a greutății.
* **Limitarea la IMC 25 subestimează proteinele** pentru persoanele foarte musculoase (IMC ridicat din masă musculară). Pot seta proteinele manual.
* **Pragul de vârstă de 18 ani** exclude adolescenții, pentru care formulele și obiectivele de slăbire nu sunt potrivite fără supraveghere.

---

## 4. Referințe
1. Mifflin M.D., St Jeor S.T. et al. *A new predictive equation for resting energy expenditure in healthy individuals.* Am J Clin Nutr, 51(2), 1990.
2. Frankenfield D., Roth-Yousey L., Compher C. *Comparison of predictive equations for resting metabolic rate in healthy nonobese and obese adults: a systematic review.* J Am Diet Assoc, 105(5), 2005.
3. Jäger R. et al. *International Society of Sports Nutrition Position Stand: protein and exercise.* J Int Soc Sports Nutr, 14:20, 2017.
4. Institute of Medicine. *Dietary Reference Intakes for Energy, Carbohydrate, Fiber, Fat, Fatty Acids, Cholesterol, Protein, and Amino Acids* (AMDR). National Academies Press, 2005.
