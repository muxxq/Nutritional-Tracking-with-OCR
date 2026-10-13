import sys
from docx import Document
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT
from docx.oxml import OxmlElement, parse_xml
from docx.oxml.ns import nsdecls, qn

OUT = sys.argv[1]
FONT = "Calibri"
BLACK = RGBColor(0, 0, 0)
CONTENT_W = 6.27  # inch, A4 minus 1" margins

doc = Document()

# ---------- styles ----------
st = doc.styles["Normal"]
st.font.name = FONT
st.font.size = Pt(12)
st.font.color.rgb = BLACK
st.element.rPr.rFonts.set(qn("w:eastAsia"), FONT)

for name, size, before, after in [("Heading 1", 18, 20, 8), ("Heading 2", 15, 15, 6), ("Heading 3", 13, 11, 4)]:
    h = doc.styles[name]
    h.font.name = FONT
    h.font.size = Pt(size)
    h.font.bold = True
    h.font.italic = False
    h.font.color.rgb = BLACK
    rpr = h.element.get_or_add_rPr()
    rf = rpr.find(qn("w:rFonts"))
    if rf is None:
        rf = OxmlElement("w:rFonts")
        rpr.append(rf)
    for a in ("w:ascii", "w:hAnsi", "w:eastAsia", "w:cs"):
        rf.set(qn(a), FONT)
    for a in ("w:asciiTheme", "w:hAnsiTheme", "w:eastAsiaTheme", "w:cstheme"):
        if rf.get(qn(a)) is not None:
            del rf.attrib[qn(a)]
    pf = h.paragraph_format
    pf.space_before = Pt(before)
    pf.space_after = Pt(after)
    pf.keep_with_next = True

# ---------- page setup, header, footer ----------
sec = doc.sections[0]
sec.page_width = Inches(8.27)
sec.page_height = Inches(11.69)
for m in ("top_margin", "bottom_margin", "left_margin", "right_margin"):
    setattr(sec, m, Inches(1.0))
sec.different_first_page_header_footer = True

hp = sec.header.paragraphs[0]
hp.alignment = WD_ALIGN_PARAGRAPH.RIGHT
r = hp.add_run("Specificația Cerințelor Software (SRS) | Standard IEEE 830")
r.font.size = Pt(10)


def add_field(par, instr):
    r1 = par.add_run()
    f1 = OxmlElement("w:fldChar"); f1.set(qn("w:fldCharType"), "begin"); r1._r.append(f1)
    r2 = par.add_run()
    it = OxmlElement("w:instrText"); it.set(qn("xml:space"), "preserve"); it.text = instr; r2._r.append(it)
    r3 = par.add_run()
    f2 = OxmlElement("w:fldChar"); f2.set(qn("w:fldCharType"), "separate"); r3._r.append(f2)
    r4 = par.add_run("1")
    r5 = par.add_run()
    f3 = OxmlElement("w:fldChar"); f3.set(qn("w:fldCharType"), "end"); r5._r.append(f3)
    for rr in (r1, r2, r3, r4, r5):
        rr.font.size = Pt(10)


fp = sec.footer.paragraphs[0]
fp.alignment = WD_ALIGN_PARAGRAPH.CENTER
r = fp.add_run("Lucrare de Licență — Aplicație Full Stack Tracking Nutrițional | Pagina ")
r.font.size = Pt(10)
add_field(fp, "PAGE")


# ---------- helpers ----------
def cell_bg(cell, fill="FFFFFF"):
    cell._element.get_or_add_tcPr().append(parse_xml(f'<w:shd {nsdecls("w")} w:val="clear" w:color="auto" w:fill="{fill}"/>'))


def cell_margins(cell, top=80, bottom=80, left=100, right=100):
    cell._element.get_or_add_tcPr().append(parse_xml(
        f'<w:tcMar {nsdecls("w")}><w:top w:w="{top}" w:type="dxa"/><w:left w:w="{left}" w:type="dxa"/>'
        f'<w:bottom w:w="{bottom}" w:type="dxa"/><w:right w:w="{right}" w:type="dxa"/></w:tcMar>'))


def table_borders(table, sz=6):
    b = "".join(f'<w:{s} w:val="single" w:sz="{sz}" w:space="0" w:color="000000"/>'
                for s in ("top", "left", "bottom", "right", "insideH", "insideV"))
    table._element.tblPr.append(parse_xml(f'<w:tblBorders {nsdecls("w")}>{b}</w:tblBorders>'))


def repeat_header(row):
    trPr = row._tr.get_or_add_trPr()
    el = OxmlElement("w:tblHeader"); el.set(qn("w:val"), "true"); trPr.append(el)


def no_split(row):
    trPr = row._tr.get_or_add_trPr()
    trPr.append(OxmlElement("w:cantSplit"))


def write_cell(cell, text, size=11, bold=False, bold_prefix=None):
    """text may contain '\n' -> separate paragraphs inside the cell."""
    lines = text.split("\n")
    for i, line in enumerate(lines):
        p = cell.paragraphs[0] if i == 0 else cell.add_paragraph()
        p.paragraph_format.space_after = Pt(1 if i < len(lines) - 1 else 0)
        p.paragraph_format.space_before = Pt(0)
        p.paragraph_format.line_spacing = 1.1
        rr = p.add_run(line)
        rr.font.size = Pt(size)
        rr.bold = bold


def h1(t): doc.add_heading(t, level=1)
def h2(t): doc.add_heading(t, level=2)
def h3(t): doc.add_heading(t, level=3)


def body(text, bold_prefix="", italic=False):
    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(2)
    p.paragraph_format.space_after = Pt(5)
    p.paragraph_format.line_spacing = 1.25
    p.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
    if bold_prefix:
        p.add_run(bold_prefix).bold = True
    rr = p.add_run(text)
    rr.italic = italic
    return p


def bullet(text, bold_prefix=""):
    p = doc.add_paragraph(style="List Bullet")
    p.paragraph_format.space_before = Pt(2)
    p.paragraph_format.space_after = Pt(3)
    p.paragraph_format.line_spacing = 1.25
    if bold_prefix:
        p.add_run(bold_prefix).bold = True
    p.add_run(text)
    return p


def numbered(items):
    for i, t in enumerate(items, 1):
        p = doc.add_paragraph()
        p.paragraph_format.left_indent = Inches(0.35)
        p.paragraph_format.first_line_indent = Inches(-0.25)
        p.paragraph_format.space_after = Pt(3)
        p.paragraph_format.line_spacing = 1.25
        p.add_run(f"{i}.  ")
        p.add_run(t)


def spacer(pt=6):
    p = doc.add_paragraph()
    p.paragraph_format.space_after = Pt(0)
    p.paragraph_format.space_before = Pt(0)
    p.paragraph_format.line_spacing = Pt(pt)


def grid_table(headers, rows, widths, size=10.5, bold_cols=()):
    t = doc.add_table(rows=1 + len(rows), cols=len(headers))
    t.alignment = WD_TABLE_ALIGNMENT.CENTER
    t.autofit = False
    table_borders(t)
    for j, w in enumerate(widths):
        t.columns[j].width = Inches(w)
    for j, h in enumerate(headers):
        c = t.rows[0].cells[j]
        c.width = Inches(widths[j])
        cell_bg(c, "E7E7E7"); cell_margins(c)
        write_cell(c, h, size=size + 0.5, bold=True)
    repeat_header(t.rows[0])
    for i, row in enumerate(rows, 1):
        no_split(t.rows[i])
        for j, v in enumerate(row):
            c = t.rows[i].cells[j]
            c.width = Inches(widths[j])
            cell_bg(c); cell_margins(c, 60, 60, 90, 90)
            write_cell(c, v, size=size, bold=(j in bold_cols))
    spacer()
    return t


