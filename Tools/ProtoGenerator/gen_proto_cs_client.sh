#!/bin/zsh
WORKSPACE=../..

PROTO_GEN_DLL=$WORKSPACE/Tools/ProtoGenerator/Bin/ProtoGenerator.dll
PRPTO_ROOT=./Proto/Share
OUTPUT_CODE=./Generate/Codes

dotnet ${PROTO_GEN_DLL} 	\
    -p ${PRPTO_ROOT} 		\
    -o ${OUTPUT_CODE}/csharp/Share 	\
    -c cs_memorygepack 		\
    -g Sequence 		\
    -m Specific 		\
    --offset_min 1000 		\
    --offset_max 2000 		\

  
cp -r -f ${OUTPUT_CODE}/csharp/Share ${WORKSPACE}/Unity/Assets/Scripts/Model/Shard/Generated/Message