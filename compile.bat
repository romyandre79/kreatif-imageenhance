@echo off
title Build Fotocopy EXE
cd /d "%~dp0"

set "MSBUILD="C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe""

echo Memulai kompilasi Fotocopy (Release)...
%MSBUILD% Fotocopy.csproj /p:Configuration=Release /t:Rebuild /v:minimal

if errorlevel 1 (
    echo.
    echo Kompilasi GAGAL!
    pause
    exit /b 1
)

echo.
echo ========================================================
echo [SUKSES] File EXE berhasil dibuat di:
echo bin\Release\Fotocopy.exe
echo ========================================================
echo.
pause