def kv_table(rows, w0=1.8):
    t = doc.add_table(rows=len(rows), cols=2)
    t.alignment = WD_TABLE_ALIGNMENT.CENTER
    t.autofit = False
    table_borders(t)
    widths = (w0, CONTENT_W - w0)
    for j, w in enumerate(widths):
        t.columns[j].width = Inches(w)
    for i, (k, v) in enumerate(rows):
        no_split(t.rows[i])
        c0, c1 = t.rows[i].cells
        c0.width, c1.width = Inches(widths[0]), Inches(widths[1])
        cell_bg(c0, "F2F2F2"); cell_bg(c1)
        cell_margins(c0); cell_margins(c1)
        write_cell(c0, k, bold=True)
        write_cell(c1, v)
    spacer()
    return t


def card(title, fields):
    """Requirement card: bordered box with thick left rule, like the reference SRS."""
    t = doc.add_table(rows=1, cols=1)
    t.alignment = WD_TABLE_ALIGNMENT.CENTER
    t.autofit = False
    t.columns[0].width = Inches(CONTENT_W)
    no_split(t.rows[0])
    c = t.cell(0, 0)
    c.width = Inches(CONTENT_W)
    cell_bg(c); cell_margins(c, 120, 120, 200, 200)
    c._element.get_or_add_tcPr().append(parse_xml(
        f'<w:tcBorders {nsdecls("w")}><w:top w:val="single" w:sz="6" w:color="000000"/>'
        f'<w:left w:val="single" w:sz="24" w:color="000000"/><w:bottom w:val="single" w:sz="6" w:color="000000"/>'
        f'<w:right w:val="single" w:sz="6" w:color="000000"/></w:tcBorders>'))
    p = c.paragraphs[0]
    p.paragraph_format.space_after = Pt(4)
    p.paragraph_format.keep_with_next = True
    rr = p.add_run(title); rr.bold = True; rr.font.size = Pt(12)
    for k, v in fields:
        p = c.add_paragraph()
        p.paragraph_format.space_after = Pt(2)
        p.paragraph_format.line_spacing = 1.15
        p.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
        a = p.add_run(f"{k}: "); a.bold = True; a.font.size = Pt(11)
        b = p.add_run(v); b.font.size = Pt(11)
    spacer()


PRI = {"P1": "P1 (Critic)", "P2": "P2 (Important)", "P3": "P3 (Opțional)"}

# =====================================================================
# COVER
# =====================================================================
p = doc.add_paragraph(); p.alignment = WD_ALIGN_PARAGRAPH.CENTER
p.paragraph_format.space_before = Pt(10)
for i, line in enumerate(["UNIVERSITATEA DIN CRAIOVA",
                          "FACULTATEA DE AUTOMATICĂ, CALCULATOARE ȘI ELECTRONICĂ",
                          "DEPARTAMENTUL DE CALCULATOARE ȘI TEHNOLOGIA INFORMAȚIEI"]):
    rr = p.add_run(line); rr.bold = True; rr.font.size = Pt(12)
    if i < 2:
        rr.add_break()

for _ in range(5):
    doc.add_paragraph()

p = doc.add_paragraph(); p.alignment = WD_ALIGN_PARAGRAPH.CENTER
p.paragraph_format.space_after = Pt(8)
rr = p.add_run("SPECIFICAȚIA CERINȚELOR SOFTWARE (SRS)"); rr.bold = True; rr.font.size = Pt(24)

p = doc.add_paragraph(); p.alignment = WD_ALIGN_PARAGRAPH.CENTER
p.paragraph_format.space_after = Pt(4)
rr = p.add_run("Aplicație Full Stack Tracking Nutrițional\n"
               "cu Extragere Automată a Valorilor Nutriționale prin OCR"); rr.font.size = Pt(14)
p = doc.add_paragraph(); p.alignment = WD_ALIGN_PARAGRAPH.CENTER
p.paragraph_format.space_after = Pt(28)
rr = p.add_run("Conform Standardului IEEE 830-1998 (Adaptat)"); rr.font.size = Pt(13); rr.italic = True

meta = [
    ("Tip Document:", "Software Requirements Specification (SRS) — Lucrare de Licență"),
    ("Domeniu de Specialitate:", "Calculatoare și Tehnologia Informației"),
    ("Autor:", "Maximilian Andrei"),
    ("Coordonator Științific:", "Dr. Ing. Mocanu Lucian-Mihai"),
    ("Titlul Temei:", "Aplicație Full Stack Tracking Nutrițional"),
    ("Arhitectură de Referință:", "Clean Architecture, CQRS (MediatR), React (TypeScript) PWA, ASP.NET Core 8 Web API, microserviciu OCR Python"),
    ("Versiune și Dată:", "Versiunea 1.2 — Octombrie 2026"),
]
t = doc.add_table(rows=len(meta), cols=2)
t.alignment = WD_TABLE_ALIGNMENT.CENTER
t.autofit = False
table_borders(t)
for j, w in enumerate((2.2, 4.07)):
    t.columns[j].width = Inches(w)
for i, (k, v) in enumerate(meta):
    c0, c1 = t.rows[i].cells
    c0.width, c1.width = Inches(2.2), Inches(4.07)
    cell_bg(c0); cell_bg(c1)
    cell_margins(c0, 90, 90, 130, 130); cell_margins(c1, 90, 90, 130, 130)
    write_cell(c0, k, bold=True); write_cell(c1, v)

doc.add_page_break()

# =====================================================================
# 1. INTRODUCERE
# =====================================================================
h1("1. INTRODUCERE")

h2("1.1 Scopul Documentului")
body("Prezentul document definește Specificația Cerințelor Software (SRS — Software Requirements Specification) pentru "
     "un sistem hibrid de monitorizare a nutriției, al cărui element diferențiator este extragerea automată, prin OCR, a "
     "valorilor nutriționale de pe eticheta unui produs fotografiat cu telefonul. Documentul este structurat conform "
     "recomandărilor standardului IEEE 830-1998 și servește drept referință unică pentru proiectarea, implementarea, "
     "testarea și evaluarea lucrării de licență.")

h2("1.2 Scopul Sistemului (Project Scope)")
body("Sistemul este o aplicație hibridă — platformă web și Progressive Web App (PWA) instalabilă pe telefon — susținută de "
     "un API ASP.NET Core, un microserviciu OCR în Python și o bază de date PostgreSQL. Scopul principal este reducerea "
     "efortului de jurnalizare nutrițională: în locul căutării și tastării manuale a fiecărui aliment, utilizatorul "
     "fotografiază tabelul nutrițional, verifică valorile extrase automat și le confirmă.")
body("Ecosistemul este completat de un jurnal alimentar cu statistici, un rețetar cu calcul nutrițional automat, un "
     "planificator săptămânal de mese și un generator de liste de cumpărături care agregă ingredientele din plan.")
bullet("scanarea OCR a etichetelor, jurnalul alimentar cu statistici, rețetarul cu calcul nutrițional, planificatorul de "
       "mese, generarea listei de cumpărături, gestiunea contului și a profilului, administrarea catalogului de alimente.",
       "În domeniu: ")
bullet("recomandări medicale, integrarea cu dispozitive purtabile (smartwatch), plăți online, aplicații native "
       "compilate pentru iOS/Android.", "În afara domeniului: ")

