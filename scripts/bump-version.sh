#!/usr/bin/env bash
set -e

# ==============================================================================
# One-Command Version Bump Utility for ADI Application
# Usage: ./scripts/bump-version.sh <version> [notes]
# Example: ./scripts/bump-version.sh 1.0.4 "Bug fixes and stability improvements"
# ==============================================================================

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
CAR_BINARY_DIR="$(cd "$PROJECT_ROOT/../car_binary" 2>/dev/null && pwd || echo "")"

NEW_VERSION="$1"
NOTES="${2:-Version $NEW_VERSION release.}"

if [ -z "$NEW_VERSION" ]; then
    echo "❌ Error: Version argument missing."
    echo "Usage: $0 <version> [notes]"
    echo "Example: $0 1.0.4 \"Release notes here...\""
    exit 1
fi

FOUR_PART_VERSION="${NEW_VERSION}.0"
RELEASE_DATE="$(date +'%Y-%m-%d')"

echo "=========================================="
echo " Bumping ADIapp to v${NEW_VERSION}"
echo "=========================================="

# 1. ADIapp.csproj (Single Source of Truth for .NET)
CSPROJ="$PROJECT_ROOT/ADIapp.csproj"
if [ -f "$CSPROJ" ]; then
    sed -i '' -E "s|<Version>[^<]+</Version>|<Version>${NEW_VERSION}</Version>|g" "$CSPROJ" 2>/dev/null || \
    sed -i -E "s|<Version>[^<]+</Version>|<Version>${NEW_VERSION}</Version>|g" "$CSPROJ"
    sed -i '' -E "s|<AssemblyVersion>[^<]+</AssemblyVersion>|<AssemblyVersion>${FOUR_PART_VERSION}</AssemblyVersion>|g" "$CSPROJ" 2>/dev/null || \
    sed -i -E "s|<AssemblyVersion>[^<]+</AssemblyVersion>|<AssemblyVersion>${FOUR_PART_VERSION}</AssemblyVersion>|g" "$CSPROJ"
    sed -i '' -E "s|<FileVersion>[^<]+</FileVersion>|<FileVersion>${FOUR_PART_VERSION}</FileVersion>|g" "$CSPROJ" 2>/dev/null || \
    sed -i -E "s|<FileVersion>[^<]+</FileVersion>|<FileVersion>${FOUR_PART_VERSION}</FileVersion>|g" "$CSPROJ"
    echo "  ✓ Updated ADIapp.csproj"
fi

# 2. Inno Setup (installer/windows/setup.iss)
SETUP_ISS="$PROJECT_ROOT/installer/windows/setup.iss"
if [ -f "$SETUP_ISS" ]; then
    sed -i '' -E "s|#define MyAppVersion \"[^\"]+\"|#define MyAppVersion \"${NEW_VERSION}\"|g" "$SETUP_ISS" 2>/dev/null || \
    sed -i -E "s|#define MyAppVersion \"[^\"]+\"|#define MyAppVersion \"${NEW_VERSION}\"|g" "$SETUP_ISS"
    echo "  ✓ Updated installer/windows/setup.iss"
fi

# 3. macOS Info.plist (installer/macos/Info.plist)
INFO_PLIST="$PROJECT_ROOT/installer/macos/Info.plist"
if [ -f "$INFO_PLIST" ]; then
    sed -i '' -E "/<key>CFBundleShortVersionString<\/key>/{n;s|<string>[^<]+</string>|<string>${NEW_VERSION}</string>|;}" "$INFO_PLIST" 2>/dev/null || \
    sed -i -E "/<key>CFBundleShortVersionString<\/key>/{n;s|<string>[^<]+</string>|<string>${NEW_VERSION}</string>|;}" "$INFO_PLIST"
    sed -i '' -E "/<key>CFBundleVersion<\/key>/{n;s|<string>[^<]+</string>|<string>${NEW_VERSION}</string>|;}" "$INFO_PLIST" 2>/dev/null || \
    sed -i -E "/<key>CFBundleVersion<\/key>/{n;s|<string>[^<]+</string>|<string>${NEW_VERSION}</string>|;}" "$INFO_PLIST"
    echo "  ✓ Updated installer/macos/Info.plist"
fi

# 4. Central Web Server Manifest (car_binary/storage/app/releases/version.json)
if [ -n "$CAR_BINARY_DIR" ] && [ -d "$CAR_BINARY_DIR/storage/app/releases" ]; then
    VERSION_JSON="$CAR_BINARY_DIR/storage/app/releases/version.json"
    cat <<JSONEOF > "$VERSION_JSON"
{
    "version": "${NEW_VERSION}",
    "min_version": "1.0.0",
    "mandatory": false,
    "release_date": "${RELEASE_DATE}",
    "notes": "${NOTES}",
    "files": {
        "win-x64": "ADIapp-Setup-${NEW_VERSION}.exe",
        "osx-x64": "ADIapp-macOS-${NEW_VERSION}.dmg",
        "osx-arm64": "ADIapp-macOS-${NEW_VERSION}.dmg",
        "linux-x64": "adiapp_${NEW_VERSION}_amd64.deb"
    }
}
JSONEOF
    echo "  ✓ Generated $VERSION_JSON"
fi

echo "=========================================="
echo "  All project & release files bumped to v${NEW_VERSION} successfully!"
echo "=========================================="
