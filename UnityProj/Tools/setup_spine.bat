@echo off
setlocal EnableExtensions
REM Setup Spine runtime source links for Unity project
REM Run this after cloning the repository

set NO_PAUSE=
set FORCE=
if /I "%~1"=="--no-pause" set NO_PAUSE=1
if /I "%~2"=="--no-pause" set NO_PAUSE=1
if /I "%~1"=="--force" set FORCE=1
if /I "%~2"=="--force" set FORCE=1

cd /d "%~dp0..\.."

echo Initializing git submodules...
git submodule update --init --recursive UnityProj/ThirdParty/spine-runtimes
if errorlevel 1 (
    echo ERROR: Failed to initialize Spine submodule.
    goto end
)

cd /d "%~dp0.."

call :EnsureJunction "Assets\Spine" "ThirdParty\spine-runtimes\spine-unity\Assets\Spine"
if errorlevel 1 goto end

call :EnsureJunction "Assets\SpineCSharp" "ThirdParty\spine-runtimes\spine-csharp\src"
if errorlevel 1 goto end

echo Done! Spine source links are ready.
echo Next step: enable FAIRYGUI_SPINE define from Unity menu if needed.
goto end

:EnsureJunction
set TARGET=%~1
set SOURCE=%~2

echo.
echo Preparing %TARGET% -> %SOURCE%

if not exist "%SOURCE%\NUL" (
    echo ERROR: Source path does not exist: %SOURCE%
    exit /b 1
)

if exist "%TARGET%" (
    fsutil reparsepoint query "%TARGET%" >nul 2>&1
    if not errorlevel 1 (
        echo Junction/symlink %TARGET% already exists, skipping.
        exit /b 0
    )

    echo WARNING: %TARGET% exists as a regular directory (not a link).
    echo It will be DELETED and replaced with a junction.
    if not defined FORCE (
        set /p CONFIRM="Continue? (Y/N): "
        if /I not "%CONFIRM%"=="Y" (
            echo Aborted by user.
            exit /b 1
        )
    )

    rmdir /S /Q "%TARGET%"
    if errorlevel 1 (
        echo ERROR: Failed to remove existing directory: %TARGET%
        exit /b 1
    )
)

mklink /J "%TARGET%" "%SOURCE%"
if errorlevel 1 (
    echo ERROR: Failed to create junction: %TARGET%
    exit /b 1
)

exit /b 0

:end
if not defined NO_PAUSE pause
endlocal
