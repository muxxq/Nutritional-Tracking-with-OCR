#!/usr/bin/env python3
"""Populează aplicația cu un cont demo, ca interfața să poată fi explorată cu date realiste.

Creează (sau refolosește) contul demo@nutriscan.ro / Demo1234! și adaugă, doar prin API-ul public:
  * profilul fizic (din care backend-ul calculează BMR, TDEE și țintele de macronutrienți);
  * câteva alimente în plus față de catalogul inițial;
  * 4 rețete;
  * jurnalul ultimelor 3 săptămâni;
  * planul săptămânii curente și lista de cumpărături generată din el.

Rulare (cu aplicația pornită prin `docker compose up -d`):
    python3 scripts/seed_demo.py                # API implicit: http://localhost:5000/api
    python3 scripts/seed_demo.py --api http://alt-host:5000/api

Folosește doar biblioteca standard Python 3.8+. Este idempotent: o a doua rulare nu dublează datele.
"""
from __future__ import annotations

import argparse
import datetime as dt
import json
import random
import sys
import urllib.error
import urllib.request

EMAIL = "demo@nutriscan.ro"
PASSWORD = "Demo1234!"

EXTRA_FOODS = [
    # nume, marcă, kcal, proteine, carbohidrați, grăsimi (per 100 g)
    ("Pâine integrală", None, 247, 13, 41, 3.4),
    ("Brânză telemea de vacă", None, 265, 17, 1.5, 21),
    ("Roșii", None, 18, 0.9, 3.9, 0.2),
    ("Avocado", None, 160, 2, 8.5, 14.7),
    ("Măr", None, 52, 0.3, 14, 0.2),
    ("Paste integrale", None, 350, 13, 64, 2.5),
    ("Lapte 1.5%", None, 46, 3.3, 4.8, 1.5),
    ("Cartofi", None, 77, 2, 17, 0.1),
    ("Ton în apă", None, 103, 24, 0, 0.8),
]

RECIPES = [
    ("Bol cu somon și orez", "Bol proteic cu somon la cuptor, orez basmati și broccoli.", 2, 30,
     [("File de somon", 300), ("Orez alb", 150), ("Broccoli", 250), ("Ulei de măsline", 15)]),
    ("Ovăz cu banană și migdale", "Mic dejun rapid, bogat în fibre.", 1, 5,
     [("Fulgi de ovăz", 60), ("Banană", 120), ("Migdale", 15), ("Lapte", 200)]),
    ("Pui cu cartofi la cuptor", "Piept de pui marinat cu cartofi și roșii.", 3, 45,
     [("Piept de pui", 500), ("Cartofi", 600), ("Roșii", 300), ("Ulei de măsline", 20)]),
    ("Paste cu ton și roșii", "Cină simplă în 15 minute.", 2, 15,
     [("Paste integrale", 180), ("Ton", 160), ("Roșii", 250), ("Ulei de măsline", 10)]),
]


