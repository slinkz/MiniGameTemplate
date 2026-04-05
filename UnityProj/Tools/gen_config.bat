@echo off
setlocal

set LUBAN_CLIENT=luban
set CONF_ROOT=%~dp0..\DataTables
set OUTPUT_CODE=%~dp0..\Assets\_Framework\DataSystem\Scripts\Config\Generated
set OUTPUT_DATA=%~dp0..\Assets\_Framework\DataSystem\Resources\ConfigData

echo [Luban] Generating config tables...

%LUBAN_CLIENT% ^
    -t all ^
    -d %CONF_ROOT%\Defs\__root__.xml ^
    --input_data_dir %CONF_ROOT%\Datas ^
    --output_code_dir %OUTPUT_CODE% ^
    --output_data_dir %OUTPUT_DATA% ^
    --gen_types code_cs_unity_json,data_json ^
    -s all

if %ERRORLEVEL% NEQ 0 (
    echo [Luban] ERROR: Generation failed!
    pause
    exit /b 1
)

echo [Luban] Generation complete.
pause
