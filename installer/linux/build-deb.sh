#!/usr/bin/env bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
DIST_DIR="$PROJECT_ROOT/dist"

RID="${1:-linux-x64}"
VERSION="1.0.2"
APP_NAME="adiapp"
DEB_DIR="/tmp/${APP_NAME}_${VERSION}_amd64"

echo "=== Building Linux Self-Contained App for $RID ==="

cd "$PROJECT_ROOT"
dotnet publish ADIapp.csproj \
    -c Release \
    -r "$RID" \
    --self-contained true \
    -p:PublishSingleFile=false

echo "=== Assembling Debian Package Directory Structure ==="
rm -rf "$DEB_DIR"
mkdir -p "$DEB_DIR/usr/lib/$APP_NAME"
mkdir -p "$DEB_DIR/usr/bin"
mkdir -p "$DEB_DIR/usr/share/applications"
mkdir -p "$DEB_DIR/usr/share/icons/hicolor/256x256/apps"
mkdir -p "$DEB_DIR/DEBIAN"

# Copy published binaries to /usr/lib/adiapp/
cp -R "$PROJECT_ROOT/bin/Release/net9.0/$RID/publish/"* "$DEB_DIR/usr/lib/$APP_NAME/"

# Create symlink in /usr/bin/
ln -sf "/usr/lib/$APP_NAME/ADIapp" "$DEB_DIR/usr/bin/$APP_NAME"

# Copy icon
cp "$PROJECT_ROOT/Assets/login_logo.png" "$DEB_DIR/usr/share/icons/hicolor/256x256/apps/$APP_NAME.png"

# Create .desktop file
cat <<EOF > "$DEB_DIR/usr/share/applications/$APP_NAME.desktop"
[Desktop Entry]
Name=ADI Application
Comment=ADI Performance ECU & Chiptuning Management Client
Exec=/usr/bin/$APP_NAME
Icon=$APP_NAME
Terminal=false
Type=Application
Categories=Utility;Engineering;
EOF

# Create control file
cat <<EOF > "$DEB_DIR/DEBIAN/control"
Package: $APP_NAME
Version: $VERSION
Architecture: amd64
Maintainer: ADI Performance <contact@adi-performance.com>
Description: ADI Performance Desktop Client Application
 Cross-platform ECU identification, chiptuning, and ticket management client.
EOF

echo "=== Packaging DEB Package ==="
mkdir -p "$DIST_DIR"
FINAL_DEB="$DIST_DIR/${APP_NAME}_${VERSION}_amd64.deb"

dpkg-deb --build "$DEB_DIR" "$FINAL_DEB" || true
rm -rf "$DEB_DIR"

echo "=== Linux DEB Build Process Complete: $FINAL_DEB ==="