h2("1.3 Definiții, Acronime și Abrevieri")
grid_table(["Termen", "Semnificație"], [
    ("SRS", "Software Requirements Specification — specificația cerințelor software"),
    ("OCR", "Optical Character Recognition — recunoașterea optică a caracterelor"),
    ("PWA", "Progressive Web App — aplicație web instalabilă, cu Service Worker și manifest"),
    ("CQRS", "Command Query Responsibility Segregation — separarea operațiilor de scriere de cele de citire"),
    ("JWT", "JSON Web Token — token semnat pentru autentificare fără stare (stateless)"),
    ("EF Core", "Entity Framework Core — ORM-ul platformei .NET"),
    ("Macronutrienți", "Proteine, carbohidrați, grăsimi; exprimați în grame"),
    ("ROI", "Region of Interest — zona din imagine care conține tabelul nutrițional"),
    ("p95", "Percentila 95 — valoarea sub care se încadrează 95% din măsurători"),
    ("HitL", "Human-in-the-Loop — confirmarea explicită de către utilizator a unui rezultat automat"),
], (1.6, CONTENT_W - 1.6), bold_cols=(0,))

h2("1.4 Convenții Documentare și Sistem de Notare")
body("Pentru a asigura trasabilitatea cerințelor, sunt adoptate următoarele convenții de etichetare:")
bullet("Cerință Funcțională (Functional Requirement), numerotată crescător (ex: FR-01, FR-02).", "FR-xx: ")
bullet("Cerință Non-Funcțională (Non-Functional Requirement), numerotată crescător (ex: NFR-01, NFR-02).", "NFR-xx: ")
bullet("Caz de Utilizare (Use Case), descris operațional prin precondiții, flux principal și fluxuri alternative.", "UC-xx: ")
bullet("Cerință obligatorie, indispensabilă funcționării de bază a sistemului (MoSCoW: Must-Have).", "P1 (Critic): ")
bullet("Cerință importantă, cu valoare adăugată majoră (MoSCoW: Should-Have).", "P2 (Important): ")
bullet("Cerință opțională sau extensie de confort (MoSCoW: Could-Have).", "P3 (Opțional): ")

h2("1.5 Public Țintă și Ghid de Lectură")
body("Documentul se adresează comisiei de evaluare a lucrării de licență, coordonatorului științific, precum și celor care "
     "dezvoltă și testează sistemul. Secțiunile 2 și 3 oferă perspectiva generală și cazurile de utilizare, secțiunile 4 "
     "și 5 descriu cerințele funcționale și non-funcționale, secțiunea 6 analizează constrângerile și limitele sistemului, "
     "iar secțiunea 7 conține matricea de trasabilitate. Anexa A prezintă planul de proiectare și dezvoltare.")

# =====================================================================
# 2. DESCRIEREA GENERALĂ
# =====================================================================
doc.add_page_break()
h1("2. DESCRIEREA GENERALĂ A SISTEMULUI")

h2("2.1 Perspectiva Produsului și Arhitectura la Nivel Înalt")
body("Sistemul este compus din patru componente decuplate: clientul React PWA (TypeScript, Tailwind CSS, shadcn/ui), API-ul ASP.NET Core 8 (punct unic de "
     "intrare), microserviciul OCR Python și baza de date PostgreSQL. Ultimele trei rulează în containere Docker "
     "separate, orchestrate cu Docker Compose.")
body("Clientul nu apelează niciodată direct serviciul OCR sau baza de date; API-ul este singura componentă expusă public "
     "și aplică autentificarea, validările și regulile de business.")
grid_table(["Componentă", "Tehnologie", "Responsabilitate", "Expunere"], [
    ("Client", "React 18/19 (TypeScript, Vite, Tailwind CSS), PWA", "UI responsive mobil/desktop, captură foto cameră, validarea manuală a valorilor OCR, cache offline", "Browser utilizator"),
    ("API", "ASP.NET Core 8, MediatR, FluentValidation", "Autentificare JWT, logica de business, orchestrarea apelului OCR, persistență", "Public, port 443 (prin reverse proxy)"),
    ("Serviciu OCR", "Python 3.12, FastAPI, OpenCV, Tesseract / EasyOCR", "Preprocesare imagine, recunoaștere text, extragere structurată a valorilor", "Doar rețeaua internă Docker"),
    ("Bază de date", "PostgreSQL 16, EF Core + Npgsql", "Persistența entităților, integritate referențială", "Doar rețeaua internă Docker"),
], (1.1, 1.6, 2.2, 1.37), bold_cols=(0,))
body("Aplicația client React este configurată ca Progressive Web App (PWA) instalabilă atât pe telefoane (iOS/Android), "
     "cât și pe PC-uri desktop. Comunicarea cu API-ul ASP.NET Core se realizează prin apeluri REST HTTPS securizate prin token-uri JWT.")

h2("2.2 Arhitectura Tehnologică Detaliată")
body("API-ul este organizat conform Clean Architecture, cu dependențe orientate exclusiv spre interior (Dependency "
     "Inversion), iar cazurile de utilizare sunt exprimate după șablonul CQRS:")
bullet("entități (User, Food, DiaryEntry, Recipe, RecipeIngredient, MealPlan, MealPlanSlot, ShoppingList), obiecte-valoare "
       "(NutritionFacts, Quantity) și reguli de calcul; nu depinde de niciun alt strat.", "Domain: ")
bullet("comenzi (scriere, ex. CreateRecipeCommand) și interogări (citire, ex. GetWeeklySummaryQuery), fiecare cu "
       "handler MediatR; pipeline behaviors pentru validare (FluentValidation), logging și tranzacții. Definește "
       "interfețele IApplicationDbContext și IOcrClient.", "Application: ")
bullet("implementările concrete: DbContext EF Core, migrări, clientul HTTP tipizat către serviciul OCR (cu Polly), "
       "generarea token-urilor JWT, hashing-ul parolelor.", "Infrastructure: ")
bullet("endpoint-uri subțiri care traduc cererea HTTP într-o comandă sau interogare și o trimit prin IMediator.",
       "Presentation (WebApi): ")
bullet("FastAPI; lanț de preprocesare OpenCV (grayscale, CLAHE, corecție de perspectivă, binarizare adaptivă, eliminare "
       "zgomot), Tesseract ca motor implicit și EasyOCR ca fallback, parser bazat pe dicționar de sinonime și pe "
       "poziția bounding box-urilor.", "Microserviciul OCR: ")
body("Schimbarea motorului OCR sau a bazei de date nu afectează straturile Domain și Application. Interogările de "
     "raportare (agregări săptămânale/lunare) folosesc proiecții AsNoTracking() direct în DTO-uri, separat de modelul "
     "de scriere.")

h2("2.3 Motivarea Deciziilor Arhitecturale")
grid_table(["Decizie", "Alternativă respinsă", "Motivare (atribut de calitate)"], [
    ("OCR ca microserviciu Python separat", "Biblioteci OCR .NET în același proces",
     "Ecosistemul Python (OpenCV, EasyOCR) este matur pentru viziune artificială; procesarea CPU-intensivă este izolată și poate fi scalată independent — scalabilitate, izolarea defectelor"),
    ("API ca punct unic de intrare (gateway)", "Clientul apelează direct OCR-ul",
     "O singură suprafață de atac, autentificare centralizată, serviciul OCR rămâne stateless și neexpus — securitate"),
    ("Clean Architecture", "Arhitectură în straturi clasică cu logică în controllere",
     "Logica de domeniu testabilă unitar, independentă de framework și de baza de date — mentenabilitate, testabilitate"),
    ("CQRS cu MediatR", "Servicii generice CRUD",
     "Fiecare caz de utilizare are un handler mic și focalizat; citirile pentru statistici pot fi optimizate separat — separarea preocupărilor, performanță la citire"),
    ("React PWA (Vite + TypeScript + shadcn/ui)", "Aplicație nativă iOS (Swift) sau MAUI",
     "Rulare pe orice dispozitiv (telefon și PC) fără magazine de aplicații; ecosistem modern de componente UI responsive; viteză ridicată de dezvoltare și întreținere — portabilitate, cost de dezvoltare"),
    ("PostgreSQL + EF Core", "Bază NoSQL (MongoDB)",
     "Date puternic relaționale (rețetă → ingrediente → alimente → plan); tranzacții ACID și agregări SQL eficiente — integritate, consistență"),
    ("Docker Compose", "Instalare manuală pe gazdă",
     "Mediu reproductibil identic la dezvoltare și la demonstrația de licență; fiecare serviciu cu propriile dependențe — portabilitate, reproductibilitate"),
], (1.6, 1.6, 3.07), bold_cols=(0,))

