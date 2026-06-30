#!/bin/zsh
WORKSPACE=../..

LUBAN_DLL=$WORKSPACE/Tools/Configs/Luban/Luban.dll
CONF_ROOT=.
OUTPUT_DATA=./Generate/Datas/s
OUTPUT_CODE=./Generate/Codes/s

dotnet ${LUBAN_DLL} \
    --conf ${CONF_ROOT}/luban.conf \
    -x outputCodeDir=${OUTPUT_CODE}/csharp \
    -x outputDataDir=${OUTPUT_DATA}/json \
    -t server \
    -d json \
    -c cs-dotnet-json
  



cp -r -f ${OUTPUT_DATA}/json/  ${WORKSPACE}/ServerRes/Configs
cp -r -f ${OUTPUT_CODE}/csharp/ ${WORKSPACE}/Server/Model/Server/Generated/Configs