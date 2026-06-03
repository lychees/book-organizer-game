#!/bin/bash

# ============================================================
# Git Auto Push Script for Unity Project
# Usage: ./git-auto-push.sh
# ============================================================

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}  Unity Project - Git Auto Push${NC}"
echo -e "${BLUE}========================================${NC}"

# --------------------------------------------------
# 1. Check if inside a git repository
# --------------------------------------------------
if ! git rev-parse --git-dir > /dev/null 2>&1; then
    echo -e "${RED}[Error] Not a git repository.${NC}"
    echo "Please run this script from inside a git-tracked project."
    exit 1
fi

PROJECT_ROOT=$(git rev-parse --show-toplevel)
cd "$PROJECT_ROOT"
echo -e "${BLUE}Project root: $PROJECT_ROOT${NC}"

# --------------------------------------------------
# 2. Check remote 'origin'
# --------------------------------------------------
REMOTE=$(git remote get-url origin 2>/dev/null || true)
if [ -z "$REMOTE" ]; then
    echo -e "${RED}[Error] No remote 'origin' configured.${NC}"
    echo "Add one with: git remote add origin <your-repo-url>"
    exit 1
fi
echo -e "${BLUE}Remote origin: $REMOTE${NC}"

# --------------------------------------------------
# 3. Check current branch
# --------------------------------------------------
BRANCH=$(git rev-parse --abbrev-ref HEAD)
echo -e "${BLUE}Current branch: $BRANCH${NC}"

# --------------------------------------------------
# 4. Check if there are any changes
# --------------------------------------------------
MODIFIED=$(git diff --name-only | wc -l | tr -d ' ')
STAGED=$(git diff --staged --name-only | wc -l | tr -d ' ')
UNTRACKED=$(git ls-files --others --exclude-standard | wc -l | tr -d ' ')

TOTAL_CHANGES=$((MODIFIED + STAGED + UNTRACKED))

if [ "$TOTAL_CHANGES" -eq 0 ]; then
    echo -e "${YELLOW}[Info] No changes detected. Nothing to commit.${NC}"
    exit 0
fi

echo -e "${BLUE}Changes detected:${NC}"
echo "  - Modified (unstaged): $MODIFIED"
echo "  - Staged:              $STAGED"
echo "  - Untracked:           $UNTRACKED"

# --------------------------------------------------
# 5. Pull latest changes first (to avoid conflicts)
# --------------------------------------------------
echo -e "${YELLOW}[Step 1/4] Pulling latest changes from origin/$BRANCH...${NC}"
if git pull --rebase origin "$BRANCH" 2>/dev/null || git pull origin "$BRANCH" 2>/dev/null; then
    echo -e "${GREEN}  -> Pull successful (or already up to date).${NC}"
else
    echo -e "${YELLOW}  -> Could not pull (might be offline or no upstream set). Continuing...${NC}"
fi

# --------------------------------------------------
# 6. Stage all changes
# --------------------------------------------------
echo -e "${YELLOW}[Step 2/4] Staging all changes...${NC}"
git add -A
echo -e "${GREEN}  -> All changes staged.${NC}"

# --------------------------------------------------
# 7. Generate commit message
# --------------------------------------------------
TIMESTAMP=$(date '+%Y-%m-%d %H:%M:%S')
HOSTNAME=$(hostname)

# List changed files for detail
CHANGED_FILES=$(git diff --staged --name-only | head -10 | sed 's/^/  - /')
FILE_COUNT=$(git diff --staged --name-only | wc -l | tr -d ' ')

COMMIT_MSG="auto: $TIMESTAMP

Changes: $FILE_COUNT file(s)
Host: $HOSTNAME
Branch: $BRANCH

Files:
$CHANGED_FILES"

echo -e "${YELLOW}[Step 3/4] Committing...${NC}"
echo -e "${BLUE}Commit message preview:${NC}"
echo "----------------------------------------"
echo "$COMMIT_MSG"
echo "----------------------------------------"

git commit -m "$COMMIT_MSG"

# --------------------------------------------------
# 8. Push to remote
# --------------------------------------------------
echo -e "${YELLOW}[Step 4/4] Pushing to origin/$BRANCH...${NC}"
if git push origin "$BRANCH"; then
    echo -e "${GREEN}========================================${NC}"
    echo -e "${GREEN}  Push successful!${NC}"
    echo -e "${GREEN}  $REMOTE${NC}"
    echo -e "${GREEN}  Branch: $BRANCH${NC}"
    echo -e "${GREEN}========================================${NC}"
else
    echo -e "${RED}========================================${NC}"
    echo -e "${RED}  Push failed!${NC}"
    echo -e "${RED}  Please check your network connection${NC}"
    echo -e "${RED}  or GitHub credentials.${NC}"
    echo -e "${RED}========================================${NC}"
    exit 1
fi
