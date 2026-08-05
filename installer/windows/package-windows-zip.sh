#!/usr/bin/env bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
DIST_DIR="$PROJECT_ROOT/dist"

RID="win-x64"
APP_NAME="ADIapp"
PUBLISH_DIR="$PROJECT_ROOT/bin/Release/net9.0/$RID/publish"

echo "=== Building Windows Self-Contained Release ($RID) on macOS ==="
cd "$PROJECT_ROOT"
dotnet publish ADIapp.csproj \
    -c Release \
    -r "$RID" \
    --self-contained true \
    -p:PublishSingleFile=false

mkdir -p "$DIST_DIR"
ZIP_PATH="$DIST_DIR/$APP_NAME-Windows-$RID.zip"
rm -f "$ZIP_PATH"

echo "=== Packaging Windows ZIP Bundle ==="
cd "$PUBLISH_DIR"
zip -q -r "$ZIP_PATH" .

echo "=== Windows ZIP Package Created Successfully: $ZIP_PATH ==="
