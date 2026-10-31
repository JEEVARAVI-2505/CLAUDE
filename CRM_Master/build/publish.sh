#!/usr/bin/env bash
# Cross-publish the Windows app from Linux/macOS (no installer - Inno Setup needs Windows). Usage: build/publish.sh [version]
set -euo pipefail
cd "$(dirname "$0")/.."
VERSION="${1:-1.0.0}"
dotnet --version
dotnet test CRM_Master.sln -c Release
rm -rf dist
dotnet publish CRM_Master.App -c Release -r win-x64 --self-contained true -p:Version="$VERSION" -p:DebugType=none -p:DebugSymbols=false -o dist/publish
[ -f CRM_Master.App/branding.json ] && cp CRM_Master.App/branding.json dist/publish/
test -f dist/publish/CRM_Master.exe && file dist/publish/CRM_Master.exe
(cd dist/publish && zip -qr "../CRM_MASTER_portable_${VERSION}.zip" .)
ls -lh dist/*.zip
