@echo off
setlocal EnableExtensions

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "UNITY_PROJ_DIR=%%~fI"
set "LUBAN_DLL=%SCRIPT_DIR%Luban\Luban.dll"
set "CONF_ROOT=%UNITY_PROJ_DIR%\DataTables"
set "OUTPUT_CODE=%UNITY_PROJ_DIR%\Assets\_Framework\DataSystem\Scripts\Config\Generated"
set "OUTPUT_DATA_BIN=%UNITY_PROJ_DIR%\Assets\_Game\ConfigData"
set "OUTPUT_DATA_JSON=%UNITY_PROJ_DIR%\Assets\_Framework\Editor\ConfigPreview"

cd /d "%UNITY_PROJ_DIR%"

echo ============================================
echo  Luban v4.6 Config Generator
echo ============================================
echo.

if not exist "%LUBAN_DLL%" (
    echo [ERROR] Luban.dll not found: %LUBAN_DLL%
    goto fail
)

if not exist "%CONF_ROOT%\luban.conf" (
    echo [ERROR] luban.conf not found: %CONF_ROOT%\luban.conf
    goto fail
)

REM --- Step 1: Generate C# code (cs-bin) + Binary data ---
echo [Step 1/2] Generating C# code + Binary data...
dotnet "%LUBAN_DLL%" ^
    --conf "%CONF_ROOT%\luban.conf" ^
    -t all ^
    -c cs-bin ^
    -d bin ^
    -x outputCodeDir="%OUTPUT_CODE%" ^
    -x outputDataDir="%OUTPUT_DATA_BIN%"

if errorlevel 1 (
    echo.
    echo [ERROR] Binary generation failed!
    goto fail
)
echo [OK] Binary code + data generated.
echo.

REM --- Step 2: Generate JSON data (editor-only preview) ---
REM JSON files go to an Editor/ folder so they are excluded from builds.
echo [Step 2/2] Generating JSON data (editor-only preview)...
if not exist "%OUTPUT_DATA_JSON%" mkdir "%OUTPUT_DATA_JSON%"
dotnet "%LUBAN_DLL%" ^
    --conf "%CONF_ROOT%\luban.conf" ^
    -t all ^
    -d json ^
    -x outputDataDir="%OUTPUT_DATA_JSON%"

if errorlevel 1 (
    echo.
    echo [ERROR] JSON generation failed!
    goto fail
)
echo [OK] JSON data generated for editor preview.
echo.

echo ============================================
echo  All done!
echo  Code:       Assets\_Framework\DataSystem\Scripts\Config\Generated\
echo  Binary:     Assets\_Game\ConfigData\*.bytes          (YooAsset - runtime)
echo  JSON:       Assets\_Framework\Editor\ConfigPreview\  (editor-only, excluded from builds)
echo ============================================
pause
endlocal
exit /b 0

:fail
pause
endlocal
exit /b 1
