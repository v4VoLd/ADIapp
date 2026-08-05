#!/usr/bin/env bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
DIST_DIR="$PROJECT_ROOT/dist"

MODE="${1:-universal}"
APP_NAME="ADIapp"
BUNDLE_DIR="/tmp/adi_build/$MODE/$APP_NAME.app"

cd "$PROJECT_ROOT"

if [ "$MODE" = "universal" ]; then
    echo "=== Building macOS Universal (Intel x86_64 + Apple Silicon ARM64) ==="
    
    rm -rf "$PROJECT_ROOT/bin/Release/net9.0/osx-x64"
    rm -rf "$PROJECT_ROOT/bin/Release/net9.0/osx-arm64"

    dotnet publish ADIapp.csproj -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=false -p:UseAppHost=true
    dotnet publish ADIapp.csproj -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=false -p:UseAppHost=true

    rm -rf "$BUNDLE_DIR"
    mkdir -p "$BUNDLE_DIR/Contents/MacOS"
    mkdir -p "$BUNDLE_DIR/Contents/Resources"

    # Start with arm64 tree as base template
    rsync -av --exclude="*.app" "$PROJECT_ROOT/bin/Release/net9.0/osx-arm64/publish/" "$BUNDLE_DIR/Contents/MacOS/"

    # Merge Mach-O executables and dylibs using lipo
    X64_DIR="$PROJECT_ROOT/bin/Release/net9.0/osx-x64/publish"
    ARM64_DIR="$PROJECT_ROOT/bin/Release/net9.0/osx-arm64/publish"

    for arm_file in $(find "$ARM64_DIR" -type f); do
        rel_path="${arm_file#$ARM64_DIR/}"
        x64_file="$X64_DIR/$rel_path"
        target_file="$BUNDLE_DIR/Contents/MacOS/$rel_path"

        if [ -f "$x64_file" ]; then
            if file "$arm_file" | grep -q "Mach-O"; then
                lipo -create "$arm_file" "$x64_file" -output "$target_file" 2>/dev/null || cp "$arm_file" "$target_file"
            fi
        fi
    done
else
    echo "=== Building macOS Self-Contained App for $MODE ==="
    rm -rf "$PROJECT_ROOT/bin/Release/net9.0/$MODE"
    dotnet publish ADIapp.csproj -c Release -r "$MODE" --self-contained true -p:PublishSingleFile=false -p:UseAppHost=true

    rm -rf "$BUNDLE_DIR"
    mkdir -p "$BUNDLE_DIR/Contents/MacOS"
    mkdir -p "$BUNDLE_DIR/Contents/Resources"

    rsync -av --exclude="*.app" "$PROJECT_ROOT/bin/Release/net9.0/$MODE/publish/" "$BUNDLE_DIR/Contents/MacOS/"
fi

echo "=== Assembling $APP_NAME.app Bundle Metadata ==="
cp "$SCRIPT_DIR/Info.plist" "$BUNDLE_DIR/Contents/Info.plist"

# Generate ICNS icon
ICONSET_DIR="/tmp/adi_app_icon.iconset"
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

echo "=== Creating macOS DMG Package ==="
mkdir -p "$DIST_DIR"

DMG_TMP="/tmp/adi_dmg_tmp"
rm -rf "$DMG_TMP"
mkdir -p "$DMG_TMP"

cp -R "$BUNDLE_DIR" "$DMG_TMP/"
ln -s /Applications "$DMG_TMP/Applications"

FINAL_DMG="$DIST_DIR/$APP_NAME-macOS-$MODE.dmg"
rm -f "$FINAL_DMG"

hdiutil create -volname "$APP_NAME" \
    -srcfolder "$DMG_TMP" \
    -ov -format UDZO \
    "$FINAL_DMG"

rm -rf "$DMG_TMP"

echo "=== macOS DMG Created Successfully: $FINAL_DMG ==="
