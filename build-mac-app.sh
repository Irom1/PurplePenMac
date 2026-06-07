#!/bin/bash
# Build PurplePen as a macOS .app bundle
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
APP_NAME="PurplePen"
PUBLISH_DIR="$SCRIPT_DIR/src/AvPurplePen/bin/publish/osx-arm64"
APP_BUNDLE="$SCRIPT_DIR/$APP_NAME.app"

echo "=== Publishing self-contained macOS build ==="
cd "$SCRIPT_DIR/src"
dotnet publish AvPurplePen/AvPurplePen.csproj \
    -r osx-arm64 \
    --self-contained true \
    -c Release \
    -o "$PUBLISH_DIR"

echo "=== Creating .app bundle ==="
rm -rf "$APP_BUNDLE"
mkdir -p "$APP_BUNDLE/Contents/MacOS"
mkdir -p "$APP_BUNDLE/Contents/Resources"

# Copy everything into the bundle
cp -R "$PUBLISH_DIR"/* "$APP_BUNDLE/Contents/MacOS/"

# Copy icon
if [ -f "$SCRIPT_DIR/src/AvPurplePen/Assets/PurplePenIcon.png" ]; then
    cp "$SCRIPT_DIR/src/AvPurplePen/Assets/PurplePenIcon.png" "$APP_BUNDLE/Contents/Resources/PurplePen.icns"
fi

# Create Info.plist
cat > "$APP_BUNDLE/Contents/Info.plist" << 'PLIST'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key>
    <string>AvPurplePen</string>
    <key>CFBundleIdentifier</key>
    <string>com.purplepen.app</string>
    <key>CFBundleName</key>
    <string>PurplePen</string>
    <key>CFBundleDisplayName</key>
    <string>Purple Pen</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleVersion</key>
    <string>4.0.0</string>
    <key>CFBundleShortVersionString</key>
    <string>4.0.0</string>
    <key>NSHighResolutionCapable</key>
    <true/>
</dict>
</plist>
PLIST

echo ""
echo "=== Done ==="
echo "App bundle: $APP_BUNDLE"
echo "Launch:     open $APP_BUNDLE"
