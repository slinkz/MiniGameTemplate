@echo off
setlocal

set SCRIPT_DIR=%~dp0
set LUBAN_DLL=%SCRIPT_DIR%Luban\Luban.dll
set CONF_ROOT=%SCRIPT_DIR%..\DataTables
set OUTPUT_CODE=%SCRIPT_DIR%..\Assets\_Framework\DataSystem\Scripts\Config\Generated
set OUTPUT_DATA_BIN=%SCRIPT_DIR%..\Assets\_Game\ConfigData
set OUTPUT_DATA_JSON=%SCRIPT_DIR%..\Assets\_Framework\Editor\ConfigPreview
set RESOURCES_BIN=%SCRIPT_DIR%..\Assets\_Framework\DataSystem\Resources\ConfigData

echo ============================================
echo  Luban v4.6 Config Generator (Dual Format)
echo ============================================
echo.

REM --- Step 1: Generate C# code (cs-bin) + Binary data ---
echo [Step 1/3] Generating C# code + Binary data...
dotnet "%LUBAN_DLL%" ^
    --conf "%CONF_ROOT%\luban.conf" ^
    -t all ^
    -c cs-bin ^
    -d bin ^
    -x outputCodeDir="%OUTPUT_CODE%" ^
    -x outputDataDir="%OUTPUT_DATA_BIN%"

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Binary generation failed!
    pause
    exit /b 1
)
echo [OK] Binary code + data generated.
echo.

REM --- Step 2: Generate JSON data (editor-only preview) ---
REM JSON files go to an Editor/ folder so they are excluded from builds.
echo [Step 2/3] Generating JSON data (editor-only preview)...
if not exist "%OUTPUT_DATA_JSON%" mkdir "%OUTPUT_DATA_JSON%"
dotnet "%LUBAN_DLL%" ^
    --conf "%CONF_ROOT%\luban.conf" ^
    -t all ^
    -d json ^
    -x outputDataDir="%OUTPUT_DATA_JSON%"

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] JSON generation failed!
    pause
    exit /b 1
)
echo [OK] JSON data generated for editor preview.
echo.

REM --- Step 3: Copy binary data to Resources for fallback loading ---
echo [Step 3/3] Copying binary data to Resources fallback...
if not exist "%RESOURCES_BIN%" mkdir "%RESOURCES_BIN%"
copy /Y "%OUTPUT_DATA_BIN%\*.bytes" "%RESOURCES_BIN%\" > nul
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [WARN] Binary copy to Resources failed! Check that .bytes files exist in %OUTPUT_DATA_BIN%
)
echo [OK] Binary data copied to Resources/ConfigData/.
echo.

echo ============================================
echo  All done!
echo  Code:       Assets\_Framework\DataSystem\Scripts\Config\Generated\
echo  Binary:     Assets\_Game\ConfigData\*.bytes          (YooAsset - runtime)
echo  Bin copy:   Assets\_Framework\DataSystem\Resources\ConfigData\*.bytes (Resources fallback)
echo  JSON:       Assets\_Framework\Editor\ConfigPreview\  (editor-only, excluded from builds)
echo ============================================
pause
