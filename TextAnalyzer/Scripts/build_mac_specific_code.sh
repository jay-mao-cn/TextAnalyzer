#!/bin/bash

# args: out_lib_name, src_file, arch
function build()
{
    arch=$3
    OUT_LIB_DIR="Mac/$arch"
    if [ ! -d "$OUT_LIB_DIR" ]; then
        mkdir -p "$OUT_LIB_DIR"
    fi

    LIB_NAME=$1
    OUT_LIB_PATH="$OUT_LIB_DIR/$LIB_NAME"
    SRC_FILE="Mac/$2"
    clang -O2 -DNDEBUG -arch $arch -framework Cocoa -dynamiclib $SRC_FILE -o $OUT_LIB_PATH

    real_arch=$(uname -m)
    if [[ "$real_arch" == "$arch" ]]; then
        cp $OUT_LIB_PATH bin/Debug/net8.0/
        cp $OUT_LIB_PATH bin/Release/net8.0/
    fi
}

function build_all_archs()
{
    build $1 $2 x86_64
    build $1 $2 arm64
}

build_all_archs libMacOpenFileDelegate.dylib MacOpenFileDelegate.m
