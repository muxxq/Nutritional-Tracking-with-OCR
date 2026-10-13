import os
import docx
from docx import Document
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_ALIGN_VERTICAL
from docx.oxml import OxmlElement, parse_xml
from docx.oxml.ns import nsdecls, qn

def create_black_and_white_srs():
    doc = Document()
    
    # Configure Page Setup - A4, standard academic margins (2.5 cm all around = 1 inch)
    for section in doc.sections:
        section.page_width = Inches(8.27)   # A4 width
        section.page_height = Inches(11.69) # A4 height
        section.top_margin = Inches(1.0)
        section.bottom_margin = Inches(1.0)
        section.left_margin = Inches(1.0)
        section.right_margin = Inches(1.0)
        
        # Configure Header & Footer - Strictly Black Text
        header = section.header
        hp = header.paragraphs[0]
        hp.alignment = WD_ALIGN_PARAGRAPH.RIGHT
        hrun = hp.add_run("Specificația Cerințelor Software (SRS) | Standard IEEE 830")
        hrun.font.name = "Calibri"
        hrun.font.size = Pt(10)
        hrun.font.color.rgb = RGBColor(0x00, 0x00, 0x00)
        
        footer = section.footer
        fp = footer.paragraphs[0]
        fp.alignment = WD_ALIGN_PARAGRAPH.CENTER
        frun = fp.add_run("Lucrare de Licență - Facultatea de Automatică, Calculatoare și Electronică | Document Tehnic")
        frun.font.name = "Calibri"
        frun.font.size = Pt(10)
        frun.font.color.rgb = RGBColor(0x00, 0x00, 0x00)

    # Strictly Black Color
    COLOR_BLACK = RGBColor(0x00, 0x00, 0x00)
    FONT_FAMILY = "Calibri"

    def set_cell_background(cell, fill_hex="FFFFFF"):
        tcPr = cell._element.get_or_add_tcPr()
        shd = parse_xml(f'<w:shd {nsdecls("w")} w:fill="{fill_hex}"/>')
        tcPr.append(shd)

    def set_cell_margins(cell, top=140, bottom=140, left=180, right=180):
        tcPr = cell._element.get_or_add_tcPr()
        tcMar = parse_xml(f'''
            <w:tcMar {nsdecls("w")}>
                <w:top w:w="{top}" w:type="dxa"/>
                <w:bottom w:w="{bottom}" w:type="dxa"/>
                <w:left w:w="{left}" w:type="dxa"/>
                <w:right w:w="{right}" w:type="dxa"/>
            </w:tcMar>
        ''')
        tcPr.append(tcMar)

    def set_table_borders(table, color="000000", sz="6", val="single"):
        tblPr = table._element.xpath('w:tblPr')
        if tblPr:
            borders = parse_xml(f'''
                <w:tblBorders {nsdecls("w")}>
                    <w:top w:val="{val}" w:sz="{sz}" w:space="0" w:color="{color}"/>
                    <w:bottom w:val="{val}" w:sz="{sz}" w:space="0" w:color="{color}"/>
                    <w:left w:val="{val}" w:sz="{sz}" w:space="0" w:color="{color}"/>
                    <w:right w:val="{val}" w:sz="{sz}" w:space="0" w:color="{color}"/>
                    <w:insideH w:val="{val}" w:sz="{sz}" w:space="0" w:color="{color}"/>
                    <w:insideV w:val="{val}" w:sz="{sz}" w:space="0" w:color="{color}"/>
                </w:tblBorders>
            ''')
            tblPr[0].append(borders)

    def add_callout(text, title=""):
        tbl = doc.add_table(rows=1, cols=1)
        tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
        tbl.autofit = False
        tbl.columns[0].width = Inches(6.27)
        cell = tbl.cell(0, 0)
        set_cell_background(cell, "FFFFFF")
        set_cell_margins(cell, top=140, bottom=140, left=200, right=200)
        
        tcPr = cell._element.get_or_add_tcPr()
        borders = parse_xml(f'''
            <w:tcBorders {nsdecls("w")}>
                <w:top w:val="single" w:sz="6" w:color="000000"/>
                <w:left w:val="single" w:sz="24" w:color="000000"/>
                <w:bottom w:val="single" w:sz="6" w:color="000000"/>
                <w:right w:val="single" w:sz="6" w:color="000000"/>
            </w:tcBorders>
        ''')
        tcPr.append(borders)
        
        p = cell.paragraphs[0]
        p.paragraph_format.space_before = Pt(3)
        p.paragraph_format.space_after = Pt(3)
        p.paragraph_format.line_spacing = 1.2
        if title:
            rt = p.add_run(f"{title}\n")
            rt.bold = True
            rt.font.name = FONT_FAMILY
            rt.font.size = Pt(12)
            rt.font.color.rgb = COLOR_BLACK
        r = p.add_run(text)
        r.font.name = FONT_FAMILY
        r.font.size = Pt(11.5)
        r.font.color.rgb = COLOR_BLACK

    def add_code_block(code_text):
        tbl = doc.add_table(rows=1, cols=1)
        tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
        tbl.autofit = False
        tbl.columns[0].width = Inches(6.27)
        cell = tbl.cell(0, 0)
        set_cell_background(cell, "FFFFFF")
        set_cell_margins(cell, top=140, bottom=140, left=160, right=160)
        
        tcPr = cell._element.get_or_add_tcPr()
        borders = parse_xml(f'''
            <w:tcBorders {nsdecls("w")}>
                <w:top w:val="single" w:sz="8" w:color="000000"/>
                <w:left w:val="single" w:sz="8" w:color="000000"/>
                <w:bottom w:val="single" w:sz="8" w:color="000000"/>
                <w:right w:val="single" w:sz="8" w:color="000000"/>
            </w:tcBorders>
        ''')
        tcPr.append(borders)
        
        p = cell.paragraphs[0]
        p.paragraph_format.space_before = Pt(2)
        p.paragraph_format.space_after = Pt(2)
        p.paragraph_format.line_spacing = 1.05
        r = p.add_run(code_text)
        r.font.name = "Consolas"
        r.font.size = Pt(9.5)
        r.font.color.rgb = COLOR_BLACK

    def add_h1(text):
        p = doc.add_paragraph()
        p.paragraph_format.space_before = Pt(20)
        p.paragraph_format.space_after = Pt(8)
        p.paragraph_format.keep_with_next = True
        run = p.add_run(text)
        run.bold = True
        run.font.name = FONT_FAMILY
        run.font.size = Pt(18)
        run.font.color.rgb = COLOR_BLACK
        return p

    def add_h2(text):
        p = doc.add_paragraph()
        p.paragraph_format.space_before = Pt(15)
        p.paragraph_format.space_after = Pt(6)
        p.paragraph_format.keep_with_next = True
        run = p.add_run(text)
        run.bold = True
        run.font.name = FONT_FAMILY
        run.font.size = Pt(15)
        run.font.color.rgb = COLOR_BLACK
        return p

    def add_h3(text):
        p = doc.add_paragraph()
        p.paragraph_format.space_before = Pt(11)
        p.paragraph_format.space_after = Pt(4)
        p.paragraph_format.keep_with_next = True
        run = p.add_run(text)
        run.bold = True
        run.font.name = FONT_FAMILY
        run.font.size = Pt(13)
        run.font.color.rgb = COLOR_BLACK
        return p

    def add_body_p(text, bold_prefix="", italic=False):
        p = doc.add_paragraph()
        p.paragraph_format.space_before = Pt(2)
        p.paragraph_format.space_after = Pt(5)
        p.paragraph_format.line_spacing = 1.25
        p.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
        if bold_prefix:
            rb = p.add_run(bold_prefix)
            rb.bold = True
            rb.font.name = FONT_FAMILY
            rb.font.size = Pt(12)
            rb.font.color.rgb = COLOR_BLACK
        r = p.add_run(text)
        r.italic = italic
        r.font.name = FONT_FAMILY
        r.font.size = Pt(12)
        r.font.color.rgb = COLOR_BLACK
        return p

    def add_bullet_p(text, bold_prefix=""):
        p = doc.add_paragraph(style='List Bullet')
        p.paragraph_format.space_before = Pt(2)
        p.paragraph_format.space_after = Pt(3)
        p.paragraph_format.line_spacing = 1.25
        if bold_prefix:
            rb = p.add_run(bold_prefix)
            rb.bold = True
            rb.font.name = FONT_FAMILY
            rb.font.size = Pt(12)
            rb.font.color.rgb = COLOR_BLACK
        r = p.add_run(text)
        r.font.name = FONT_FAMILY
        r.font.size = Pt(12)
        r.font.color.rgb = COLOR_BLACK
        return p

    # --- TITLE / COVER BLOCK (STRICTLY BLACK AND WHITE) ---
    title_p = doc.add_paragraph()
    title_p.paragraph_format.space_before = Pt(10)
    title_p.paragraph_format.space_after = Pt(4)
    title_p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    run_t1 = title_p.add_run(
        "UNIVERSITATEA DIN CRAIOVA\n"
        "FACULTATEA DE AUTOMATICĂ, CALCULATOARE ȘI ELECTRONICĂ\n"
        "DEPARTAMENTUL DE CALCULATOARE ȘI TEHNOLOGIA INFORMAȚIEI\n\n"
    )
    run_t1.bold = True
    run_t1.font.name = FONT_FAMILY
    run_t1.font.size = Pt(12)
    run_t1.font.color.rgb = COLOR_BLACK

    title_main = doc.add_paragraph()
    title_main.paragraph_format.space_before = Pt(14)
    title_main.paragraph_format.space_after = Pt(8)
    title_main.alignment = WD_ALIGN_PARAGRAPH.CENTER
    run_m = title_main.add_run("SPECIFICAȚIA CERINȚELOR SOFTWARE (SRS)")
    run_m.bold = True
    run_m.font.name = FONT_FAMILY
    run_m.font.size = Pt(24)
    run_m.font.color.rgb = COLOR_BLACK

    sub_p = doc.add_paragraph()
    sub_p.paragraph_format.space_before = Pt(0)
    sub_p.paragraph_format.space_after = Pt(20)
    sub_p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    run_s = sub_p.add_run(
        "Aplicație Full Stack Tracking Nutrițional\n"
        "cu Extragere Automată a Valorilor Nutriționale prin OCR\n"
        "Conform Standardului IEEE 830-1998 (Adaptat)"
    )
    run_s.font.name = FONT_FAMILY
    run_s.font.size = Pt(14)
    run_s.font.color.rgb = COLOR_BLACK

    # Metadata Table (Pure White Background, Crisp Black Borders)
    meta_data = [
        ("Tip Document:", "Software Requirements Specification (SRS) - Lucrare de Licență"),
        ("Domeniu de Specialitate:", "Calculatoare și Tehnologia Informației"),
        ("Autor:", "Maximilian Andrei"),
        ("Coordonator Științific:", "Dr. Ing. Mocanu Lucian-Mihai"),
        ("Titlul Temei:", "Aplicație Full Stack Tracking Nutrițional"),
        ("Arhitectură de Referință:", "Clean Architecture, CQRS (MediatR), React (TypeScript) PWA, ASP.NET Core 8 Web API, Python OCR"),
        ("Stadiu Proiect & Versiune:", "Versiunea 1.2 (Conformă Standard IEEE 830)")
    ]

    meta_table = doc.add_table(rows=len(meta_data), cols=2)
    meta_table.alignment = WD_TABLE_ALIGNMENT.CENTER
    meta_table.autofit = False
    meta_table.columns[0].width = Inches(2.2)
    meta_table.columns[1].width = Inches(4.07)
    set_table_borders(meta_table, color="000000", sz="6")

    for i, (k, v) in enumerate(meta_data):
        row = meta_table.rows[i]
        c0, c1 = row.cells[0], row.cells[1]
        set_cell_background(c0, "FFFFFF")
        set_cell_background(c1, "FFFFFF")
        set_cell_margins(c0, top=90, bottom=90, left=130, right=130)
        set_cell_margins(c1, top=90, bottom=90, left=130, right=130)
        
        p0 = c0.paragraphs[0]
        p0.paragraph_format.space_after = Pt(0)
        r0 = p0.add_run(k)
        r0.bold = True
        r0.font.name = FONT_FAMILY
        r0.font.size = Pt(11)
        r0.font.color.rgb = COLOR_BLACK
        
        p1 = c1.paragraphs[0]
        p1.paragraph_format.space_after = Pt(0)
        r1 = p1.add_run(v)
        r1.font.name = FONT_FAMILY
        r1.font.size = Pt(11)
        r1.font.color.rgb = COLOR_BLACK

    doc.add_page_break()

    # --- 1. INTRODUCERE ---
    add_h1("1. INTRODUCERE")
    
    add_h2("1.1 Scopul Documentului")
    add_body_p(
        "Prezentul document definește Specificația Cerințelor Software (SRS - Software Requirements Specification) "
        "pentru sistemul informatic integrat de monitorizare a nutriției, planificare a meselor și asistență dietetică. "
        "Documentul a fost structurat în conformitate cu recomandările standardului IEEE 830-1998 și servește drept fundament formal "
        "pentru întregul ciclu de dezvoltare: proiectare arhitecturală, implementare tehnică, asigurarea calității și validare experimentală "
        "în cadrul lucrării de licență în Ingineria Software."
    )

    add_h2("1.2 Scopul Sistemului (Project Scope)")
    add_body_p(
        "Sistemul proiectat este un ecosistem distribuit, cross-platform (aplicație mobilă dedicată, aplicație web de gestiune și analiză, "
        "backend unificat REST API și un microserviciu specializat de Computer Vision/AI). Misiunea principală este digitalizarea și simplificarea radicală "
        "a comportamentului de jurnalizare nutrițională."
    )
    add_body_p(
        "În mod tradițional, monitorizarea aportului caloric este abandonată de utilizatori din cauza fricțiunii ridicate generate de căutarea "
        "și tastarea manuală a fiecărui ingredient și nutrient. Sistemul elimină această barieră prin introducerea unui pipeline automat bazat pe "
        "Inteligență Artificială și Optical Character Recognition (OCR), capabil să extragă valorile macro și micronutrienților direct din fotografiile "
        "etichetelor alimentare ambalate. Ecosistemul se completează cu un jurnal alimentar dinamic, un planificator avansat de rețete și un algoritm "
        "inteligent de consolidare automată a listelor de cumpărături."
    )

    add_h2("1.3 Convenții Documentare și Sistem de Notare")
    add_body_p(
        "Pentru a garanta rigoarea academică și trasabilitatea cerințelor, sunt adoptate următoarele convenții de etichetare:"
    )
    add_bullet_p("Cerință Funcțională (Functional Requirement), numerotată crescător (ex: FR01, FR02).", "FR-xx: ")
    add_bullet_p("Cerință Non-Funcțională (Non-Functional Requirement), numerotată crescător (ex: NFR01, NFR02).", "NFR-xx: ")
    add_bullet_p("Caz de Utilizare analitic (Use Case) detaliat operațional.", "UC-xx: ")
    add_bullet_p("Cerință critică indispensabilă pentru lansarea și funcționarea de bază a sistemului (Must-Have).", "P1 (Critic): ")
    add_bullet_p("Cerință importantă cu valoare adăugată majoră (Should-Have).", "P2 (Important): ")
    add_bullet_p("Cerință opțională sau extensie funcțională de confort (Nice-to-Have).", "P3 (Opțional): ")

    add_h2("1.4 Public Țintă și Ghid de Lectură")
    add_body_p(
        "Acest document se adresează comisiei de evaluare a lucrării de licență, arhitecților software, dezvoltatorilor frontend/backend/mobile "
        "și inginerilor de testare (QA). Secțiunile 2 și 3 oferă perspectiva contextuală și cazurile de utilizare, Secțiunile 4 și 5 descriu specificațiile "
        "tehnice concrete, iar Secțiunile 6 și 7 analizează limitele sistemului și matricea completă de trasabilitate."
    )

    # --- 2. DESCRIEREA GENERALĂ A SISTEMULUI ---
    add_h1("2. DESCRIEREA GENERALĂ A SISTEMULUI")

    add_h2("2.1 Perspectiva Produsului și Arhitectura la Nivel Înalt")
    add_body_p(
        "Sistemul este conceput conform paradigmei Clean Architecture și a șablonului CQRS (Command Query Responsibility Segregation), "
        "izolând complet logica de afaceri de infrastructura de date și de subsistemul de inferență vizuală. Diagrama de mai jos ilustrează "
        "fluxurile de date și interfețele dintre componentele majore:"
    )

    arch_diagram = (
"+-----------------------------------------------------------------------------------+\n"
"|                              DISPOZITIVE CLIENT                                   |\n"
"|   +------------------------------------+   +----------------------------------+   |\n"
"|   |  Aplicație Mobilă (.NET MAUI / RN)  |   |   Portal Web (Blazor WebAssembly)|  |\n"
"|   |  - Modul Captură Cameră + Ghidaj    |   |   - Dashboard Analitic & Admin   |  |\n"
"|   +-----------------+------------------+   +-----------------+----------------+  |\n"
"+---------------------|----------------------------------------|--------------------+\n"
"                      | HTTPS / REST / JWT                     | HTTPS / REST / JWT\n"
"                      v                                        v\n"
"+-----------------------------------------------------------------------------------+\n"
"|                               BACKEND PRINCIPAL                                   |\n"
"|                      ASP.NET Core REST API (Clean Architecture)                   |\n"
"|   +---------------------------------------------------------------------------+   |\n"
"|   | Application Layer: MediatR (CQRS - Commands & Queries), FluentValidation  |   |\n"
"|   +---------------------------------------------------------------------------+   |\n"
"|   | Core Domain Layer: Entities, Value Objects, Domain Events                 |   |\n"
"|   +---------------------------------------------------------------------------+   |\n"
"|   | Infrastructure Layer: EF Core, Identity, JWT Handler, HttpClientFactory    |   |\n"
"|   +--------------------+----------------------------------+-------------------+   |\n"
"+------------------------|----------------------------------|-----------------------+\n"
"                         |                                  |\n"
"            gRPC / REST  | (Asincron prin Coadă / Polling)  | EF Core\n"
"                         v                                  v\n"
"+------------------------------------+    +-----------------------------------------+\n"
"|      MICROSERVICIU AI (PYTHON)      |    |        BAZĂ DE DATE RELAȚIONALĂ         |\n"
"|  - FastAPI Engine                  |    |  PostgreSQL / SQL Server                 |\n"
"|  - Preprocesare: OpenCV            |    |  - Tabele: Users, Products, Logs,        |\n"
"|  - OCR: Tesseract / EasyOCR        |    |            Recipes, MealPlans, Targets   |\n"
"|  - Parser Euristic & Regex         |    |  - Migrări versionate via EF Core        |\n"
"+------------------------------------+    +-----------------------------------------+"
    )
    add_code_block(arch_diagram)

    add_h2("2.2 Arhitectura Tehnologică Detaliată")
    add_body_p(
        "Fiecare palier al ecosistemului utilizează tehnologii moderne, selectate pentru a îndeplini constrângerile academice și industriale:"
    )
    add_bullet_p(
        "Construit pe ASP.NET Core 8.0, structurat pe 4 straturi (Domain, Application, Infrastructure, WebAPI). "
        "Mediatorul MediatR decuplează procesarea comenzilor de scriere (Commands) de interogările optimizate de citire (Queries). "
        "Validarea la intrare este garantată de pipeline behaviors prin FluentValidation.",
        "Backend Principal: "
    )
    add_bullet_p(
        "Interfață bazată pe Blazor WebAssembly, rulând cod C# compilat în WebAssembly în browserul utilizatorului. "
        "Asigură un dashboard analitic performant, administrarea catalogului de produse și vizualizarea grafică avansată.",
        "Frontend Web: "
    )
    add_bullet_p(
        "Aplicație cross-platform (.NET MAUI sau React Native), permițând accesul nativ la senzorii hardware: camera foto de înaltă rezoluție, "
        "accelerometru și giroscop (pentru detecția vibrațiilor/stabilității mâinii) și senzorul de lumină ambientală.",
        "Aplicație Mobilă: "
    )
    add_bullet_p(
        "Microserviciu Python expus prin FastAPI. Pipeline-ul de viziune computerizată folosește OpenCV pentru operațiuni de "
        "conversie în tonuri de gri, filtrare bilaterală pentru zgomot, corecție de orientare (deskew) și binarizare Otsu. "
        "Inferența optică combină Tesseract OCR cu EasyOCR (modele deep learning bazate pe CRAFT și ResNet-LSTM) asistate de un parser euristic cu regex-uri flexibile.",
        "Modul AI/OCR (Microserviciu Separat): "
    )
    add_bullet_p(
        "Sistem de Gestiune a Bazelor de Date Relaționale (PostgreSQL sau Microsoft SQL Server), accesat prin Entity Framework Core. "
        "Include migrări automate, indexare pe chei externe și constrângeri de unicitate și integritate referențială.",
        "Bază de Date: "
    )

    add_h2("2.3 Clase de Utilizatori și Caracteristici")
    add_body_p("Sistemul identifică trei categorii distincte de utilizatori cu responsabilități și privilegii bine delimitate:")

    # Table Users (White background, black borders, bold headers)
    user_table = doc.add_table(rows=4, cols=4)
    user_table.alignment = WD_TABLE_ALIGNMENT.CENTER
    user_table.autofit = False
    user_table.columns[0].width = Inches(1.3)
    user_table.columns[1].width = Inches(2.2)
    user_table.columns[2].width = Inches(1.3)
    user_table.columns[3].width = Inches(1.47)
    set_table_borders(user_table, color="000000", sz="6")

    headers_u = ["Clasă Utilizator", "Descriere & Obiective", "Competență Tehnică", "Drepturi / Rol"]
    for j, h in enumerate(headers_u):
        c = user_table.rows[0].cells[j]
        set_cell_background(c, "FFFFFF")
        set_cell_margins(c, top=90, bottom=90, left=100, right=100)
        p = c.paragraphs[0]
        p.paragraph_format.space_after = Pt(0)
        r = p.add_run(h)
        r.bold = True
        r.font.name = FONT_FAMILY
        r.font.size = Pt(11.5)
        r.font.color.rgb = COLOR_BLACK

    users_data = [
        ("Utilizator Neautentificat", "Vizitator al paginii web sau al aplicației mobile; poate parcurge onboarding-ul și fluxul de login.", "Redusă / Medie", "Anonim (Guest)"),
        ("Utilizator Final Înregistrat", "Persoană interesată de fitness/nutriție. Scanează etichete, ține jurnalul alimentar, creează rețete și planuri.", "Medie", "Rol: Standard User (JWT User)"),
        ("Administrator Sistem", "Responsabil de integritatea bazei de date globale de alimente, monitorizarea performanței OCR și gestiunea erorilor.", "Avansată", "Rol: System Admin (JWT Admin)")
    ]

    for i, row_data in enumerate(users_data):
        row = user_table.rows[i+1]
        for j, val in enumerate(row_data):
            c = row.cells[j]
            set_cell_background(c, "FFFFFF")
            set_cell_margins(c, top=80, bottom=80, left=100, right=100)
            p = c.paragraphs[0]
            p.paragraph_format.space_after = Pt(0)
            r = p.add_run(val)
            r.font.name = FONT_FAMILY
            r.font.size = Pt(11)
            r.font.color.rgb = COLOR_BLACK

    add_h2("2.4 Mediu de Operare")
    add_bullet_p("Android 11.0 (API Level 30) sau superior; iOS 15.0 sau superior; necesită cameră cu Auto-Focus și acces la internet.", "Client Mobil: ")
    add_bullet_p("Orice browser web modern compatibil WebAssembly (Google Chrome v90+, Mozilla Firefox v88+, Apple Safari v14+, Microsoft Edge v90+).", "Client Web: ")
    add_bullet_p("Sistem de operare Linux (Ubuntu Server 22.04 LTS / Debian 12), containerizat prin Docker Engine 24+, orchestrat via Docker Compose sau Kubernetes.", "Infrastructură Server: ")

    # --- 3. CAZURI DE UTILIZARE REPREZENTATIVE ---
    add_h1("3. CAZURI DE UTILIZARE REPREZENTATIVE (USE CASES)")

    add_h2("3.1 Diagrama Logică Textuală a Actorilor și Cazurilor de Utilizare")
    add_body_p(
        "Interacțiunile dintre actorul principal (Utilizatorul Final Înregistrat) și funcționalitățile sistemului sunt reprezentate în diagrama relațională următoare:"
    )

    uc_diagram = (
"  +--------------------------------+\n"
"  |   Utilizator Final Autentificat|\n"
"  +---------------+----------------+\n"
"                  |\n"
"                  +---> [UC01: Scanare și Extragere Valori Nutriționale via OCR]\n"
"                  |        |\n"
"                  |        +-- <<include>> --> [Validare Manuală Date Extrase]\n"
"                  |        +-- <<extend>>  --> [Tratare Erori Imagine Neclară]\n"
"                  |\n"
"                  +---> [UC02: Înregistrare și Monitorizare în Jurnalul Alimentar]\n"
"                  |\n"
"                  +---> [UC03: Creare Rețetă Compusă și Calcul Agregat Macros]\n"
"                  |\n"
"                  +---> [UC04: Planificare Săptămânală a Meselor]\n"
"                  |        |\n"
"                  |        +-- <<include>> --> [UC05: Generare Listă Cumpărături]\n"
"                  |\n"
"                  +---> [UC06: Monitorizare și Vizualizare Statistici & Grafice]"
    )
    add_code_block(uc_diagram)

    add_h2("3.2 Specificarea Cazurilor de Utilizare Cheie")

    # UC01 Detailed Table (Black on White)
    add_h3("Cazul de Utilizare UC01: Scanare și Extragere Valori Nutriționale via OCR")
    
    uc01_table = doc.add_table(rows=8, cols=2)
    uc01_table.alignment = WD_TABLE_ALIGNMENT.CENTER
    uc01_table.autofit = False
    uc01_table.columns[0].width = Inches(1.8)
    uc01_table.columns[1].width = Inches(4.47)
    set_table_borders(uc01_table, color="000000", sz="6")

    uc01_fields = [
        ("Identificator & Nume", "UC01: Scanare și Extragere Valori Nutriționale via OCR"),
        ("Actor Principal", "Utilizator Final Autentificat"),
        ("Precondiții", "1. Utilizatorul este autentificat în aplicația mobilă.\n2. Permisiunea de utilizare a camerei foto este acordată.\n3. Dispozitivul are conexiune activă la internet."),
        ("Postcondiții", "Produsul alimentar este validat de utilizator și persistat în baza de date cu valorile nutriționale asociate."),
        ("Declanșator (Trigger)", "Utilizatorul selectează butonul „Scanare Etichetă” din ecranul de adăugare produs."),
        ("Flux Principal de Evenimente (Normal Flow)", 
         "1. Aplicația mobilă inițializează ecranul camerei cu ghidaj rectangular suprapus.\n"
         "2. Sistemul analizează senzorii și informează utilizatorul când imaginea este stabilă.\n"
         "3. Utilizatorul declanșează captura foto a tabelului nutrițional.\n"
         "4. Aplicația comprimă imaginea (max 1080p, JPEG q=85) și o trimite prin POST HTTPS la ASP.NET Core API.\n"
         "5. Backend-ul redirecționează payload-ul către microserviciul Python FastAPI.\n"
         "6. Modulul OpenCV aplică: conversie grayscale, filtrare zgomot, deskewing și binarizare Otsu.\n"
         "7. Modulul OCR extrage blocurile de text și localizează perechile cheie-valoare pentru energie (kcal/kJ), proteine, carbohidrați, grăsimi, fibre și sare.\n"
         "8. Parserul euristic detectează unitatea de referință (per 100g sau per porție).\n"
         "9. Microserviciul returnează răspunsul structurat JSON către backend, care îl livrează mobilului.\n"
         "10. Aplicația mobilă deschide formularul de revizuire, pre-populat cu datele extrase.\n"
         "11. Utilizatorul inspectează valorile, completează denumirea produsului și apasă „Salvează”.\n"
         "12. Datele sunt persistate în baza de date relațională și produsul devine disponibil în jurnal."),
        ("Fluxuri Alternative & Excepții",
         "A1. Nivel insuficient de lumină: La pasul 2, senzorul semnalează luminozitate critică; interfața afișează: „Lumină insuficientă. Activați blițul”.\n"
         "A2. Eșec OCR / Calitate slabă: La pasul 7, gradul de încredere (confidence score) este sub 50%; sistemul afișează formularul gol cu mesajul: „Eticheta nu a putut fi citită clar. Vă rugăm să introduceți datele manual”.\n"
         "A3. Lipsă conexiune rețea: La pasul 4, request-ul eșuează; sistemul avertizează utilizatorul și oferă opțiunea de salvare a pozei în galerie pentru reîncercare."),
        ("Reguli de Afaceri", "BR01: Niciun produs scanat nu este salvat direct fără confirmarea explicită a utilizatorului (Human-in-the-loop).")
    ]

    for i, (k, v) in enumerate(uc01_fields):
        row = uc01_table.rows[i]
        c0, c1 = row.cells[0], row.cells[1]
        set_cell_background(c0, "FFFFFF")
        set_cell_background(c1, "FFFFFF")
        set_cell_margins(c0, top=80, bottom=80, left=100, right=100)
        set_cell_margins(c1, top=80, bottom=80, left=100, right=100)
        
        p0 = c0.paragraphs[0]
        p0.paragraph_format.space_after = Pt(0)
        r0 = p0.add_run(k)
        r0.bold = True
        r0.font.name = FONT_FAMILY
        r0.font.size = Pt(11)
        r0.font.color.rgb = COLOR_BLACK
        
        p1 = c1.paragraphs[0]
        p1.paragraph_format.space_after = Pt(0)
        r1 = p1.add_run(v)
        r1.font.name = FONT_FAMILY
        r1.font.size = Pt(11)
        r1.font.color.rgb = COLOR_BLACK

    # UC05 Detailed Table (Black on White)
    add_h3("Cazul de Utilizare UC05: Generare Listă Consolidată de Cumpărături")
    
    uc05_table = doc.add_table(rows=7, cols=2)
    uc05_table.alignment = WD_TABLE_ALIGNMENT.CENTER
    uc05_table.autofit = False
    uc05_table.columns[0].width = Inches(1.8)
    uc05_table.columns[1].width = Inches(4.47)
    set_table_borders(uc05_table, color="000000", sz="6")

    uc05_fields = [
        ("Identificator & Nume", "UC05: Generare Listă Consolidată de Cumpărături"),
        ("Actor Principal", "Utilizator Final Autentificat"),
        ("Precondiții", "Utilizatorul are cel puțin o masă sau rețetă programată în planificatorul de mese pentru intervalul calendaristic specificat."),
        ("Postcondiții", "O listă de cumpărături unificată și agregată este calculată, afișată și pregătită pentru marcare interactivă."),
        ("Declanșator (Trigger)", "Utilizatorul accesează meniul „Planificator” și acționează butonul „Generare Listă Cumpărături”."),
        ("Flux Principal de Evenimente",
         "1. Utilizatorul selectează intervalul temporal dorit (ex: Luni 12 Octombrie - Duminică 18 Octombrie).\n"
         "2. Clientul trimite o cerere HTTP GET către endpoint-ul /api/v1/meal-plans/shopping-list cu parametrii de dată.\n"
         "3. Backend-ul (prin intermediul unui Query Handler MediatR) interoghează planul meselor din baza de date pentru utilizatorul curent.\n"
         "4. Algoritmul parcurge toate alimentele simple și rețetele compuse din interval.\n"
         "5. Pentru ingredientele duplicate, sistemul normalizează unitățile de măsură (ex: mililitri în litri, grame în kilograme) și calculează suma cantităților.\n"
         "6. Ingredientele sunt grupate pe categorii alimentare (Lactate, Carne, Legume & Fructe, Cereale, Condimente).\n"
         "7. Răspunsul este livrat către client și randat sub formă de listă bifabilă interactivă.\n"
         "8. Pe măsură ce utilizatorul cumpără produsele, bifează căsuțele corespunzătoare, starea fiind salvată local/sesiune."),
        ("Fluxuri Alternative & Excepții",
         "A1. Nu există mese planificate în interval: Sistemul afișează o alertă: „Nu aveți mese planificate pentru perioada selectată. Planificați mesele în calendar pentru a genera lista”.\n"
         "A2. Unități de măsură neomogene (ex: 3 bucăți măr + 250g mere rase): Algoritmul afișează ambele sub-totaluri separat pentru a nu crea confuzie.")
    ]

    for i, (k, v) in enumerate(uc05_fields):
        row = uc05_table.rows[i]
        c0, c1 = row.cells[0], row.cells[1]
        set_cell_background(c0, "FFFFFF")
        set_cell_background(c1, "FFFFFF")
        set_cell_margins(c0, top=80, bottom=80, left=100, right=100)
        set_cell_margins(c1, top=80, bottom=80, left=100, right=100)
        
        p0 = c0.paragraphs[0]
        p0.paragraph_format.space_after = Pt(0)
        r0 = p0.add_run(k)
        r0.bold = True
        r0.font.name = FONT_FAMILY
        r0.font.size = Pt(11)
        r0.font.color.rgb = COLOR_BLACK
        
        p1 = c1.paragraphs[0]
        p1.paragraph_format.space_after = Pt(0)
        r1 = p1.add_run(v)
        r1.font.name = FONT_FAMILY
        r1.font.size = Pt(11)
        r1.font.color.rgb = COLOR_BLACK

    # --- 4. CERINȚE FUNCȚIONALE (FR) ---
    doc.add_page_break()
    add_h1("4. CERINȚE FUNCȚIONALE (FUNCTIONAL REQUIREMENTS)")

    add_h2("4.1 Modulul AI / Optical Character Recognition (OCR)")
    
    fr_ocr = [
        ("FR01", "Captură Ghidată a Tabelului Nutrițional", "P1 (Critic)",
         "Aplicația mobilă trebuie să furnizeze o interfață personalizată de cameră foto ce suprapune un cadru rectangular transparent (bounding box) pentru a ghida încadrarea precisă a etichetei. Interfața va include comenzi pentru activarea blițului și comutarea focalizării automate."),
        ("FR02", "Pipeline Preprocesare Imagine (OpenCV)", "P1 (Critic)",
         "Microserviciul Python trebuie să proceseze imaginea primită printr-o succesiune de algoritmi de Computer Vision: reducere la scară de gri (grayscale), filtrare bilaterală pentru atenuarea zgomotului cu conservarea muchiilor, detecție automată a unghiului de rotație (deskewing) și binarizare adaptivă Otsu pentru maximizarea contrastului dintre text și fundal."),
        ("FR03", "Recunoaștere Text și Parsare Semantică a Nutrienților", "P1 (Critic)",
         "Sistemul OCR (Tesseract / EasyOCR) combinat cu modulul de analiză sintactică bazat pe expresii regulate și dicționare multilingve (Română, Engleză) trebuie să extragă valorile numerice și unitățile de măsură pentru: valoare energetică (kcal/kJ), proteine (g), carbohidrați totali (g), zaharuri (g), grăsimi totale (g), grăsimi saturate (g), fibre (g) și sare/sodiu (g/mg)."),
        ("FR04", "Inferență Referință Cantitativă (100g vs Porție)", "P1 (Critic)",
         "Algoritmul de parsare semantică trebuie să inspecteze antetele coloanelor din tabel pentru a identifica dacă valorile sunt raportate per 100g/100ml sau per porție (serving size). În cazul detecției ambelor coloane, se prioritizează raportarea per 100g, extrăgându-se totodată gramajul porției recomandate dacă este disponibil."),
        ("FR05", "Pre-populare Formular și Validare Umană (Human-in-the-Loop)", "P1 (Critic)",
         "Sistemul interzice persistența automată a datelor recunoscute fără confirmarea utilizatorului. Aplicația client trebuie să afișeze un formular de confirmare cu valorile extrase completate în câmpurile editabile, evidențiind cu avertisment vizual (marcaj distinct) câmpurile cu grad de încredere scăzut.")
    ]

    for fr_id, fr_name, fr_pri, fr_desc in fr_ocr:
        add_callout(f"Descriere: {fr_desc}\nPrioritate: {fr_pri} | Modul: Microserviciu Python & Client Mobil", f"[{fr_id}] {fr_name}")

    add_h2("4.2 Jurnal Alimentar și Monitorizare Nutrițională")
    
    fr_diary = [
        ("FR06", "Înregistrare Consum Alimentar pe Categorii de Mese", "P1 (Critic)",
         "Utilizatorul trebuie să poată introduce un aliment sau o rețetă în jurnalul unei zile specifice, repartizându-l într-una dintre cele 4 categorii prestabilite: Mic Dejun, Prânz, Cină, Gustare, cu specificarea masei consumate în grame sau număr de porții."),
        ("FR07", "Calcul Dinamic al Consumului Caloric și Macronutrienților", "P1 (Critic)",
         "Sistemul trebuie să calculeze și să actualizeze instantaneu totalul caloriilor și distribuția gramajelor de proteine, carbohidrați și lipide, atât la nivelul fiecărei mese în parte, cât și ca sumă cumulată a zilei curente, raportat la țintele configurate."),
        ("FR08", "Vizualizare Agregată și Analitice Temporale", "P2 (Important)",
         "Platforma (web și mobil) trebuie să pună la dispoziție vizualizări grafice interactive (grafice de tip bară și pie chart) reprezentând evoluția consumului pe intervale de 24 de ore, 7 zile și 30 de zile, comparând aportul real cu obiectivul stabilit."),
        ("FR09", "Jurnalizare Istorică și Editare Retrospectivă", "P2 (Important)",
         "Sistemul trebuie să permită navigarea în calendar pentru a consulta jurnale din trecut, editarea cantităților consumate anterior, ștergerea intrărilor eronate sau adăugarea retrospectivă a meselor omise.")
    ]

    for fr_id, fr_name, fr_pri, fr_desc in fr_diary:
        add_callout(f"Descriere: {fr_desc}\nPrioritate: {fr_pri} | Modul: Backend ASP.NET Core & Interfețe Client", f"[{fr_id}] {fr_name}")

    add_h2("4.3 Planificator Dietetic și Gestiune Rețetar")

    fr_planner = [
        ("FR10", "Configurare Obiective Nutriționale (Targets)", "P1 (Critic)",
         "Sistemul trebuie să permită definirea unui profil energetic personalizat: plafon caloric zilnic (kcal) și procentele dorite de macronutrienți (ex: 50% carbohidrați, 25% proteine, 25% lipide). Sistemul va valida matematic ca suma procentelor să fie egală cu 100%."),
        ("FR11", "Creare și Management Rețete Compuse", "P1 (Critic)",
         "Utilizatorul trebuie să poată crea entități de tip „Rețetă”, adăugând două sau mai multe alimente componente cu cantitățile aferente, precum și instrucțiuni opționale de preparare și numărul total de porții rezultate."),
        ("FR12", "Calcul Automat al Densității Nutriționale a Rețetei", "P1 (Critic)",
         "Sistemul trebuie să calculeze automat profilul nutrițional complet al preparatului rezultat (calorii, proteine, carbohidrați, lipide per 100g de rețetă finită și per porție individuală), actualizându-l dinamic dacă se modifică vreun ingredient."),
        ("FR13", "Planificator Săptămânal de Mese în Avans (Meal Planner)", "P2 (Important)",
         "Utilizatorul trebuie să poată asocia alimente și rețete pe zile viitoare în calendarul săptămânal, sistemul afișând gradul de încărcare caloric estimat față de ținta zilnică.")
    ]

    for fr_id, fr_name, fr_pri, fr_desc in fr_planner:
        add_callout(f"Descriere: {fr_desc}\nPrioritate: {fr_pri} | Modul: Backend ASP.NET Core & Application Layer", f"[{fr_id}] {fr_name}")

    add_h2("4.4 Modulul de Generare a Listei de Cumpărături")

    fr_shop = [
        ("FR14", "Consolidare Algoritmică a Ingredientelor", "P1 (Critic)",
         "Sistemul trebuie să parcurgă toate rețetele și alimentele programate în planificatorul de mese pe un interval temporal selectat de utilizator și să execute un algoritm de fuziune a elementelor identice (ex: adunarea cantităților de carne de pui din 3 mese distincte într-o singură poziție de cumpărare)."),
        ("FR15", "Normalizare și Conversie a Unităților de Măsură", "P2 (Important)",
         "Algoritmul de agregare trebuie să convertească automat unitățile de măsură compatibile (ex: 750g + 500g devin 1.25 kg; 250ml + 750ml devin 1 litru). În cazul unităților incompatibile (ex: bucăți vs grame), sistemul va lista sub-cantitățile pe linii distincte."),
        ("FR16", "Interactivitate și Gestiune Progres Listă Cumpărături", "P3 (Opțional)",
         "Lista de cumpărături generată trebuie să fie interactivă: utilizatorul poate bifa produsele cumpărate (acestea fiind mutate vizual la secțiunea „Achiziționate”), poate adăuga manual articole suplimentare necorelate cu planul alimentar și poate exporta lista sub formă text sau PDF.")
    ]

    for fr_id, fr_name, fr_pri, fr_desc in fr_shop:
        add_callout(f"Descriere: {fr_desc}\nPrioritate: {fr_pri} | Modul: Backend ASP.NET Core & Interfețe Client", f"[{fr_id}] {fr_name}")

    # --- 5. CERINȚE NON-FUNCȚIONALE (NFR) ---
    doc.add_page_break()
    add_h1("5. CERINȚE NON-FUNCȚIONALE (NON-FUNCTIONAL REQUIREMENTS)")

    add_h2("5.1 Performanță și Eficiență (Performance)")
    
    nfr_perf = [
        ("NFR01", "Timp de Răspuns Pipeline OCR (3-5 secunde)", "P1 (Critic)",
         "Timpul total scurs de la inițierea upload-ului imaginii de către clientul mobil până la recepționarea payload-ului JSON structurat de la microserviciul Python nu trebuie să depășească intervalul de 3-5 secunde în condițiile unei conexiuni 4G/LTE standard (latență RTT < 50ms) și o rezoluție optimizată a imaginii."),
        ("NFR02", "Latență Redusă API Backend (< 200 ms)", "P1 (Critic)",
         "Pentru operațiunile CRUD obișnuite (interogare jurnal, adăugare aliment, actualizare profil), 95% din cererile HTTP (p95) procesate de backend-ul ASP.NET Core trebuie să aibă un timp de răspuns sub 200 ms la o încărcare de până la 50 de utilizatori concurenți."),
        ("NFR03", "Compresie și Optimizare Payload Binar (< 1.5 MB)", "P2 (Important)",
         "Aplicația mobilă va procesa fotografia capturată local înainte de upload: redimensionare la maximum 1920x1080 pixeli și compresie JPEG cu factor de calitate 85%, asigurând că dimensiunea fișierului transferat prin rețea nu depășește 1.5 MB.")
    ]

    for nfr_id, nfr_name, nfr_pri, nfr_desc in nfr_perf:
        add_callout(f"Descriere: {nfr_desc}\nPrioritate: {nfr_pri} | Categorie: Performanță", f"[{nfr_id}] {nfr_name}")

    add_h2("5.2 Disponibilitate, Fiabilitate și Scalabilitate")

    nfr_scal = [
        ("NFR04", "Procesare Asincronă și Cozi de Așteptare", "P1 (Critic)",
         "Sistemul trebuie să gestioneze eficient cererile concurente de scanare OCR. În situația în care microserviciul Python atinge limita de procesare simultană (thread pool / GPU memory limit), noile cereri vor fi ordonate într-o coadă FIFO (Channel asincron sau RabbitMQ/Redis), utilizatorul primind un indicator de progres fără blocarea aplicației."),
        ("NFR05", "Toleranță la Erori și Degradare Grațioasă", "P2 (Important)",
         "Dacă microserviciul Python devine temporar indisponibil sau depășește un prag de timeout stabilit la 8 secunde, backend-ul principal va returna un răspuns HTTP 503 cu cod de eroare explicit. Aplicația client va degrada grațios prin afișarea unui mesaj informativ și deschiderea directă a formularului manual de adăugare.")
    ]

    for nfr_id, nfr_name, nfr_pri, nfr_desc in nfr_scal:
        add_callout(f"Descriere: {nfr_desc}\nPrioritate: {nfr_pri} | Categorie: Fiabilitate & Scalabilitate", f"[{nfr_id}] {nfr_name}")

    add_h2("5.3 Securitate și Conformitate (Security)")

    nfr_sec = [
        ("NFR06", "Autentificare și Autorizare Bazată pe JWT", "P1 (Critic)",
         "Accesul la toate endpoint-urile protejate ale API-ului este condiționat de validarea unui JSON Web Token (JWT) semnat criptografic cu algoritmul HMAC-SHA256 sau RSA-256. Token-urile de acces au o durabilitate de 15 minute și sunt reînnoite printr-un mecanism securizat de Refresh Token cu persistență în baza de date și opțiune de revocare imediată."),
        ("NFR07", "Stocare Criptografică a Parolelor", "P1 (Critic)",
         "Parolele conturilor de utilizator nu sunt niciodată stocate în clar. Sistemul folosește un mecanism de dispersie criptografică cu salt adaptiv bazat pe Argon2id sau PBKDF2 cu HMAC-SHA512 (standardul ASP.NET Core Identity), cu un număr minim de 100.000 de iterații."),
        ("NFR08", "Validare Strictă a Datelor și Imunitate la Injecții (SQLi/XSS)", "P1 (Critic)",
         "Toate interogările către baza de date relațională sunt intermediate de Entity Framework Core prin interogări parametrizate, eliminând complet vulnerabilitățile de tip SQL Injection. Payload-urile de intrare sunt validate la nivel de Application Layer prin FluentValidation, respingând date invalide, scripturi malițioase sau valori biologice absurde (ex: macronutrienți negativi)."),
        ("NFR09", "Criptarea Comunicațiilor în Tranzit (TLS 1.3)", "P1 (Critic)",
         "Orice canal de comunicație între aplicațiile client (mobil, web) și backend, precum și între backend și microserviciul AI, este strict restricționat la protocolul HTTPS utilizând TLS 1.3 (minim TLS 1.2 cu suite de cifruri securizate).")
    ]

    for nfr_id, nfr_name, nfr_pri, nfr_desc in nfr_sec:
        add_callout(f"Descriere: {nfr_desc}\nPrioritate: {nfr_pri} | Categorie: Securitate", f"[{nfr_id}] {nfr_name}")

    add_h2("5.4 Ergonomie și Ghidaj Interfață Utilizator (UX/UI Usability)")

    nfr_ux = [
        ("NFR10", "Ghidaj Senzorial în Timp Real la Captură", "P2 (Important)",
         "Interfața mobilă de captură trebuie să analizeze datele de la senzorii dispozitivului pentru a asigura calitatea optimă a fotografiei: cadrul rectangular indică stabilitatea camerei și afișează un avertisment contextual proactiv („Lumină ambientală slabă – activați blițul”) dacă expunerea este deficitară."),
        ("NFR11", "Eficiență în Validarea Formularului (< 10 secunde)", "P2 (Important)",
         "Interfața utilizator pentru ecranul de confirmare a datelor scanate trebuie concepută astfel încât timpul mediu necesar unui utilizator obișnuit pentru a verifica valorile și a confirma salvarea să fie sub 10 secunde.")
    ]

    for nfr_id, nfr_name, nfr_pri, nfr_desc in nfr_ux:
        add_callout(f"Descriere: {nfr_desc}\nPrioritate: {nfr_pri} | Categorie: Ergonomie & UX", f"[{nfr_id}] {nfr_name}")

    # --- 6. LIMITELE SISTEMULUI, CONSTRÂNGERI ȘI ASUMPȚII ---
    doc.add_page_break()
    add_h1("6. LIMITELE SISTEMULUI, CONSTRÂNGERI ȘI ASUMPȚII")

    add_h2("6.1 Dependența Critică de Calitatea Imaginii și Reflexii")
    add_body_p(
        "Acuratețea oricărui algoritm bazat pe Computer Vision și OCR depinde în mod critic de proprietățile fizice ale mediului de captură. "
        "În domeniul ambalajelor alimentare, provocările frecvente includ:"
    )
    add_bullet_p("Ambalaje confecționate din folie de aluminiu sau plastic lucios care reflectă lumina directă a blițului sau a corpurilor de iluminat, ducând la supraexpunere locală și pierderea contrastului literelor.", "Reflexii Speculare: ")
    add_bullet_p("Sticle, doze metalice sau pungi deformate care induc o distorsiune neliniară a textului tabelului nutrițional, făcând detecția liniilor orizontale dificilă.", "Suprafețe Curbate: ")
    add_bullet_p("Mișcarea dispozitivului în momentul declanșării în condiții de timp lung de expunere induce blur cinetic ireversibil.", "Mișcare și Focalizare: ")
    add_body_p(
        "Măsură de atenuare adoptată: Sistemul țintește o rată de recunoaștere automată corectă de minim 85% pe etichete plane și curate. "
        "Pentru a proteja fiabilitatea jurnalului alimentar, etapa de verificare umană (Human-in-the-Loop - FR05) este declarată o barieră obligatorie "
        "de sistem, oferind utilizatorului posibilitatea de a corecta instantaneu orice discrepanță înainte de persistență."
    )

    add_h2("6.2 Eterogenitatea și Lipsa Standardizării Globale a Etichetelor")
    add_body_p(
        "Formatul și terminologia informațiilor nutriționale nu sunt unificate la nivel internațional:"
    )
    add_bullet_p("Conform Regulamentului (UE) nr. 1169/2011, menționarea valorilor per 100g sau per 100ml este strict obligatorie, exprimată în kJ și kcal, cu raportare standardizată pentru 7 nutrienți obligatorii.", "Standardul European (UE): ")
    add_bullet_p("Standardul FDA (Food and Drug Administration) impune raportarea primară per „Serving Size” (ex: per cup, per 2 cookies), valorile per 100g fiind deseori absente.", "Standardul American (FDA): ")
    add_bullet_p("Variații lingvistice majore (ex: „Valoare energetică”, „Energy”, „Brennwert”; „Glucide”, „Carbohydrates”, „Kohlenhydrate”).", "Diferențe Lingvistice: ")
    add_body_p(
        "Măsură de atenuare adoptată: Parserul semantic integrează dicționare sinonimice extinse și euristici care caută cuvintele-cheie pentru baza de raportare. "
        "În cazul ambiguităților nerezolvabile algoritmic, utilizatorul este invitat explicit prin formular să confirme dacă valorile se referă la 100g sau la întregul produs/porție."
    )

    add_h2("6.3 Conectivitate Exclusivă la Rețea (Cloud-based AI vs On-Device)")
    add_body_p(
        "Modelele optice moderne bazate pe rețele neuronale profunde (Deep Learning OCR - CRAFT / ResNet) și bibliotecile robuste de viziune computerizată (OpenCV) "
        "necesită un volum substanțial de memorie RAM și putere de calcul grafic. Rularea acestor modele direct pe dispozitive mobile de gamă medie/joasă (On-Device ML) "
        "ar conduce la supraîncălzire, consum accelerat al bateriei și dimensiuni exorbitante ale pachetului de instalare a aplicației mobile (> 150 MB)."
    )
    add_body_p(
        "Constrângere asumată: Întreaga sarcină grea de prelucrare a imaginilor este transferată în server/cloud pe microserviciul Python. "
        "Prin urmare, modulul de scanare OCR impune cu strictețe o conexiune activă și stabilă la internet. Modul offline este limitat la "
        "consultarea datelor aflate deja în memoria cache a dispozitivului și introducerea manuală a meselor cu sincronizare la reconectare."
    )

    add_h2("6.4 Asumpții Tehnologice și Politici de Confidențialitate")
    add_bullet_p("Dispozitivul utilizatorului este echipat cu o cameră foto de minim 8 Megapixeli cu funcție de Auto-Focus activă.", "Hardware Mobil: ")
    add_bullet_p("Fotografiile trimise pentru procesare OCR sunt menținute în memoria volatilă a microserviciului exclusiv pe durata ciclului de inferență (câteva secunde), fiind șterse imediat după extragerea textului. Doar datele nutriționale textuale validate sunt salvate în baza de date, respectând principiile de minimizare a datelor și confidențialitate.", "Retenție Date: ")

    # --- 7. MATRICE DE TRASABILITATE (STRICTLY BLACK AND WHITE) ---
    doc.add_page_break()
    add_h1("7. MATRICE DE TRASABILITATE ȘI REZUMATUL CERINȚELOR")
    add_body_p(
        "Tabelul sintetic de mai jos centralizează toate cerințele funcționale și non-funcționale specificate în acest document, "
        "evidențiind componenta responsabilă, nivelul de prioritate MoSCoW, cazul de utilizare asociat și criteriul de acceptare tehnic:"
    )

    matrix_table = doc.add_table(rows=28, cols=5)
    matrix_table.alignment = WD_TABLE_ALIGNMENT.CENTER
    matrix_table.autofit = False
    matrix_table.columns[0].width = Inches(0.8)
    matrix_table.columns[1].width = Inches(1.8)
    matrix_table.columns[2].width = Inches(1.1)
    matrix_table.columns[3].width = Inches(0.9)
    matrix_table.columns[4].width = Inches(1.67)
    set_table_borders(matrix_table, color="000000", sz="6")

    headers_m = ["ID", "Denumire Cerință", "Subsistem", "Prioritate", "Criteriu de Verificare"]
    for j, h in enumerate(headers_m):
        c = matrix_table.rows[0].cells[j]
        set_cell_background(c, "FFFFFF")
        set_cell_margins(c, top=90, bottom=90, left=80, right=80)
        p = c.paragraphs[0]
        p.paragraph_format.space_after = Pt(0)
        r = p.add_run(h)
        r.bold = True
        r.font.name = FONT_FAMILY
        r.font.size = Pt(11)
        r.font.color.rgb = COLOR_BLACK

    matrix_rows = [
        ("FR01", "Captură Ghidată Tabel Nutrițional", "Client Mobil", "P1", "Overlay vizual activ, flash toggle funcțional"),
        ("FR02", "Pipeline Preprocesare OpenCV", "Python AI", "P1", "Imagine filtrată, contrast mărit, rotire corectată"),
        ("FR03", "Recunoaștere Text și Parsare Macros", "Python AI", "P1", "Extragere corectă calorii, P, C, F, sare"),
        ("FR04", "Inferență Bază Raportare (100g/porție)", "Python AI", "P1", "Detecție corectă 100g sau gramaj porție"),
        ("FR05", "Validare Umană Formular (HitL)", "Client Mobil/Web", "P1", "Formular pre-populat, confirmare manuală"),
        ("FR06", "Înregistrare Consum în Jurnal", "Backend / DB", "P1", "Persistență masă pe categorii prestabilite"),
        ("FR07", "Calcul Dinamic Calorii & Macros", "Backend / App", "P1", "Recalculare instantanee la schimbarea cantității"),
        ("FR08", "Vizualizare Agregată Grafice", "Client Mobil/Web", "P2", "Grafice bară/pie pe 24h, 7 zile, 30 zile"),
        ("FR09", "Jurnalizare Istorică & Editare", "Backend / DB", "P2", "Navigare date anterioare, editare, ștergere"),
        ("FR10", "Configurare Obiective Nutriționale", "Backend / DB", "P1", "Plafon caloric și procent macros validat 100%"),
        ("FR11", "Creare și Management Rețete", "Backend / DB", "P1", "Asociere multiplă ingrediente + gramaje"),
        ("FR12", "Calcul Densitate Nutrițională Rețetă", "Backend / App", "P1", "Suma macros per total și per porție calculată"),
        ("FR13", "Planificator Săptămânal Mese", "Backend / DB", "P2", "Planificare pe zile în avans cu target vizibil"),
        ("FR14", "Consolidare Listă Cumpărături", "Backend / App", "P1", "Agregare ingrediente comune din planul meselor"),
        ("FR15", "Normalizare Unități de Măsură", "Backend / App", "P2", "Conversie automată g->kg, ml->l"),
        ("FR16", "Interactivitate Listă Cumpărături", "Client Mobil/Web", "P3", "Bifare elemente cumpărate, adăugare manuală"),
        ("NFR01", "Timp Răspuns OCR 3-5 secunde", "Python AI / API", "P1", "Test de performanță: latență < 5s pe 4G"),
        ("NFR02", "Latență API CRUD sub 200ms", "Backend ASP.NET", "P1", "Benchmark k6/JMeter: p95 < 200ms la 50 VU"),
        ("NFR03", "Compresie Imagine sub 1.5 MB", "Client Mobil", "P2", "Dimensiune fișier payload verificată programatic"),
        ("NFR04", "Procesare Asincronă / Cozi", "Backend / Queue", "P1", "Thread pool non-blocant la solicitare concurentă"),
        ("NFR05", "Degradare Grațioasă la Timeout", "Backend ASP.NET", "P2", "HTTP 503 controlat la timeout AI > 8s"),
        ("NFR06", "Autentificare JWT & Refresh Token", "Backend / Security", "P1", "Token expirat la 15 min, reînnoire validată"),
        ("NFR07", "Hashing Securizat Parole (Argon2)", "Backend / Security", "P1", "Salt unic, verificare rezistență atac forță brută"),
        ("NFR08", "Validare FluentValidation / EF Core", "Backend / App", "P1", "Interogări 100% parametrizate, XSS blocat"),
        ("NFR09", "Criptare TLS 1.3 în Tranzit", "Infrastructură", "P1", "Scor SSL Labs A+, forțare HTTPS strict"),
        ("NFR10", "Ghidaj Vizual și Senzorial Captură", "Client Mobil", "P2", "Indicator stabilitate cameră, alertă lumină"),
        ("NFR11", "Timp Validare Formular < 10s", "UX / UI", "P2", "Test de utilizabilitate pe eșantion utilizatori")
    ]

    for i, row_data in enumerate(matrix_rows):
        row = matrix_table.rows[i+1]
        for j, val in enumerate(row_data):
            c = row.cells[j]
            set_cell_background(c, "FFFFFF")
            set_cell_margins(c, top=60, bottom=60, left=70, right=70)
            p = c.paragraphs[0]
            p.paragraph_format.space_after = Pt(0)
            r = p.add_run(val)
            r.font.name = FONT_FAMILY
            r.font.size = Pt(10)
            r.font.color.rgb = COLOR_BLACK
            if j == 0 or j == 3:
                r.bold = True

    # Final Notes
    add_h2("7.1 Concluzii și Pași Următori de Dezvoltare")
    add_body_p(
        "Specificația de cerințe reflectă fidel obiectivele de cercetare și inginerie ale lucrării de licență. "
        "Etapa următoare constă în elaborarea Documentului de Arhitectură și Design Software (SAD - Software Architecture Document), "
        "urmată de implementarea verticală a primului prototip funcțional (User Story: Înregistrare utilizator -> Captură foto -> "
        "Inferență microserviciu AI -> Populare Jurnal Alimentar)."
    )

    script_dir = os.path.dirname(os.path.abspath(__file__))
    out_linux = os.path.abspath(os.path.join(script_dir, "..", "Specificatie_Cerinte_Software_SRS.docx"))
    doc.save(out_linux)
    print(f"Document successfully created at: {out_linux}")

    # Also try saving to the Obsidian Vault if accessible
    out_vault = "/home/Max/Documents/Obsidian Vault/Licenta/Specificatie_Cerinte_Software_SRS.docx"
    try:
        doc.save(out_vault)
        print(f"Document successfully created at: {out_vault}")
    except Exception as e:
        print(f"Notice: Vault is read-only ({e}), saved in Linux workspace: {out_linux}")

if __name__ == "__main__":
    create_black_and_white_srs()
