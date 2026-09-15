#!/usr/bin/env bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
DIST_DIR="$PROJECT_ROOT/dist"

mkdir -p "$DIST_DIR"

TARGET_ENV="${1:-${TARGET_ENV:-Testing}}"

echo "=========================================="
echo "  ADI Application Cross-Platform Packaging"
echo "  Target Environment: $TARGET_ENV"
echo "=========================================="

OS_TYPE="$(uname -s)"

if [ "$OS_TYPE" = "Darwin" ]; then
    echo "-> Running macOS App & DMG Build Pipeline (Universal, Intel x64, Apple Silicon)..."
    export TARGET_ENV="$TARGET_ENV"
    bash "$PROJECT_ROOT/installer/macos/build-dmg.sh" universal "$TARGET_ENV"
    bash "$PROJECT_ROOT/installer/macos/build-dmg.sh" osx-x64 "$TARGET_ENV"
    bash "$PROJECT_ROOT/installer/macos/build-dmg.sh" osx-arm64 "$TARGET_ENV"
fi

echo "-> Building Windows Self-Contained Release (win-x64) [$TARGET_ENV]..."
cd "$PROJECT_ROOT"
dotnet publish ADIapp.csproj \
    -c Release \
    -r win-x64 \
    --self-contained true \
    -p:TargetEnvironment="$TARGET_ENV"

echo "-> Building Linux Self-Contained Release (linux-x64) [$TARGET_ENV]..."
dotnet publish ADIapp.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    -p:TargetEnvironment="$TARGET_ENV"

if [ "$OS_TYPE" = "Linux" ]; then
    echo "-> Packaging Linux DEB Installer..."
    bash "$PROJECT_ROOT/installer/linux/build-deb.sh" linux-x64
fi

echo ""
echo "=========================================="
echo " Packaging Completed Successfully!"
echo " Output files placed in: $DIST_DIR"
echo "=========================================="
ls -la "$DIST_DIR" || true
