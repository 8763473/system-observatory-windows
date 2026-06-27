@echo off
setlocal EnableExtensions
chcp 65001 >nul

cd /d "%~dp0"

where powershell >nul 2>nul
if errorlevel 1 (
  echo [Error] PowerShell was not found.
  echo.
  pause
  exit /b 1
)

echo Watching parent project files...
echo Close this window to stop automatic builds.
echo.

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "& { $ErrorActionPreference = 'Stop'; $root = (Resolve-Path '.').Path; $targets = @('Windows\HwMonitor.csproj','Windows\Program.cs','Windows\Hardware','Windows\Models','Windows\Settings','Windows\UI','Windows\Utilities'); function Get-Stamp { $files = foreach ($target in $targets) { $path = Join-Path $root $target; if (Test-Path -LiteralPath $path -PathType Leaf) { Get-Item -LiteralPath $path } elseif (Test-Path -LiteralPath $path -PathType Container) { Get-ChildItem -LiteralPath $path -Recurse -File | Where-Object { $_.Extension -in @('.cs','.csproj') } } }; if (-not $files) { return 0 }; return (($files | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1).LastWriteTimeUtc.Ticks) }; function Invoke-ParentBuild { Write-Host ('[AutoBuild] ' + (Get-Date -Format 'HH:mm:ss') + ' building parent exe...'); $env:NO_PAUSE = '1'; & (Join-Path $root 'build-parent-exe.cmd'); if ($LASTEXITCODE -ne 0) { Write-Host '[AutoBuild] build failed.' -ForegroundColor Red; if ($env:WATCH_ONCE -eq '1') { exit $LASTEXITCODE } } else { Write-Host '[AutoBuild] build finished.' -ForegroundColor Green } }; $last = Get-Stamp; Invoke-ParentBuild; if ($env:WATCH_ONCE -eq '1') { return }; while ($true) { Start-Sleep -Seconds 2; $next = Get-Stamp; if ($next -ne $last) { Write-Host '[AutoBuild] change detected.'; Start-Sleep -Seconds 1; Invoke-ParentBuild; $last = Get-Stamp } } }"

if errorlevel 1 (
  echo.
  echo Auto build watcher failed.
  if /I not "%NO_PAUSE%"=="1" pause
  exit /b 1
)

echo.
echo Auto build watcher stopped.
if /I not "%NO_PAUSE%"=="1" pause
