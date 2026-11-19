@echo off
setlocal
title Build Fotocopy EXE

cd /d "%~dp0"

echo ========================================================
echo       KOMPILASI APLIKASI FOTOCOPY MENJADI EXE
echo ========================================================
echo.

set "MSBUILD_PATH="

:: 1. Cari via vswhere
if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" (
    for /f "usebackq tokens=*" %%i in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do (
        if not defined MSBUILD_PATH set "MSBUILD_PATH=%%i"
    )
)

:: 2. Fallback path umum VS 2022
if not defined MSBUILD_PATH (
    if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" (
        set "MSBUILD_PATH=%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
    ) else if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" (
        set "MSBUILD_PATH=%ProgramFiles%\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
    ) else if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" (
        set "MSBUILD_PATH=%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
    )
)

:: 3. Fallback path umum VS 2019
if not defined MSBUILD_PATH (
    if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" (
        set "MSBUILD_PATH=%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
    ) else if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe" (
        set "MSBUILD_PATH=%ProgramFiles(x86)%\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
    )
)

:: 4. Fallback .NET Framework bawaan Windows
if not defined MSBUILD_PATH (
    if exist "%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe" (
        set "MSBUILD_PATH=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
    )
)

if not defined MSBUILD_PATH (
    echo [ERROR] MSBuild tidak ditemukan!
    echo Pastikan Visual Studio atau .NET Framework SDK sudah terpasang.
    echo.
    pause
    endlocal
    exit /b 1
)

echo Menggunakan MSBuild:
echo "%MSBUILD_PATH%"
echo.

set "CONFIG=Release"
if /i "%~1"=="debug" set "CONFIG=Debug"

echo Memulai kompilasi (Configuration: %CONFIG%)...
echo.

"%MSBUILD_PATH%" Fotocopy.csproj /p:Configuration=%CONFIG% /t:Clean;Rebuild /v:minimal

if errorlevel 1 (
    echo.
    echo ========================================================
    echo [GAGAL] Terjadi kesalahan saat proses kompilasi!
    echo ========================================================
    pause
    endlocal
    exit /b 1
)

echo.
echo ========================================================
echo [SUKSES] Kompilasi berhasil!
echo File EXE berada di:
echo %~dp0bin\%CONFIG%\Fotocopy.exe
echo ========================================================
echo.

set /p OPEN_DIR="Buka folder hasil kompilasi sekarang? (Y/N, default Y): "
if /i "%OPEN_DIR%"=="N" goto selesai
explorer "%~dp0bin\%CONFIG%"

:selesai
pause
endlocal
