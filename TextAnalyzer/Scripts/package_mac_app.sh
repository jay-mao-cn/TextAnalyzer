#!/bin/bash
# pass x86_64 or arm64 for arch, or left empty to get current system arch

if [ $# -eq 0 ]; then
    arch=$(uname -m)
else
    arch=$1
fi
echo "arch=$arch"

if [ "$arch" == "x86_64" ]; then
    PUBLISH_OUTPUT_DIRECTORY="./bin/Release/net8.0/osx-x64/publish/."
else
    PUBLISH_OUTPUT_DIRECTORY="./bin/Release/net8.0/osx-$arch/publish/."
fi

APP_NAME="Text Analyzer.app"
APP_PARENT_DIR="./bin/$arch"
if [ ! -d "$APP_PARENT_DIR" ]; then
    mkdir -p "$APP_PARENT_DIR"
fi
APP_DIR="$APP_PARENT_DIR/$APP_NAME"
APP_CONTENTS_DIR="$APP_DIR/Contents"
APP_BINARY_DIR="$APP_CONTENTS_DIR/MacOS"
APP_RESOURCE_DIR="$APP_CONTENTS_DIR/Resources"
# PUBLISH_OUTPUT_DIRECTORY should point to the output directory of your dotnet publish command.
# One example is /path/to/your/csproj/bin/Release/net8.0/osx-x64/publish/.
# If you want to change output directories, add `--output /my/directory/path` to your `dotnet publish` command.
INFO_PLIST="Info.plist"
ICON_FILE="TextAnalyzer.icns"
ICON_PATH="./Assets/$ICON_FILE"

if [ -d "$APP_DIR" ]
then
    rm -rf "$APP_DIR"
fi

mkdir "$APP_DIR"

mkdir "$APP_CONTENTS_DIR"
mkdir "$APP_BINARY_DIR"
mkdir "$APP_RESOURCE_DIR"

cp "./Mac/$INFO_PLIST" "$APP_CONTENTS_DIR/$INFO_PLIST"
cp "$ICON_PATH" "$APP_RESOURCE_DIR/$ICON_FILE"
cp -a "$PUBLISH_OUTPUT_DIRECTORY" "$APP_BINARY_DIR"
cp ./Mac/$arch/libMacOpenFileDelegate.dylib "$APP_BINARY_DIR"