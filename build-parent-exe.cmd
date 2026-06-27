@echo off
setlocal EnableExtensions
chcp 65001 >nul

cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 (
  echo [Error] dotnet was not found. Install .NET SDK, then double-click this file again.
  echo.
  if /I not "%NO_PAUSE%"=="1" pause
  exit /b 1
)

echo Building System Observatory parent EXE...
echo.

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "& { $ErrorActionPreference = 'Stop'; $root = (Resolve-Path '.').Path; $rootWithSlash = [IO.Path]::GetFullPath($root).TrimEnd('\') + '\'; $project = Join-Path $root 'Windows\HwMonitor.csproj'; $publish = Join-Path $root 'publish'; $target = Join-Path $root '系统观测台.exe'; $backupDir = Join-Path $root 'backups'; New-Item -ItemType Directory -Force -Path $backupDir | Out-Null; dotnet publish $project -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false -p:RestoreIgnoreFailedSources=true -o $publish; if ($LASTEXITCODE -ne 0) { throw ('dotnet publish failed with exit code ' + $LASTEXITCODE) }; $source = Join-Path $publish '系统观测台.exe'; if (!(Test-Path -LiteralPath $source)) { throw ('Published EXE not found: ' + $source) }; $targetFull = [IO.Path]::GetFullPath($target); $running = Get-CimInstance Win32_Process -Filter 'Name = ''系统观测台.exe''' | Where-Object { $_.ExecutablePath -and ([IO.Path]::GetFullPath($_.ExecutablePath) -ieq $targetFull) }; foreach ($process in $running) { Stop-Process -Id $process.ProcessId -Force -ErrorAction Stop }; if (Test-Path -LiteralPath $target) { $stamp = Get-Date -Format 'yyyyMMdd_HHmmss'; $backup = Join-Path $backupDir ('system_observatory_before_auto_publish_' + $stamp + '.exe'); Copy-Item -LiteralPath $target -Destination $backup -Force; Remove-Item -LiteralPath $target -Force }; Copy-Item -LiteralPath $source -Destination $target -Force; $targetHash = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash; $sourceHash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash; if ($targetHash -ne $sourceHash) { throw 'Copied EXE hash mismatch' }; foreach ($name in @('Windows\bin','Windows\obj','publish')) { $path = Join-Path $root $name; if (Test-Path -LiteralPath $path) { $full = [IO.Path]::GetFullPath($path); if (-not $full.StartsWith($rootWithSlash, [StringComparison]::OrdinalIgnoreCase)) { throw ('Cleanup path outside project root: ' + $full) }; Remove-Item -LiteralPath $path -Recurse -Force } }; Write-Host ('Parent EXE published: ' + $target) }"

if errorlevel 1 (
  echo.
  echo Build failed.
  if /I not "%NO_PAUSE%"=="1" pause
  exit /b 1
)

echo.
echo Build complete.
if /I not "%NO_PAUSE%"=="1" pause
