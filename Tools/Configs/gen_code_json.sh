#!/bin/zsh
WORKSPACE=../..

LUBAN_DLL=$WORKSPACE/Tools/Configs/Luban/Luban.dll
CONF_ROOT=.
OUTPUT_DATA=./Generate/Datas
OUTPUT_CODE=./Generate/Codes

dotnet ${LUBAN_DLL} \
    --conf ${CONF_ROOT}/luban.conf \
    -x outputCodeDir=${OUTPUT_CODE}/csharp \
    -x outputDataDir=${OUTPUT_DATA}/json \
    -t all \
    -d json \
    -c cs-simple-json
  



#cp -r -f ${OUTPUT_DATA}/json/  ${WORKSPACE}/Unity/Assets/Example/Res/Configs
#cp -r -f ${OUTPUT_CODE}/csharp/ ${WORKSPACE}/Unity/Assets/Example/Scripts/Configs