@echo off
REM Setup FairyGUI SDK directory junctions
REM Run this after cloning the repository and initializing submodules

cd /d "%~dp0..\.."

echo Initializing git submodules...
git submodule update --init --recursive

cd /d "%~dp0.."

echo Creating FairyGUI directory junctions...

if not exist "Assets\FairyGUI" mkdir "Assets\FairyGUI"

if exist "Assets\FairyGUI\Scripts" (
    echo Junction Assets\FairyGUI\Scripts already exists, skipping.
) else (
    mklink /J "Assets\FairyGUI\Scripts" "ThirdParty\FairyGUI-unity\Assets\Scripts"
)

if exist "Assets\FairyGUI\Editor" (
    echo Junction Assets\FairyGUI\Editor already exists, skipping.
) else (
    mklink /J "Assets\FairyGUI\Editor" "ThirdParty\FairyGUI-unity\Assets\Editor"
)

if exist "Assets\FairyGUI\Resources" (
    echo Junction Assets\FairyGUI\Resources already exists, skipping.
) else (
    mklink /J "Assets\FairyGUI\Resources" "ThirdParty\FairyGUI-unity\Assets\Resources"
)

echo Done! FairyGUI SDK is ready.
pause
