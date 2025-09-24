#!/bin/bash
# pass x86_64 or arm64 for arch, or left empty to get current system arch

if [ $# -eq 0 ]; then
    arch=$(uname -m)
else
    arch=$1
fi


APP_NAME="./bin/$arch/Text Analyzer.app"
ENTITLEMENTS="./Mac/TextAnalyzer.entitlements"
SIGNING_IDENTITY="Apple Development: Jay Mao (C4ZAR8Y4RZ)" # matches Keychain Access certificate name

find "$APP_NAME/Contents/MacOS/"|while read fname; do
    if [[ -f $fname ]]; then
        echo "[INFO] Signing $fname"
        codesign --force --timestamp --options=runtime --entitlements "$ENTITLEMENTS" --sign "$SIGNING_IDENTITY" "$fname"
    fi
done

echo "[INFO] Signing app file"

codesign --force --timestamp --options=runtime --entitlements "$ENTITLEMENTS" --sign "$SIGNING_IDENTITY" "$APP_NAME"