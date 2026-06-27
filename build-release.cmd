@echo off
setlocal EnableExtensions
chcp 65001 >nul

cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 (
  echo [Error] dotnet was not found. Install .NET SDK 9.0 first.
  pause
  exit /b 1
)

echo ============================================
echo   系统观测台 - One-Click Release Build
echo ============================================
echo.

set "PROJECT=Windows\HwMonitor.csproj"
set "OUTDIR=publish-release"
set "OUTNAME=系统观测台-Release"
set "TAG=%OUTNAME%-v0.1.0"

echo [1/4] Building self-contained EXE...
dotnet publish %PROJECT% -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false -p:RestoreIgnoreFailedSources=true -o %OUTDIR%
if errorlevel 1 (
  echo Build failed.
  pause
  exit /b 1
)
echo   Done.
echo.

echo [2/4] Creating release package...
set "ZIP=%TAG%.zip"
powershell -NoProfile -Command "Compress-Archive -Path '%OUTDIR%\*' -DestinationPath '%ZIP%' -Force"
if errorlevel 1 (
  echo Failed to create zip.
  pause
  exit /b 1
)
echo   %ZIP% created.
echo.

REM Get version from csproj for notes
for /f "tokens=*" %%a in ('powershell -NoProfile -Command "Select-String -Path '%PROJECT%' -Pattern 'AssemblyName' | ForEach-Object { $_.Line.Split('<>')[1] }"') do set ASSEMBLY_NAME=%%a

echo [3/4] Uploading to GitHub...
gh release create %TAG% "%ZIP%" --title "%TAG%" --notes "自包含单文件版本，无需安装 .NET 运行时。解压后双击 系统观测台.exe 即可运行。" 2>&1
if errorlevel 1 (
  echo.
  echo GitHub upload failed. Check gh login status.
  echo You can manually upload: %ZIP%
  pause
  exit /b 1
)
echo   Release created: %TAG%
echo.

echo [4/4] Cleanup...
rmdir /s /q %OUTDIR% 2>nul
echo   Done.
echo.
echo ============================================
echo   Release build complete!
echo   Download: https://github.com/8763473/system-observatory-windows/releases/latest
echo ============================================
echo.
pause
