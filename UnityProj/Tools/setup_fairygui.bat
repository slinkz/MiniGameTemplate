@echo off
setlocal EnableExtensions
REM Setup FairyGUI SDK directory junction
REM Run this after cloning the repository

set NO_PAUSE=
set FORCE=
if /I "%~1"=="--no-pause" set NO_PAUSE=1
if /I "%~2"=="--no-pause" set NO_PAUSE=1
if /I "%~1"=="--force" set FORCE=1
if /I "%~2"=="--force" set FORCE=1

cd /d "%~dp0..\.."

echo Initializing git submodules...
git submodule update --init --recursive UnityProj/ThirdParty/FairyGUI-unity
if errorlevel 1 (
    echo ERROR: Failed to initialize FairyGUI submodule.
    goto done
)

cd /d "%~dp0.."

echo Creating FairyGUI directory junction...

if not exist "ThirdParty\FairyGUI-unity\Assets\NUL" (
    echo ERROR: Source path does not exist: ThirdParty\FairyGUI-unity\Assets
    goto done
)

if exist "Assets\FairyGUI" (
    fsutil reparsepoint query "Assets\FairyGUI" >nul 2>&1
    if not errorlevel 1 (
        echo Junction/symlink Assets\FairyGUI already exists, skipping.
        goto done
    )
    echo WARNING: Assets\FairyGUI exists as a regular directory (not a junction).
    echo It will be DELETED and replaced with a junction to ThirdParty\FairyGUI-unity\Assets.
    if not defined FORCE (
        set /p CONFIRM="Are you sure you want to continue? (Y/N): "
        if /I not "%CONFIRM%"=="Y" (
            echo Aborted by user.
            goto done
        )
    )
    echo Removing existing Assets\FairyGUI directory...
    rmdir /S /Q "Assets\FairyGUI"
    if errorlevel 1 (
        echo ERROR: Failed to remove existing Assets\FairyGUI directory.
        goto done
    )
)

mklink /J "Assets\FairyGUI" "ThirdParty\FairyGUI-unity\Assets"
if errorlevel 1 (
    echo ERROR: Failed to create junction Assets\FairyGUI.
    goto done
)

echo Done! FairyGUI SDK is ready.

:done
if not defined NO_PAUSE pause
endlocal