h2("2.4 Clase de Utilizatori și Caracteristici")
body("Sistemul identifică trei categorii de utilizatori, cu responsabilități și privilegii distincte:")
grid_table(["Clasă Utilizator", "Descriere & Obiective", "Competență Tehnică", "Drepturi / Rol"], [
    ("Utilizator Neautentificat", "Vizitator al aplicației; poate crea un cont și se poate autentifica.", "Redusă / Medie", "Anonim (Guest)"),
    ("Utilizator Autentificat", "Persoană interesată de nutriție: scanează etichete, ține jurnalul, creează rețete, planifică mese și generează liste de cumpărături.", "Medie", "Rol: User (JWT)"),
    ("Administrator", "Gestionează catalogul global de alimente și monitorizează starea serviciilor și acuratețea OCR.", "Avansată", "Rol: Admin (JWT)"),
], (1.4, 2.6, 1.0, 1.27), bold_cols=(0,))

h2("2.5 Mediu de Operare")
bullet("Safari pe iOS 16.4+ (PWA instalată pe ecranul principal) și browsere desktop/Android actuale (Chrome, Edge, Firefox). Necesită cameră și conexiune la internet pentru scanare.", "Client: ")
bullet("Linux, Docker Engine + Docker Compose, gazdă unică (server sau VPS). Mediul de referință pentru măsurători: "
       "4 vCPU, 8 GB RAM, fără GPU.", "Server: ")
bullet("Rețea internă Docker pentru API ↔ OCR ↔ PostgreSQL; doar API-ul este expus, prin HTTPS.", "Rețea: ")

# =====================================================================
# 3. CAZURI DE UTILIZARE
# =====================================================================
doc.add_page_break()
h1("3. CAZURI DE UTILIZARE REPREZENTATIVE (USE CASES)")

h2("3.1 Inventarul Cazurilor de Utilizare")
body("Tabelul de mai jos enumeră cazurile de utilizare ale sistemului, actorul principal și cerințele funcționale pe "
     "care le acoperă:")
grid_table(["ID", "Caz de utilizare", "Actor principal", "Cerințe acoperite"], [
    ("UC01", "Scanare etichetă și extragere valori nutriționale prin OCR", "Utilizator autentificat", "FR-03, FR-04, FR-05, FR-06"),
    ("UC02", "Înregistrare și editare intrări în jurnalul alimentar", "Utilizator autentificat", "FR-07"),
    ("UC03", "Creare rețetă și calcul nutrițional agregat", "Utilizator autentificat", "FR-09"),
    ("UC04", "Planificare săptămânală a meselor", "Utilizator autentificat", "FR-10, FR-11"),
    ("UC05", "Generare listă consolidată de cumpărături", "Utilizator autentificat", "FR-12"),
    ("UC06", "Vizualizare statistici și progres față de target", "Utilizator autentificat", "FR-08"),
    ("UC07", "Creare cont, autentificare și configurare profil", "Utilizator neautentificat / autentificat", "FR-01, FR-02"),
    ("UC08", "Administrare catalog global de alimente", "Administrator", "FR-13"),
], (0.6, 2.67, 1.6, 1.4), bold_cols=(0,))

h2("3.2 Specificarea Cazurilor de Utilizare Cheie")

h3("Cazul de Utilizare UC01: Scanare Etichetă și Extragere Valori Nutriționale prin OCR")
kv_table([
    ("Identificator & Nume", "UC01: Scanare etichetă și extragere valori nutriționale prin OCR"),
    ("Actor Principal", "Utilizator autentificat"),
    ("Precondiții", "1. Utilizatorul este autentificat în PWA.\n2. Browserul are acces la cameră sau la galerie.\n3. Dispozitivul are conexiune la internet."),
    ("Postcondiții", "Alimentul, cu valorile confirmate de utilizator, este salvat în catalogul personal și, opțional, adăugat ca intrare în jurnal. Metadatele scanării sunt păstrate pentru statistici de acuratețe."),
    ("Declanșator", "Utilizatorul apasă „Scanează eticheta” din ecranul de adăugare aliment."),
    ("Flux Principal",
     "1. Utilizatorul fotografiază eticheta din PWA (<input type=\"file\" accept=\"image/*\" capture=\"environment\">).\n"
     "2. Clientul redimensionează imaginea la maximum 1600 px pe latura lungă și o comprimă JPEG.\n"
     "3. Clientul trimite POST /api/scans (multipart/form-data) cu token-ul JWT în antetul Authorization: Bearer.\n"
     "4. API-ul validează tipul și dimensiunea fișierului; comanda ScanNutritionLabelCommand apelează serviciul OCR prin HttpClient tipizat, cu timeout și politici de reîncercare (Polly).\n"
     "5. Serviciul OCR aplică preprocesarea (grayscale, corecție de perspectivă, binarizare adaptivă, eliminare zgomot) și rulează motorul OCR.\n"
     "6. Parserul extrage energia, proteinele, carbohidrații, grăsimile, baza de raportare (100 g / porție) și un scor de încredere per câmp, returnate ca JSON.\n"
     "7. API-ul returnează rezultatul clientului fără a crea o intrare de jurnal.\n"
     "8. Clientul afișează formularul editabil pre-populat; câmpurile cu încredere scăzută sunt evidențiate.\n"
     "9. Utilizatorul corectează și confirmă valorile; sistemul verifică coerența energetică.\n"
     "10. Clientul trimite POST /api/diary/entries, iar API-ul salvează alimentul și intrarea de jurnal în PostgreSQL."),
    ("Fluxuri Alternative & Excepții",
     "A1. Fișier invalid: la pasul 4, tipul nu este JPEG/PNG/HEIC sau depășește 10 MB → HTTP 400/413 și mesaj explicit.\n"
     "A2. Încredere scăzută: la pasul 6, un câmp are scorul sub prag sau nu este recunoscut → câmpul este marcat și trebuie confirmat explicit (FR-05).\n"
     "A3. Serviciul OCR indisponibil sau timeout: la pasul 4, circuit breaker-ul Polly se deschide → utilizatorul primește un mesaj clar și formularul gol pentru introducere manuală (NFR-12).\n"
     "A4. Incoerență energetică: la pasul 9, 4·P + 4·C + 9·G diferă cu peste 15% de energia declarată → avertisment, utilizatorul poate corecta sau confirma (FR-06)."),
    ("Reguli de Afaceri", "BR01: Niciun rezultat OCR nu este persistat ca aliment sau intrare de jurnal fără confirmarea explicită a utilizatorului (Human-in-the-Loop)."),
])

h3("Cazul de Utilizare UC03: Creare Rețetă și Calcul Nutrițional Agregat")
kv_table([
    ("Identificator & Nume", "UC03: Creare rețetă și calcul nutrițional agregat"),
    ("Actor Principal", "Utilizator autentificat"),
    ("Precondiții", "Alimentele folosite ca ingrediente există în catalog (global sau personal)."),
    ("Postcondiții", "Rețeta este salvată cu valorile nutriționale totale și per porție calculate."),
    ("Declanșator", "Utilizatorul apasă „Rețetă nouă” în secțiunea Rețetar."),
    ("Flux Principal",
     "1. Utilizatorul introduce numele rețetei și numărul de porții.\n"
     "2. Adaugă ingrediente din catalog, fiecare cu o cantitate în grame.\n"
     "3. Clientul afișează în timp real totalurile estimate.\n"
     "4. Utilizatorul adaugă opțional instrucțiuni și salvează.\n"
     "5. API-ul (CreateRecipeCommand) validează datele, calculează valorile totale și per porție și persistă rețeta."),
    ("Fluxuri Alternative & Excepții",
     "A1. Ingredient inexistent: utilizatorul îl poate crea prin scanare (UC01) sau manual și revine în rețetă.\n"
     "A2. Date invalide (cantitate ≤ 0, număr de porții ≤ 0): FluentValidation respinge cererea cu HTTP 400."),
    ("Criteriu de Acceptanță", "Dată o rețetă cu 200 g ovăz (389 kcal/100 g) și 4 porții, la salvare valoarea per porție este 194,5 kcal."),
])

