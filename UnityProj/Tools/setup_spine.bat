@echo off
setlocal EnableExtensions
REM Setup Spine runtime source links for Unity project
REM Run this after cloning the repository

set SCRIPT_DIR=%~dp0
powershell -ExecutionPolicy Bypass -File "%SCRIPT_DIR%setup_spine.ps1" %*
set EXIT_CODE=%ERRORLEVEL%
endlocal & exit /b %EXIT_CODE%
