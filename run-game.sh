#!/bin/bash

# ============================================================
# Auto Run Game Script for Book Organizer Unity Project
# Usage: ./run-game.sh
# ============================================================

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m'

PROJECT_ROOT="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT_WIN=$(cygpath -w "$PROJECT_ROOT")
UNITY_EXE="/d/Dev/Unity/6000.4.9f1/Editor/Unity.exe"
UNITY_EXE_WIN="D:\Dev\Unity\6000.4.9f1\Editor\Unity.exe"
SCENE_PATH="Assets/Scenes/BookOrganizer.unity"
REPL_SCRIPT="$PROJECT_ROOT/Library/PackageCache/com.lambda-labs.unity-repl@8cb19eac84b4/repl.sh"

cd "$PROJECT_ROOT"

echo -e "${CYAN}========================================${NC}"
echo -e "${CYAN}  Book Organizer - Auto Run${NC}"
echo -e "${CYAN}========================================${NC}"

# --------------------------------------------------
# 1. Check Unity installation
# --------------------------------------------------
if [ ! -f "$UNITY_EXE" ]; then
    echo -e "${RED}[Error] Unity not found at: $UNITY_EXE${NC}"
    exit 1
fi

# --------------------------------------------------
# 2. Find REPL script
# --------------------------------------------------
if [ ! -f "$REPL_SCRIPT" ]; then
    REPL_SCRIPT=$(find "$PROJECT_ROOT/Library/PackageCache" -name "repl.sh" -path "*unity-repl*" | head -n 1)
    if [ -z "$REPL_SCRIPT" ]; then
        echo -e "${RED}[Error] Unity REPL script not found.${NC}"
        exit 1
    fi
fi

echo -e "${BLUE}Unity: $UNITY_EXE_WIN${NC}"
echo -e "${BLUE}REPL:  $REPL_SCRIPT${NC}"

# --------------------------------------------------
# 3. Helper: check if REPL is ready
# --------------------------------------------------
ping_repl() {
    "$REPL_SCRIPT" --timeout 3 -e 'Application.unityVersion' >/dev/null 2>&1
}

# --------------------------------------------------
# 4. Check if Unity is already running
# --------------------------------------------------
UNITY_READY=false

if ping_repl; then
    echo -e "${GREEN}[Info] Unity is already running and REPL is ready.${NC}"
    UNITY_READY=true
else
    echo -e "${YELLOW}[Step 1/3] Starting Unity Editor...${NC}"
    echo -e "${YELLOW}If a security warning appears, click 'I wish to continue at my own risk'.${NC}"
    
    # Launch Unity via PowerShell with Windows-style paths
    powershell.exe -Command "Start-Process '$UNITY_EXE_WIN' -ArgumentList '-projectPath','$PROJECT_ROOT_WIN'" &
    
    # Wait for Unity to initialize
    echo -e "${YELLOW}Waiting for Unity to initialize (this may take 10-30s)...${NC}"
    
    MAX_RETRIES=20
    for i in $(seq 1 $MAX_RETRIES); do
        sleep 3
        
        # Check if Unity process exists
        UNITY_PID=$(tasklist 2>/dev/null | grep -i "Unity.exe" | awk '{print $2}')
        if [ -z "$UNITY_PID" ]; then
            echo -e "${YELLOW}  -> Unity process not found yet... ($i/$MAX_RETRIES)${NC}"
            continue
        fi
        
        if ping_repl; then
            echo -e "${GREEN}  -> Unity is ready! (PID: $UNITY_PID)${NC}"
            UNITY_READY=true
            break
        fi
        
        echo -e "${YELLOW}  -> Waiting for REPL... ($i/$MAX_RETRIES)${NC}"
    done
fi

if [ "$UNITY_READY" != "true" ]; then
    echo -e "${RED}========================================${NC}"
    echo -e "${RED}  Unity failed to start within 60s.${NC}"
    echo -e "${RED}  Possible reasons:${NC}"
    echo -e "${RED}    - Administrator privilege warning is blocking${NC}"
    echo -e "${RED}    - Unity is still importing packages${NC}"
    echo -e "${RED}    - Another Unity instance is running${NC}"
    echo -e "${RED}========================================${NC}"
    exit 1
fi

# --------------------------------------------------
# 5. Ensure the correct scene is open
# --------------------------------------------------
echo -e "${YELLOW}[Step 2/3] Checking active scene...${NC}"
CURRENT_SCENE=$("$REPL_SCRIPT" --timeout 5 -e 'SceneManager.GetActiveScene().path' 2>/dev/null | tr -d '\r')

if echo "$CURRENT_SCENE" | grep -q "BookOrganizer"; then
    echo -e "${GREEN}  -> Scene 'BookOrganizer' is already open.${NC}"
else
    echo -e "${YELLOW}  -> Opening scene: $SCENE_PATH${NC}"
    "$REPL_SCRIPT" --timeout 10 -e "EditorSceneManager.OpenScene(\"$SCENE_PATH\")" >/dev/null 2>&1
    sleep 1
    echo -e "${GREEN}  -> Scene opened.${NC}"
fi

# --------------------------------------------------
# 6. Enter Play Mode
# --------------------------------------------------
echo -e "${YELLOW}[Step 3/3] Entering Play Mode...${NC}"
"$REPL_SCRIPT" --timeout 5 -e 'EditorApplication.isPlaying = true' >/dev/null 2>&1

sleep 1
IS_PLAYING=$("$REPL_SCRIPT" --timeout 3 -e 'EditorApplication.isPlaying' 2>/dev/null | tr -d '\r')

if [ "$IS_PLAYING" = "True" ]; then
    echo -e "${GREEN}========================================${NC}"
    echo -e "${GREEN}  Game is now running!${NC}"
    echo -e "${GREEN}========================================${NC}"
    echo ""
    echo -e "${CYAN}Controls:${NC}"
    echo "  WASD  - Move"
    echo "  E     - Pick up / Place book"
    echo "  F     - Read / Open PDF"
    echo ""
    echo -e "${CYAN}To stop, switch back to Unity and click the Play button.${NC}"
else
    echo -e "${RED}[Warning] Could not verify Play Mode status.${NC}"
    echo -e "${YELLOW}Please manually click the Play button in Unity.${NC}"
fi
