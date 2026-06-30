#!/bin/zsh
WORKSPACE=../..

PROTO_GEN_DLL=$WORKSPACE/Tools/ProtoGenerator/Bin/ProtoGenerator.dll
PRPTO_ROOT=./Proto/Server
OUTPUT_CODE=./Generate/Codes

dotnet ${PROTO_GEN_DLL} 	\
    -p ${PRPTO_ROOT} 		\
    -o ${OUTPUT_CODE}/csharp/Server 	\
    -c cs_memorygepack 		\
    -g Sequence 		\
    -m Specific 		\
    --offset_min 20000		\
    --offset_max 30000 		\

  
cp -r -f ${OUTPUT_CODE}/csharp/Server ${WORKSPACE}/Server/Model/Server/Generated/Message