h3("Cazul de Utilizare UC05: Generare Listă Consolidată de Cumpărături")
kv_table([
    ("Identificator & Nume", "UC05: Generare listă consolidată de cumpărături"),
    ("Actor Principal", "Utilizator autentificat"),
    ("Precondiții", "Utilizatorul are cel puțin o masă planificată în intervalul selectat."),
    ("Postcondiții", "O listă de cumpărături agregată este salvată și afișată, pregătită pentru bifare."),
    ("Declanșator", "Utilizatorul apasă „Generează lista de cumpărături” din Planificator."),
    ("Flux Principal",
     "1. Utilizatorul selectează intervalul de zile.\n"
     "2. Clientul trimite cererea către API, care execută comanda de generare a listei.\n"
     "3. Handler-ul citește planul mesei pentru utilizatorul curent și interval.\n"
     "4. Pentru fiecare rețetă, cantitățile ingredientelor sunt scalate la numărul de porții planificate.\n"
     "5. Ingredientele identice sunt agregate prin însumarea cantităților.\n"
     "6. Articolele sunt grupate pe categorii și returnate clientului.\n"
     "7. Utilizatorul bifează articolele cumpărate și poate adăuga articole manuale."),
    ("Fluxuri Alternative & Excepții",
     "A1. Nu există mese planificate în interval: sistemul afișează un mesaj și invită utilizatorul să planifice mese.\n"
     "A2. Unități incompatibile (ex: „2 bucăți” și „250 g”): se folosește tabelul de conversie predefinit; dacă nu există conversie, sub-cantitățile apar pe linii separate."),
    ("Criteriu de Acceptanță", "Dat un plan cu două rețete care conțin 150 g și 250 g de piept de pui, lista generată conține un singur articol „Piept de pui — 400 g”."),
])

# =====================================================================
# 4. CERINȚE FUNCȚIONALE
# =====================================================================
doc.add_page_break()
h1("4. CERINȚE FUNCȚIONALE (FUNCTIONAL REQUIREMENTS)")
body("Sistemul are 13 cerințe funcționale, grupate pe modulele principale. Fiecare cerință are o prioritate (P1–P3), "
     "modulul responsabil și, pentru fluxurile critice, un criteriu de acceptanță verificabil.")

def fr(id_, name, desc, pri, module, acc=None):
    fields = [("Descriere", desc), ("Prioritate", f"{PRI[pri]} | Modul: {module}")]
    if acc:
        fields.append(("Criteriu de acceptanță", acc))
    card(f"[{id_}] {name}", fields)

h2("4.1 Gestiunea Contului și a Profilului")
fr("FR-01", "Înregistrare, Autentificare și Deconectare",
   "Sistemul trebuie să permită înregistrarea cu e-mail și parolă, autentificarea (emiterea unui JWT de acces și a unui "
   "refresh token) și deconectarea.", "P1", "API (Identity, JWT) & Client")
fr("FR-02", "Profil și Calculul Targetului Nutrițional",
   "Sistemul trebuie să permită definirea profilului (vârstă, sex, înălțime, greutate, nivel de activitate, obiectiv) și "
   "să calculeze targetul caloric zilnic (ecuația Mifflin–St Jeor × factor de activitate) și distribuția de "
   "macronutrienți, cu posibilitate de suprascriere manuală.", "P1", "API (Domain) & Client")

h2("4.2 Modulul de Scanare OCR")
fr("FR-03", "Captura Fotografiei Etichetei",
   "Sistemul trebuie să permită capturarea unei fotografii a etichetei nutriționale din PWA (camera dispozitivului sau "
   "galerie) și trimiterea ei către API.", "P1", "Client PWA & API")
fr("FR-04", "Extragerea Structurată a Valorilor Nutriționale",
   "Serviciul OCR trebuie să extragă din imagine: valoarea energetică (kcal; kJ convertit dacă lipsește kcal), proteine, "
   "carbohidrați, grăsimi (g) și baza de raportare (per 100 g/100 ml sau per porție), împreună cu un scor de încredere "
   "per câmp.", "P1", "Microserviciu OCR (Python)")
fr("FR-05", "Validare Manuală a Rezultatului (Human-in-the-Loop)",
   "Sistemul trebuie să afișeze valorile extrase într-un formular editabil, să evidențieze câmpurile cu încredere sub "
   "prag sau nerecunoscute și să salveze alimentul doar după confirmarea explicită a utilizatorului.", "P1", "Client PWA & API",
   "Dat fiind un rezultat OCR în care câmpul „proteine” are încrederea 0,42, când formularul se afișează, câmpul este "
   "marcat vizual, iar butonul de salvare cere confirmarea acelui câmp.")
fr("FR-06", "Verificarea Coerenței Energetice",
   "Sistemul trebuie să verifice coerența valorilor confirmate (energia calculată 4·P + 4·C + 9·G să nu difere cu mai "
   "mult de 15% de valoarea declarată) și să avertizeze utilizatorul în caz contrar.", "P2", "API (Domain) & Client")

h2("4.3 Jurnal Alimentar și Statistici")
fr("FR-07", "Gestiunea Intrărilor din Jurnal",
   "Sistemul trebuie să permită adăugarea, editarea și ștergerea intrărilor în jurnal (aliment sau rețetă, cantitate în "
   "grame sau porții, dată, masă: mic dejun / prânz / cină / gustare), din scanare, din căutare în catalog sau din "
   "istoric.", "P1", "API & Client")
fr("FR-08", "Totaluri Zilnice și Statistici Agregate",
   "Sistemul trebuie să afișeze totalurile zilnice de calorii și macronutrienți raportate la target, precum și statistici "
   "agregate săptămânal și lunar (medii, abateri față de target) sub formă de grafice.", "P1", "API (Queries) & Client")

h2("4.4 Rețetar și Planificator de Mese")
fr("FR-09", "Creare Rețete și Calcul Nutrițional Automat",
   "Sistemul trebuie să permită crearea și editarea rețetelor (ingrediente cu cantități, număr de porții, instrucțiuni) "
   "și să calculeze automat valorile nutriționale totale și per porție la fiecare modificare.", "P1", "API (Domain) & Client",
   "Dată o rețetă cu 200 g ovăz (389 kcal/100 g) și 4 porții, când se salvează, valoarea per porție este 194,5 kcal.")
fr("FR-10", "Planificator Săptămânal de Mese",
   "Sistemul trebuie să permită asignarea rețetelor sau alimentelor pe zile și mese ale unei săptămâni și să afișeze, "
   "pentru fiecare zi, totalul planificat comparativ cu targetul caloric (sub / în interval ±5% / peste).", "P1", "API & Client")
fr("FR-11", "Transferul unei Mese Planificate în Jurnal",
   "Sistemul trebuie să permită transferul unei mese planificate în jurnal cu o singură acțiune („Am mâncat asta”).",
   "P2", "API & Client")

