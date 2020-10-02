#!/bin/bash

cd mono
git pull
make clean
make -j -C sdks/builds provision-wasm
make -j -C sdks/wasm runtime
make -j -C sdks/wasm bcl
make -j -C sdks/wasm cross
make -j -C sdks/wasm
#make -j -C sdks/wasm runtime-threads #threading support
#make -j -C sdks/wasm runtime-dynamic # dynamic linking support
