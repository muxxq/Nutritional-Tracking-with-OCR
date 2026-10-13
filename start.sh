#!/usr/bin/env bash
set -e

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
RED='\033[0;31m'
NC='\033[0m' # No Color

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
APLICATIE_DIR="$SCRIPT_DIR"

if [ -d "$SCRIPT_DIR/aplicatie" ]; then
    APLICATIE_DIR="$SCRIPT_DIR/aplicatie"
fi

MODE="${1:-local}"

if [ "$MODE" = "docker" ]; then
    echo -e "${BLUE}🐳 Pornire NutriScan cu Docker Compose...${NC}"
    cd "$APLICATIE_DIR"
    docker compose up --build
    exit 0
fi

echo -e "${GREEN}================================================================${NC}"
echo -e "${GREEN}       🌿 NutriScan — Sistem Monitorizare Nutrițională cu OCR   ${NC}"
echo -e "${GREEN}================================================================${NC}"
echo -e "${CYAN}Mod rulare: Local Development (Fără Docker)${NC}\n"

# Array to keep track of background processes
PIDS=()

cleanup() {
    echo -e "\n${YELLOW}🛑 Oprire servicii NutriScan...${NC}"
    for pid in "${PIDS[@]}"; do
        if kill -0 "$pid" 2>/dev/null; then
            kill "$pid" 2>/dev/null || true
        fi
    done
    wait 2>/dev/null || true
    echo -e "${GREEN}✔ Toate serviciile au fost oprite curat.${NC}"
}
trap cleanup SIGINT SIGTERM EXIT

# 1. Start Python OCR Service
echo -e "${BLUE}[1/3] Pornire Microserviciu Python OCR (FastAPI)...${NC}"
cd "$APLICATIE_DIR/ocr-service"
if [ -d ".venv" ]; then
    .venv/bin/uvicorn app.main:app --host 0.0.0.0 --port 8000 > /tmp/nutriscan-ocr.log 2>&1 &
else
    python3 -m uvicorn app.main:app --host 0.0.0.0 --port 8000 > /tmp/nutriscan-ocr.log 2>&1 &
fi
OCR_PID=$!
PIDS+=("$OCR_PID")
echo -e "      ${GREEN}✔ OCR API Docs:${NC}  http://localhost:8000/docs"

# 2. Start .NET 8 Backend Web API
echo -e "${BLUE}[2/3] Pornire Backend Core (.NET 8 Web API)...${NC}"
cd "$APLICATIE_DIR/backend"
dotnet run --project src/WebApi --launch-profile http > /tmp/nutriscan-backend.log 2>&1 &
BACKEND_PID=$!
PIDS+=("$BACKEND_PID")
echo -e "      ${GREEN}✔ Swagger API:${NC}   http://localhost:5000/swagger"

# 3. Start React Vite Frontend
echo -e "${BLUE}[3/3] Pornire Frontend Client (React 18/19 Vite PWA)...${NC}"
cd "$APLICATIE_DIR/client"
echo -e "\n${GREEN}================================================================${NC}"
echo -e "${GREEN}✨ APLICAȚIA ESTE PORNITĂ! ACCESEAZĂ ÎN BROWSER:${NC}"
echo -e "   👉 ${GREEN}Interfață Web PWA:${NC}  http://localhost:3000"
echo -e "   👉 ${BLUE}Swagger Backend:${NC}   http://localhost:5000/swagger"
echo -e "   👉 ${BLUE}Swagger OCR:${NC}       http://localhost:8000/docs"
echo -e "${GREEN}================================================================${NC}"
echo -e "${YELLOW}Jurnalele de fundal se află în /tmp/nutriscan-*.log${NC}"
echo -e "${YELLOW}Apasă Ctrl+C în orice moment pentru a opri toate serviciile.${NC}\n"

npm run dev