h2("4.5 Lista de Cumpărături")
fr("FR-12", "Generarea Listei de Cumpărături Agregate",
   "Sistemul trebuie să genereze lista de cumpărături pentru un interval de zile din plan, agregând ingredientele "
   "identice (însumarea cantităților scalate la numărul de porții planificate), grupate pe categorii, cu posibilitatea "
   "de a bifa articolele și de a adăuga articole manuale.", "P1", "API (Application) & Client",
   "Dat un plan cu două rețete care conțin 150 g și 250 g de piept de pui, când se generează lista, apare un singur "
   "articol „Piept de pui — 400 g”.")

h2("4.6 Administrare")
fr("FR-13", "Gestiunea Catalogului Global de Alimente",
   "Administratorul trebuie să poată gestiona catalogul global de alimente (adăugare, corectare, marcare ca verificat).",
   "P3", "API & Client (rol Admin)")

# =====================================================================
# 5. CERINȚE NON-FUNCȚIONALE
# =====================================================================
doc.add_page_break()
h1("5. CERINȚE NON-FUNCȚIONALE (NON-FUNCTIONAL REQUIREMENTS)")
body("Fiecare cerință non-funcțională are o metrică și o metodă de verificare, astfel încât să poată fi testată obiectiv "
     "în capitolul de evaluare al lucrării. Măsurătorile de performanță se fac pe mediul de referință: un server cu "
     "4 vCPU și 8 GB RAM, fără GPU, rulând Docker Compose.")

def nfr(id_, name, desc, metric, pri, cat):
    card(f"[{id_}] {name}", [("Descriere", desc), ("Metrică / verificare", metric),
                             ("Prioritate", f"{PRI[pri]} | Categorie: {cat}")])

h2("5.1 Performanță")
nfr("NFR-01", "Timp de Procesare OCR pe Server",
    "Procesarea OCR a unei imagini de maximum 1600 px pe latura lungă trebuie să se încheie rapid pe server.",
    "p95 ≤ 3 s (Tesseract), p95 ≤ 6 s (EasyOCR pe CPU); măsurat cu 100 de imagini de test și Locust.", "P1", "Performanță")
nfr("NFR-02", "Timp Total Captură → Formular de Validare",
    "Timpul total „captură → formular de validare” perceput de utilizator pe iPhone, în rețea 4G.",
    "p95 ≤ 8 s, incluzând compresia în client și upload-ul.", "P2", "Performanță")
nfr("NFR-03", "Latența Endpoint-urilor CRUD și de Statistici",
    "Endpoint-urile CRUD și de statistici (fără OCR) trebuie să răspundă rapid sub încărcare.",
    "p95 ≤ 300 ms la 50 de utilizatori concurenți; statistica lunară ≤ 500 ms.", "P1", "Performanță")
nfr("NFR-04", "Încărcarea Inițială a PWA-ului",
    "Descărcarea runtime-ului WASM la prima vizită și pornirea din cache la vizitele ulterioare.",
    "≤ 5 s la prima vizită pe 4G cu compresie Brotli și trimming IL; ≤ 1,5 s la vizitele ulterioare din cache-ul Service Worker.",
    "P2", "Performanță")

h2("5.2 Acuratețea Recunoașterii")
nfr("NFR-05", "Acuratețea Extragerii OCR",
    "Extragerea corectă a celor patru valori principale din etichete fotografiate în condiții normale (iluminare "
    "uniformă, etichetă plană, unghi ≤ 20°).",
    "≥ 85% câmpuri corecte pe un set de ≥ 100 de etichete de pe piața din România.", "P1", "Acuratețe")

h2("5.3 Securitate")
nfr("NFR-06", "Autentificare JWT cu Refresh Token",
    "Autentificarea folosește JWT semnat HMAC-SHA256 (sau RS256), cu token de acces de 15 min și refresh token rotativ "
    "de 7 zile, stocat hashed în baza de date și revocabil.",
    "Teste de integrare: token expirat / alterat → HTTP 401.", "P1", "Securitate")
nfr("NFR-07", "Stocarea Securizată a Parolelor",
    "Parolele se stochează exclusiv ca hash cu sare, folosind PBKDF2 (ASP.NET Core Identity PasswordHasher, ≥ 100.000 "
    "iterații) sau Argon2id; politică minimă de 8 caractere.",
    "Inspecția tabelei de utilizatori: nicio parolă în clar.", "P1", "Securitate")
nfr("NFR-08", "Prevenirea SQL Injection",
    "Accesul la date se face exclusiv prin LINQ / interogări parametrizate EF Core; este interzisă concatenarea de SQL brut.",
    "Analiză statică (SonarLint) și teste cu payload-uri OWASP.", "P1", "Securitate")
nfr("NFR-09", "Securitatea Transportului și a Fișierelor Încărcate",
    "Tot traficul extern se face prin HTTPS (TLS 1.2+), cu HSTS; serviciul OCR și PostgreSQL nu expun porturi în afara "
    "rețelei Docker; fișierele încărcate sunt limitate la 10 MB și la tipurile JPEG/PNG/HEIC, cu verificarea semnăturii fișierului.",
    "Scanare de porturi; teste cu fișiere invalide → HTTP 400/413.", "P1", "Securitate")
nfr("NFR-10", "Autorizare la Nivel de Resursă",
    "Un utilizator poate citi sau modifica doar propriile intrări, rețete și planuri.",
    "Teste de integrare: acces la ID-ul resursei altui utilizator → HTTP 404.", "P1", "Securitate")

h2("5.4 Disponibilitate și Fiabilitate")
nfr("NFR-11", "Disponibilitatea API-ului",
    "Disponibilitatea serviciului API în perioada de evaluare.",
    "≥ 99% lunar; containere cu restart: unless-stopped și health check-uri.", "P2", "Disponibilitate")
nfr("NFR-12", "Degradare Controlată la Indisponibilitatea OCR",
    "Indisponibilitatea serviciului OCR nu afectează restul funcționalităților; utilizatorul primește un mesaj clar și "
    "poate introduce valorile manual.",
    "Test: oprirea containerului OCR → jurnalul rămâne funcțional; circuit breaker Polly.", "P1", "Fiabilitate")
nfr("NFR-13", "Copii de Siguranță ale Bazei de Date",
    "Baza de date este salvată periodic, iar restaurarea este verificată.",
    "pg_dump zilnic, retenție 7 zile; restaurare verificată cel puțin o dată.", "P2", "Fiabilitate")

h2("5.5 Portabilitate și Utilizabilitate")
nfr("NFR-14", "Rulare și Instalare PWA pe iOS și Desktop",
    "PWA-ul rulează și se instalează pe ecranul principal („Add to Home Screen”) în Safari pe iOS 16.4+ și în "
    "Chrome/Edge/Firefox desktop, versiunile curente.",
    "Testare manuală pe iPhone fizic și BrowserStack.", "P1", "Portabilitate")
nfr("NFR-15", "Interfață Responsive și Accesibilă",
    "Interfața este responsive de la 360 px la 1920 px lățime, fără scroll orizontal, cu ținte tactile ≥ 44 × 44 pt (Apple HIG).",
    "Lighthouse: scor PWA valid, Accessibility ≥ 90.", "P2", "Utilizabilitate")
nfr("NFR-16", "Pornire cu o Singură Comandă",
    "Sistemul server pornește complet cu o singură comandă (docker compose up) pe Linux, Windows și macOS.",
    "Test pe două sisteme de operare diferite.", "P2", "Portabilitate")

h2("5.6 Mentenabilitate")
nfr("NFR-17", "Acoperire cu Teste Unitare",
    "Straturile Domain și Application sunt acoperite de teste unitare rulate automat.",
    "≥ 70% acoperire (Coverlet); pipeline CI care rulează testele la fiecare push.", "P2", "Mentenabilitate")

