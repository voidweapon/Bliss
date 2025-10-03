#!/bin/zsh
WORKSPACE=../..

PROTO_GEN_DLL=$WORKSPACE/Tools/ProtoGenerator/Bin/ProtoGenerator.dll
PRPTO_ROOT=./Proto
OUTPUT_CODE=./Generate/Codes

dotnet ${PROTO_GEN_DLL} 	\
    -p ${PRPTO_ROOT} 		\
    -o ${OUTPUT_CODE}/csharp/ 	\
    -c cs_memorygepack 		\
    -g Sequence 		\
    -m Specific 		\
    --offset_min 100000 	\
    --offset_max 200000 	\

  
#cp -r -f ${OUTPUT_CODE}/csharp/ ${WORKSPACE}/Unity/Assets/Scripts/Model/Generate/Message