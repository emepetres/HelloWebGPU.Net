#!/bin/bash
set -e

echo "Packaging to WebAssembly..."

MONO_SDK=$HOME/mono/sdks/out
WASM_SDK=$HOME/mono/sdks/wasm

source $WASM_SDK/emsdk_env.sh

BIN_PATH=$1
BUILD_PATH=$2
DLL=$3

echo "-------------- PARAMETERS ------------------"
echo "BIN_PATH ${BIN_PATH}"
echo "BUILD_PATH ${BUILD_PATH}"
echo "DLL ${DLL}"
echo "--------------------------------------------"

shift 3

LIBS_EXT="o"
OUTPUT_PATH=$BIN_PATH/../wasm

DEBUG=""
LINKER=""
COPY_PDB_IF_DEBUG=""
EXEC_MODE=""
FLAGS=""
REMOVE_PDB_IF_RELEASE="rm -f *.pdb"


rm -rf $BUILD_PATH

mkdir -p $BUILD_PATH
cp -r $BIN_PATH/* $BUILD_PATH
cp $BUILD_PATH/*.$LIBS_EXT $BUILD_PATH/managed/
TEMPLATE=$BUILD_PATH/runtime.js

while getopts ":aydlw" opt; do
	case ${opt} in
		a )
		  echo "Compiling in aot mode.."
			EXEC_MODE="--aot"
			;;
		y )
		  echo "Compiling in aot+interpreted mode.."
			EXEC_MODE="--aot-interp"
			;;
		d )
			echo "Debug mode activated.."
			DEBUG="--debug --debugrt"
			REMOVE_PDB_IF_RELEASE=""
			COPY_PDB_IF_DEBUG="cp -f $BUILD_PATH/managed/*.pdb $OUTPUT_PATH/managed"
			;;
		l )
		  echo "Linker activated.."
			LINKER="--linker"
			;;
		w )
		  echo "WebGL support activated.."
			FLAGS="--extra-emcc-flags=-s \"FULL_ES3=1\""
			;;
	esac
done
shift $((OPTIND -1))

#FLAGS="--extra-emcc-flags=--ignore-dynamic-linking"

if [ -z "$EXEC_MODE" ] # check if we are in interpreted mode
then
	echo "Compiling in interpreted mode..."
fi

STATIC_LIBS=""
PINVOKE_LIBS=""
for l in $BUILD_PATH/*.$LIBS_EXT; do
	[ -f "$l" ] || break
	file=${l##*/}
	name=${file%.*}

	STATIC_LIBS=$STATIC_LIBS" --native-lib="$file
	PINVOKE_LIBS=$PINVOKE_LIBS$name","
done

if [[ ! -z "$STATIC_LIBS" ]]
then
	echo "Static libs to link: ${PINVOKE_LIBS::-1}"
	PINVOKE_LIBS="--pinvoke-libs=${PINVOKE_LIBS::-1}"
fi
echo $PINVOKE_LIBS
echo $STATIC_LIBS

cd $BUILD_PATH/managed

# delete unwanted dlls
rm -f mscorlib.*
rm -f WebAssembly.Bindings.*
rm -f WebAssembly.Net.WebSockets.*
eval $REMOVE_PDB_IF_RELEASE

# package code
echo "Configuring interpreted code.."
mono $WASM_SDK/packager.exe --copy=always $DEBUG --out=$BUILD_PATH/publish $LINKER --link-descriptor=$BUILD_PATH/aot-link-descriptor.xml $DLL > /dev/null
TEMPLATE=$BUILD_PATH/publish/runtime.js
echo "Configuring.."
echo mono $WASM_SDK/packager.exe --emscripten-sdkdir=$EMSDK --enable-fs $FLAGS --mono-sdkdir=$MONO_SDK $EXEC_MODE --appdir=$BUILD_PATH/bin --builddir=$BUILD_PATH/obj --template=$TEMPLATE $DEBUG $LINKER --link-descriptor=$BUILD_PATH/aot-link-descriptor.xml $PINVOKE_LIBS $STATIC_LIBS $DLL
mono $WASM_SDK/packager.exe --emscripten-sdkdir=$EMSDK --enable-fs $FLAGS --mono-sdkdir=$MONO_SDK $EXEC_MODE --appdir=$BUILD_PATH/bin --builddir=$BUILD_PATH/obj --template=$TEMPLATE $DEBUG $LINKER --link-descriptor=$BUILD_PATH/aot-link-descriptor.xml $PINVOKE_LIBS $STATIC_LIBS $DLL

# echo "Add WebGL support..."
# sed -i 's/-s FORCE_FILESYSTEM=1/-s FORCE_FILESYSTEM=1 -s FULL_ES3=1/g' $BUILD_PATH/obj/build.ninja

echo "Add WebGPU support..."
sed -i 's/-s FORCE_FILESYSTEM=1/-s FORCE_FILESYSTEM=1 -s LLD_REPORT_UNDEFINED -s USE_WEBGPU=1 -s ENVIRONMENT=web/g' $BUILD_PATH/obj/build.ninja

echo "Compiling & linking.."
cp $BUILD_PATH/*.$LIBS_EXT $BUILD_PATH/obj/
ninja -v -C $BUILD_PATH/obj > $BUILD_PATH/ninja.log
code=$?
if [ $code -ne 0 ]
then
	echo "Errors compiling aot code, check $BUILD_PATH/ninja.log"
	exit $code
fi

# deploy
echo "Deploying.."
rm -rf $OUTPUT_PATH
cp -r $BIN_PATH $OUTPUT_PATH
rm -r $OUTPUT_PATH/managed
cd $BUILD_PATH/bin
cp -r ./managed $OUTPUT_PATH/
cp *.js $OUTPUT_PATH/
cp *.wasm $OUTPUT_PATH/
eval $COPY_PDB_IF_DEBUG

# debug runtime.js
if [ -n "${DEBUG}" ]; then
    sed -i '/^\tonRuntimeInitialized: function () {/a \\t\tvar wasm_setenv = Module.cwrap("mono_wasm_setenv", "void", ["string", "string"]);\n\t\twasm_setenv("MONO_LOG_LEVEL", "debug");\n\t\twasm_setenv("MONO_LOG_MASK", "all");' $OUTPUT_PATH/runtime.js
fi

# WebGPU fix
echo "Fixing wgpu javascript..."
sed -i 's/wgpuComputePipelineGetBindGroupLayout(/function _wgpuComputePipelineGetBindGroupLayout(/g' $OUTPUT_PATH/dotnet.js
sed -i 's/wgpuRenderPipelineGetBindGroupLayout(/function _wgpuRenderPipelineGetBindGroupLayout(/g' $OUTPUT_PATH/dotnet.js
sed -i 's/queue\["writeBuffer"\](buffer, bufferOffset, HEAPU8, data, size);/queue["writeBuffer"](buffer, bufferOffset, HEAPU8.buffer, data, size);/g' $OUTPUT_PATH/dotnet.js

echo "Compilation finished succesfully"
