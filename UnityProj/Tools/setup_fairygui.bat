@echo off
setlocal EnableExtensions
REM Setup FairyGUI SDK directory junction
REM Run this after cloning the repository

set SCRIPT_DIR=%~dp0
powershell -ExecutionPolicy Bypass -File "%SCRIPT_DIR%setup_fairygui.ps1" %*
set EXIT_CODE=%ERRORLEVEL%
endlocal & exit /b %EXIT_CODE%
