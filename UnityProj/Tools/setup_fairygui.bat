@echo off
REM Setup FairyGUI SDK directory junction
REM Run this after cloning the repository

cd /d "%~dp0..\.."

echo Initializing git submodules...
git submodule update --init --recursive

cd /d "%~dp0.."

echo Creating FairyGUI directory junction...

if exist "Assets\FairyGUI" (
    REM Check if it's already a junction
    dir "Assets\FairyGUI" /AL >nul 2>&1
    if not errorlevel 1 (
        echo Junction Assets\FairyGUI already exists, skipping.
        goto done
    )
    REM It's a regular directory — confirm before deleting
    echo WARNING: Assets\FairyGUI exists as a regular directory (not a junction).
    echo It will be DELETED and replaced with a junction to ThirdParty\FairyGUI-unity\Assets.
    set /p CONFIRM="Are you sure you want to continue? (Y/N): "
    if /I not "%CONFIRM%"=="Y" (
        echo Aborted by user.
        goto done
    )
    echo Removing existing Assets\FairyGUI directory...
    rmdir /S /Q "Assets\FairyGUI"
)

mklink /J "Assets\FairyGUI" "ThirdParty\FairyGUI-unity\Assets"

:done
echo Done! FairyGUI SDK is ready.
pause