# =====================================================================
# 6. LIMITE, CONSTRÂNGERI, ASUMPȚII
# =====================================================================
doc.add_page_break()
h1("6. LIMITELE SISTEMULUI, CONSTRÂNGERI ȘI ASUMPȚII")
body("Cea mai mare limită a sistemului este fiabilitatea OCR pe fotografii reale; de aceea validarea manuală (FR-05) este "
     "o componentă de proiectare, nu un detaliu de interfață. Celelalte limite țin de latența procesării pe CPU și de "
     "restricțiile impuse de Apple aplicațiilor web pe iOS.")

h2("6.1 Constrângeri de Proiectare Impuse")
bullet("ASP.NET Core 8, React (TypeScript) PWA, PostgreSQL, Python (FastAPI) cu OpenCV și Tesseract/EasyOCR, Docker Compose.",
       "Stivă tehnologică fixată: ")
bullet("nu este disponibil un mediu macOS/Xcode; distribuția pe iOS se face exclusiv ca PWA, nu prin App Store.",
       "Distribuție iOS: ")
bullet("gazdă unică (server sau VPS), fără GPU și fără orchestrator (Kubernetes).", "Infrastructură: ")
bullet("datele personale (greutate, obiective, jurnal) intră sub incidența GDPR: minimizarea datelor, dreptul la ștergerea "
       "contului și exportul datelor.", "Conformitate: ")

h2("6.2 Limite ale Modulului OCR")
grid_table(["Limită", "Cauză", "Efect", "Măsură de atenuare"], [
    ("Calitatea imaginii de pe iPhone", "Reflexii pe ambalaje lucioase, blur de mișcare, umbre, etichete curbate pe sticle/conserve",
     "Cifre confundate (8↔3, 0↔O, 1↔l), virgula zecimală pierdută: „2,5” citit „25”",
     "Preprocesare OpenCV (CLAHE, binarizare adaptivă, corecție de perspectivă), indicații de încadrare în UI, validare manuală și verificarea coerenței energetice (FR-06)"),
    ("Format neuniform al etichetelor", "Regulamentul UE 1169/2011 impune conținutul, nu și aspectul; tabele orizontale/verticale, multilingve, coloane „per 100 g” și „per porție” alăturate",
     "Valori atribuite greșit coloanei sau nutrientului; „din care zaharuri” confundat cu carbohidrații totali",
     "Parser cu dicționar de sinonime multilingv (RO/EN/HU/DE), ancorare pe cuvinte-cheie și pe poziția bounding box-urilor, preferință explicită pentru coloana per 100 g"),
    ("Latența procesării pe CPU", "EasyOCR (rețele CRAFT + CRNN) este de 2–5× mai lent decât Tesseract fără GPU",
     "Timpi de răspuns de ordinul secundelor (NFR-01), coadă la cereri simultane",
     "Tesseract ca motor implicit, EasyOCR ca fallback la încredere scăzută; modele încărcate o singură dată la pornire; compresie în client; replicarea containerului"),
    ("Fonturi și diacritice", "Fonturi condensate, dimensiuni mici (≥ 1,2 mm cerut de lege), diacritice românești",
     "Rată de eroare mai mare pe câmpurile text",
     "Pachetul de limbă ron pentru Tesseract; extragerea se concentrează pe cifre, mai robuste decât textul"),
    ("Lipsa unui set de date standard", "Nu există un set public de etichete românești adnotate",
     "Acuratețea raportată depinde de setul propriu de test",
     "Construirea unui set propriu de ≥ 100 de imagini adnotate manual și raportarea metodologiei"),
], (1.2, 1.6, 1.45, 2.02), size=10, bold_cols=(0,))

h2("6.3 Limite ale PWA față de o Aplicație Nativă pe iOS")
grid_table(["Aspect", "PWA în Safari (iOS)", "Aplicație nativă"], [
    ("Acces la cameră", "Doar prin <input capture> sau getUserMedia; fără control asupra focalizării, expunerii sau blițului", "Control complet prin AVFoundation"),
    ("Procesare pe dispozitiv", "Fără acces la Apple Vision / Core ML; OCR-ul se face obligatoriu pe server, deci necesită rețea", "OCR local, offline, prin Vision framework"),
    ("Stocare locală", "Safari poate șterge datele unui site nefolosit de 7 zile (ITP); cotă limitată", "Stocare persistentă"),
    ("Notificări push", "Disponibile doar din iOS 16.4 și doar pentru PWA instalată pe ecranul principal", "Disponibile complet"),
    ("Sincronizare în fundal", "Background Sync și Periodic Sync nu sunt suportate", "Suportate"),
    ("Instalare", "Manuală, din meniul Share → „Add to Home Screen”; fără prompt automat", "Din App Store"),
    ("Dimensiune și pornire", "Runtime-ul .NET WASM (~2–5 MB comprimat) încetinește prima încărcare", "Pornire nativă rapidă"),
], (1.5, 2.9, 1.87), size=10, bold_cols=(0,))
body("Pentru scopul lucrării, aceste limite sunt acceptabile: funcționalitatea esențială (captură foto, trimitere, "
     "validare) este pe deplin suportată de Safari, iar câștigul în portabilitate și cost de dezvoltare justifică alegerea.")

h2("6.4 Alte Limite și Asumpții")
bullet("alimentele neambalate (fructe, legume) nu au etichetă; sistemul se bazează pe un catalog inițial importat (de "
       "exemplu, un subset din USDA FoodData Central sau Open Food Facts).", "Catalog inițial: ")
bullet("conversia între unități în lista de cumpărături („1 cană”, „2 bucăți” → grame) este aproximativă și limitată "
       "la un tabel de conversie predefinit.", "Conversia unităților: ")
bullet("sistemul nu oferă recomandări medicale; targeturile calculate sunt estimări statistice.", "Caracter informativ: ")
bullet("imaginile trimise pentru OCR sunt procesate în memorie și nu sunt stocate; se persistă doar valorile confirmate "
       "și metadatele scanării, conform principiului minimizării datelor.", "Retenția imaginilor: ")
bullet("utilizatorul dispune de un telefon cu cameră cu autofocus și de o conexiune la internet în momentul scanării.",
       "Hardware și conectivitate: ")

# =====================================================================
# 7. TRASABILITATE
# =====================================================================
doc.add_page_break()
h1("7. MATRICE DE TRASABILITATE ȘI REZUMATUL CERINȚELOR")
body("Tabelul de mai jos centralizează toate cerințele funcționale și non-funcționale, cu subsistemul responsabil, "
     "prioritatea, cazul de utilizare asociat și criteriul de verificare:")
