#!/usr/bin/env bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
DIST_DIR="$PROJECT_ROOT/dist"

MODE="${1:-osx-x64}"
APP_NAME="ADIapp"

cd "$PROJECT_ROOT"

build_for_rid() {
    local RID="$1"
    local BUNDLE_DIR="/tmp/adi_build/$RID/$APP_NAME.app"
    local DMG_TMP="/tmp/adi_dmg_$RID"
    local FINAL_DMG="$DIST_DIR/$APP_NAME-macOS-$RID.dmg"

    echo "=== Building macOS Self-Contained App for $RID ==="
    rm -rf "$PROJECT_ROOT/bin/Release/net9.0/$RID"
    dotnet publish ADIapp.csproj -c Release -r "$RID" --self-contained true -p:PublishSingleFile=false -p:UseAppHost=true

    rm -rf "$BUNDLE_DIR"
    mkdir -p "$BUNDLE_DIR/Contents/MacOS"
    mkdir -p "$BUNDLE_DIR/Contents/Resources"

    rsync -av --exclude="*.app" "$PROJECT_ROOT/bin/Release/net9.0/$RID/publish/" "$BUNDLE_DIR/Contents/MacOS/"

    echo "=== Assembling $APP_NAME.app Metadata ==="
    cp "$SCRIPT_DIR/Info.plist" "$BUNDLE_DIR/Contents/Info.plist"

    # Generate ICNS icon
    local ICONSET_DIR="/tmp/adi_app_icon.iconset"
    rm -rf "$ICONSET_DIR"
    mkdir -p "$ICONSET_DIR"
    sips -z 16 16     "$PROJECT_ROOT/Assets/login_logo.png" --out "$ICONSET_DIR/icon_16x16.png" >/dev/null 2>&1 || true
    sips -z 32 32     "$PROJECT_ROOT/Assets/login_logo.png" --out "$ICONSET_DIR/icon_16x16@2x.png" >/dev/null 2>&1 || true
    sips -z 32 32     "$PROJECT_ROOT/Assets/login_logo.png" --out "$ICONSET_DIR/icon_32x32.png" >/dev/null 2>&1 || true
    sips -z 64 64     "$PROJECT_ROOT/Assets/login_logo.png" --out "$ICONSET_DIR/icon_32x32@2x.png" >/dev/null 2>&1 || true
    sips -z 128 128   "$PROJECT_ROOT/Assets/login_logo.png" --out "$ICONSET_DIR/icon_128x128.png" >/dev/null 2>&1 || true
    sips -z 256 256   "$PROJECT_ROOT/Assets/login_logo.png" --out "$ICONSET_DIR/icon_128x128@2x.png" >/dev/null 2>&1 || true
    sips -z 256 256   "$PROJECT_ROOT/Assets/login_logo.png" --out "$ICONSET_DIR/icon_256x256.png" >/dev/null 2>&1 || true
    sips -z 512 512   "$PROJECT_ROOT/Assets/login_logo.png" --out "$ICONSET_DIR/icon_256x256@2x.png" >/dev/null 2>&1 || true
    sips -z 512 512   "$PROJECT_ROOT/Assets/login_logo.png" --out "$ICONSET_DIR/icon_512x512.png" >/dev/null 2>&1 || true
    sips -z 1024 1024 "$PROJECT_ROOT/Assets/login_logo.png" --out "$ICONSET_DIR/icon_512x512@2x.png" >/dev/null 2>&1 || true

    iconutil -c icns "$ICONSET_DIR" -o "$BUNDLE_DIR/Contents/Resources/app_icon.icns" >/dev/null 2>&1 || true
    rm -rf "$ICONSET_DIR"

    # Set executable permissions
    chmod +x "$BUNDLE_DIR/Contents/MacOS/$APP_NAME"
    chmod +x "$BUNDLE_DIR/Contents/MacOS/"*.dylib 2>/dev/null || true

    # Strip quarantine and perform ad-hoc codesign
    xattr -cr "$BUNDLE_DIR" 2>/dev/null || true
    codesign --force --deep -s - "$BUNDLE_DIR" 2>/dev/null || true

    echo "=== Creating macOS DMG Package for $RID ==="
    mkdir -p "$DIST_DIR"
    rm -rf "$DMG_TMP"
    mkdir -p "$DMG_TMP"

    cp -R "$BUNDLE_DIR" "$DMG_TMP/"
    ln -s /Applications "$DMG_TMP/Applications"

    rm -f "$FINAL_DMG"
    hdiutil create -volname "$APP_NAME" \
        -srcfolder "$DMG_TMP" \
        -ov -format UDZO \
        "$FINAL_DMG"

    rm -rf "$DMG_TMP"
    echo "=== macOS DMG Created Successfully: $FINAL_DMG ==="
}

if [ "$MODE" = "all" ]; then
    build_for_rid "osx-x64"
    build_for_rid "osx-arm64"
else
    build_for_rid "$MODE"
fi