class Api:
    def __init__(self, base: str) -> None:
        self.base = base.rstrip("/")
        self.token: str | None = None

    def call(self, method: str, path: str, body: object | None = None, ok_codes: tuple[int, ...] = ()):
        headers = {"Content-Type": "application/json"}
        if self.token:
            headers["Authorization"] = f"Bearer {self.token}"
        data = json.dumps(body).encode() if body is not None else None
        request = urllib.request.Request(self.base + path, data=data, method=method, headers=headers)
        try:
            with urllib.request.urlopen(request, timeout=30) as response:
                raw = response.read()
                return json.loads(raw) if raw else None
        except urllib.error.HTTPError as error:
            if error.code in ok_codes:
                return None
            sys.exit(f"{method} {path} -> {error.code}: {error.read()[:300].decode(errors='replace')}")
        except urllib.error.URLError as error:
            sys.exit(f"API-ul nu răspunde la {self.base} ({error.reason}). Aplicația este pornită?")

    def all_pages(self, path: str) -> list[dict]:
        items, page = [], 1
        while True:
            result = self.call("GET", f"{path}{'&' if '?' in path else '?'}pageSize=50&page={page}")
            items += result["items"]
            if len(items) >= result["totalCount"] or not result["items"]:
                return items
            page += 1


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--api", default="http://localhost:5000/api", help="adresa de bază a API-ului")
    api = Api(parser.parse_args().api)

    api.call("POST", "/auth/register",
             {"email": EMAIL, "password": PASSWORD, "firstName": "Andrei", "lastName": "Popescu"}, ok_codes=(409,))
    api.token = api.call("POST", "/auth/login", {"email": EMAIL, "password": PASSWORD})["accessToken"]
    api.call("PUT", "/profile", {"firstName": "Andrei", "lastName": "Popescu", "birthDate": "2001-05-14",
                                 "heightCm": 180, "weightKg": 78, "gender": "male",
                                 "activityLevel": "moderatelyActive", "goal": "maintainWeight"})

    existing = {f["name"] for f in api.all_pages("/foods")}
    for name, brand, kcal, prot, carbs, fats in EXTRA_FOODS:
        if name not in existing:
            api.call("POST", "/foods", {"name": name, "brand": brand, "calories": kcal,
                                        "proteins": prot, "carbohydrates": carbs, "fats": fats})
    foods = {f["name"]: f["id"] for f in api.all_pages("/foods")}

    def food(prefix: str) -> str:
        return next(fid for name, fid in foods.items() if name.startswith(prefix))

    existing = {r["name"] for r in api.all_pages("/recipes")}
    for name, description, servings, prep, ingredients in RECIPES:
        if name not in existing:
            api.call("POST", "/recipes", {
                "name": name, "description": description, "servingsCount": servings, "prepTimeMinutes": prep,
                "instructions": "1. Pregătiți ingredientele.\n2. Gătiți conform timpului indicat.\n3. Porționați și serviți.",
                "ingredients": [{"foodId": food(p), "quantityGrams": g} for p, g in ingredients]})
    recipes = {r["name"]: r["id"] for r in api.all_pages("/recipes")}

    today = dt.date.today()
    rng = random.Random(7)
    today_entries = api.call("GET", f"/diary/days/{today.isoformat()}")["meals"]
    if not any(meal["entries"] for meal in today_entries):
        for days_ago in range(20, -1, -1):
            if days_ago in (9, 15):  # câteva zile nenotate, ca statisticile să fie realiste
                continue
            date = (today - dt.timedelta(days=days_ago)).isoformat()
            s = rng.uniform(0.85, 1.15)
            entries = [
                ("breakfast", "recipe", "Ovăz cu banană și migdale", 1),
                ("breakfast", "food", "Iaurt grecesc", round(150 * s)),
                ("lunch", "recipe", rng.choice(["Bol cu somon și orez", "Pui cu cartofi la cuptor"]), 1),
                ("lunch", "food", "Pâine integrală", round(60 * s)),
                ("snack", "food", "Măr", 180),
                ("snack", "food", "Migdale", round(25 * s)),
                ("dinner", "recipe", "Paste cu ton și roșii", 1) if days_ago % 2
                else ("dinner", "food", "Ou de găină", round(150 * s)),
                ("dinner", "food", "Brânză telemea", round(60 * s)),
                ("dinner", "food", "Avocado", round(70 * s)),
            ]
            if days_ago == 0:  # ziua curentă este notată doar parțial
                entries = entries[:6] + [entries[7]]
            for meal, kind, name, qty in entries:
                body = {"date": date, "mealType": meal}
                body.update({"recipeId": recipes[name], "servings": qty} if kind == "recipe"
                            else {"foodId": food(name), "quantityGrams": qty})
                api.call("POST", "/diary/entries", body)

    monday = today - dt.timedelta(days=today.weekday())
    sunday = monday + dt.timedelta(days=6)
    plan = api.call("GET", f"/plan?from={monday}&to={sunday}")
    if not any(item for day in plan["days"] for meal in day["meals"] for item in meal["items"]):
        for offset in range(7):
            date = (monday + dt.timedelta(days=offset)).isoformat()
            lunch = ["Bol cu somon și orez", "Pui cu cartofi la cuptor"][offset % 2]
            for meal, name in [("breakfast", "Ovăz cu banană și migdale"), ("lunch", lunch),
                               ("dinner", "Paste cu ton și roșii")]:
                api.call("POST", "/plan/items", {"date": date, "mealType": meal, "recipeId": recipes[name], "servings": 1})
            api.call("POST", "/plan/items", {"date": date, "mealType": "snack",
                                             "foodId": food("Iaurt grecesc"), "quantityGrams": 170})
        api.call("POST", "/shopping-list/generate", {"from": monday.isoformat(), "to": sunday.isoformat()})
        api.call("POST", "/shopping-list/items", {"name": "Hârtie de copt", "quantity": "1 rolă", "category": "other"})

    print(f"Gata. Autentificați-vă la http://localhost:3000 cu {EMAIL} / {PASSWORD}")


if __name__ == "__main__":
    main()