grid_table(["ID", "Denumire cerință", "Subsistem", "Prior.", "UC", "Criteriu de verificare"], [
    ("FR-01", "Înregistrare, autentificare, deconectare", "API / Client", "P1", "UC07", "JWT + refresh token emise; logout invalidează token-ul"),
    ("FR-02", "Profil și target nutrițional", "API / Client", "P1", "UC07", "Target Mifflin–St Jeor calculat; suprascriere manuală"),
    ("FR-03", "Captura fotografiei etichetei", "Client PWA", "P1", "UC01", "Imagine trimisă la POST /api/scans"),
    ("FR-04", "Extragere structurată OCR", "Serviciu OCR", "P1", "UC01", "JSON cu 4 valori, bază de raportare, scor per câmp"),
    ("FR-05", "Validare manuală (HitL)", "Client / API", "P1", "UC01", "Câmpuri sub prag marcate; salvare doar la confirmare"),
    ("FR-06", "Coerență energetică", "API / Client", "P2", "UC01", "Avertisment la abatere > 15%"),
    ("FR-07", "Gestiunea intrărilor din jurnal", "API / Client", "P1", "UC02", "Adăugare, editare, ștergere pe cele 4 mese"),
    ("FR-08", "Totaluri și statistici", "API / Client", "P1", "UC06", "Totaluri zilnice; grafice săptămânale/lunare"),
    ("FR-09", "Rețete și calcul nutrițional", "API / Client", "P1", "UC03", "200 g ovăz, 4 porții → 194,5 kcal/porție"),
    ("FR-10", "Planificator săptămânal", "API / Client", "P1", "UC04", "Total zilnic vs. target (sub / ±5% / peste)"),
    ("FR-11", "Transfer masă planificată în jurnal", "API / Client", "P2", "UC04", "O singură acțiune creează intrarea"),
    ("FR-12", "Listă de cumpărături agregată", "API / Client", "P1", "UC05", "150 g + 250 g pui → un articol de 400 g"),
    ("FR-13", "Catalog global de alimente", "API / Admin", "P3", "UC08", "CRUD și marcare „verificat” de către Admin"),
    ("NFR-01", "Timp procesare OCR", "Serviciu OCR", "P1", "UC01", "p95 ≤ 3 s / ≤ 6 s; Locust, 100 imagini"),
    ("NFR-02", "Captură → formular", "Client / OCR", "P2", "UC01", "p95 ≤ 8 s pe 4G"),
    ("NFR-03", "Latență CRUD / statistici", "API", "P1", "—", "p95 ≤ 300 ms la 50 utilizatori; lunar ≤ 500 ms"),
    ("NFR-04", "Încărcare inițială PWA", "Client PWA", "P2", "—", "≤ 5 s prima vizită; ≤ 1,5 s din cache"),
    ("NFR-05", "Acuratețe OCR", "Serviciu OCR", "P1", "UC01", "≥ 85% câmpuri corecte pe ≥ 100 etichete"),
    ("NFR-06", "JWT + refresh token", "API", "P1", "UC07", "Token expirat / alterat → HTTP 401"),
    ("NFR-07", "Hashing parole", "API", "P1", "UC07", "Nicio parolă în clar în baza de date"),
    ("NFR-08", "Prevenire SQL Injection", "API", "P1", "—", "SonarLint + payload-uri OWASP"),
    ("NFR-09", "HTTPS și fișiere încărcate", "Infrastructură", "P1", "UC01", "Porturi interne închise; fișier invalid → 400/413"),
    ("NFR-10", "Autorizare pe resursă", "API", "P1", "—", "Acces la resursa altui utilizator → HTTP 404"),
    ("NFR-11", "Disponibilitate API", "Infrastructură", "P2", "—", "≥ 99% lunar; health check-uri"),
    ("NFR-12", "Degradare controlată OCR", "API / Client", "P1", "UC01", "Container OCR oprit → jurnal funcțional"),
    ("NFR-13", "Backup bază de date", "Infrastructură", "P2", "—", "pg_dump zilnic, restaurare verificată"),
    ("NFR-14", "PWA pe iOS și desktop", "Client PWA", "P1", "—", "Instalare verificată pe iPhone fizic"),
    ("NFR-15", "Responsive și accesibil", "Client PWA", "P2", "—", "Lighthouse Accessibility ≥ 90"),
    ("NFR-16", "Pornire cu o comandă", "Infrastructură", "P2", "—", "docker compose up pe 2 SO diferite"),
    ("NFR-17", "Acoperire teste unitare", "API", "P2", "—", "≥ 70% Domain + Application, CI la push"),
], (0.65, 1.55, 1.0, 0.5, 0.55, 2.02), size=9.5, bold_cols=(0, 3))

h2("7.1 Concluzii și Pași Următori de Dezvoltare")
body("Specificația reflectă obiectivele de inginerie ale lucrării de licență și stabilește, pentru fiecare cerință, un "
     "criteriu de verificare obiectiv. Etapa următoare constă în elaborarea diagramelor UML și a modelului "
     "entitate-relație, urmată de implementarea verticală a primului flux funcțional complet (înregistrare utilizator → "
     "captură foto → procesare OCR → validare → intrare în jurnal), conform planului din Anexa A.")

# =====================================================================
# ANEXA A
# =====================================================================
doc.add_page_break()
h1("ANEXA A. PLAN DE PROIECTARE ȘI DEZVOLTARE")
body("Dezvoltarea se împarte în cinci faze pe 20 de săptămâni, cu un efort total estimat de aproximativ 440 de ore "
     "(circa 22 h/săptămână). Fazele 2 și 3 se suprapun parțial, deoarece serviciul OCR este decuplat și poate fi "
     "dezvoltat pe baza unui contract de API stabilit în Faza 1. Fiecare fază se încheie cu un reper verificabil (M1–M5).")

h2("A.1 Faze, Activități și Repere")
grid_table(["Fază", "Săpt.", "Efort", "Activități principale", "Reper / livrabil"], [
    ("F1 · Analiză, arhitectură și bază de date", "1–2", "~40 h",
     "Finalizarea SRS; diagrame UML (cazuri de utilizare, componente, secvență pentru fluxul OCR); modelul entitate-relație; contractul OpenAPI între API și OCR; schelet Clean Architecture; docker-compose.yml inițial; repository Git + CI",
     "M1: schema DB migrată cu EF Core, toate containerele pornesc"),
    ("F2 · Backend API", "3–7", "~90 h",
     "Autentificare JWT + refresh token; comenzi și interogări MediatR pentru jurnal, rețete, planificator, listă de cumpărături; FluentValidation; agregări statistice; client OCR cu Polly; teste unitare și de integrare (xUnit, Testcontainers)",
     "M2: API complet documentat în Swagger, acoperire ≥ 70%"),
    ("F3 · Modul OCR Python", "6–10", "~90 h",
     "Colectarea și adnotarea setului de ≥ 100 de etichete; pipeline OpenCV; integrare Tesseract și EasyOCR; parser pentru nutrienți și coloane; scor de încredere; endpoint FastAPI; teste pytest; măsurători de acuratețe și latență",
     "M3: raport de evaluare OCR (acuratețe per câmp, timpi p95)"),
    ("F4 · Interfața React PWA", "9–14", "~100 h",
     "Layout responsive (mobil/desktop); ecrane pentru autentificare, profil, jurnal, scanare OCR cu vizor și validare HitL, rețetar, planificator, listă de cumpărături; grafice; manifest PWA și Service Worker; testare pe telefon și PC",
     "M4: MVP integrat, instalabil pe telefon, toate FR P1 funcționale"),
    ("F5 · Integrare, testare și documentație", "14–20", "~120 h",
     "Teste end-to-end (Playwright); teste de încărcare (Locust/k6) pentru NFR-01–03; verificări de securitate (OWASP ZAP); corectarea defectelor; redactarea lucrării; pregătirea demonstrației",
     "M5: aplicație finală + lucrare de licență predată"),
], (1.25, 0.5, 0.55, 2.47, 1.5), size=9.5, bold_cols=(0,))

h2("A.2 Riscuri Principale")
grid_table(["Risc", "Probabilitate", "Impact", "Răspuns"], [
    ("Acuratețea OCR sub țintă (NFR-05)", "Ridicată", "Ridicat", "Validarea manuală păstrează fluxul utilizabil; se raportează onest rezultatele și se compară cele două motoare OCR"),
    ("Particularități Safari/iOS descoperite târziu", "Medie", "Mediu", "Testare pe iPhone fizic încă din F4, nu doar în F5"),
    ("Depășirea estimărilor pentru F4 (UI)", "Medie", "Mediu", "Cerințele P2 și P3 (FR-06, FR-11, FR-13) pot fi amânate fără a afecta MVP-ul"),
    ("Dimensiunea bundle-ului frontend", "Scăzută", "Scăzut", "Code-splitting prin Vite, lazy-loading pe rute și componente dinamice"),
], (1.8, 1.0, 0.8, 2.67), size=10, bold_cols=(0,))

doc.save(OUT)
print("saved", OUT)
