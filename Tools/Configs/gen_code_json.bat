
set WORKSPACE=..\..
set LUBAN_DLL=%WORKSPACE%\Tools\Configs\Luban\Luban.dll
set CONF_ROOT=.

%LUBAN_DLL% ^
	 --conf %CONF_ROOT%\luban.conf ^
	 -x outputDataDir=%OUTPUT_DATA%\Client\json ^
	 -x outputCodeDir=%OUTPUT_CODE%\Client\csharp ^
	 -t all ^
	 -d json ^
	 -c cs-simple-json 


pause