@echo off
setlocal enabledelayedexpansion

echo ========================================================
echo   Building ADI Application Windows Setup Installer (.exe)
echo ========================================================

cd /d "%~dp0..\.."

echo -> Publishing self-contained release (win-x64)...
dotnet publish ADIapp.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false
if %ERRORLEVEL% NEQ 0 (
    echo Error publishing dotnet app.
    exit /b %ERRORLEVEL%
)

if not exist "dist" mkdir "dist"

set "ISCC_PATH=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

if not exist "%ISCC_PATH%" (
    echo.
    echo Warning: Inno Setup compiler not found at %ISCC_PATH%
    echo Published self-contained binaries are ready in:
    echo   bin\Release\net9.0\win-x64\publish
    echo To build the .exe installer, download Inno Setup from https://jrsoftware.org/isinfo.php
    pause
    exit /b 0
)

echo -> Compiling Inno Setup wizard installer...
"%ISCC_PATH%" installer\windows\setup.iss
if %ERRORLEVEL% NEQ 0 (
    echo Error compiling Inno Setup script.
    exit /b %ERRORLEVEL%
)

echo ========================================================
echo Windows Setup Installer created successfully in dist\
echo ========================================================
dir dist\*.exe
pause
