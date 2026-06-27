@echo off
setlocal EnableExtensions EnableDelayedExpansion
chcp 65001 >nul

cd /d "%~dp0"

echo ============================================
echo   System Observatory - Public Tunnel Setup
echo ============================================
echo.

REM Check local server
echo [1/4] Checking local server on port 8787...
powershell -NoProfile -Command "try { $r = Invoke-WebRequest -UseBasicParsing -Uri 'http://127.0.0.1:8787/health' -TimeoutSec 5; if ($r.StatusCode -eq 200) { exit 0 } else { exit 1 } } catch { exit 1 }"
if errorlevel 1 (
  echo   Local server not running. Starting System Observatory...
  start "" "%~dp0系统观测台.exe"
  timeout /t 4 /nobreak >nul
  powershell -NoProfile -Command "try { $r = Invoke-WebRequest -UseBasicParsing -Uri 'http://127.0.0.1:8787/health' -TimeoutSec 5; if ($r.StatusCode -eq 200) { exit 0 } else { exit 1 } } catch { exit 1 }"
  if errorlevel 1 (
    echo   ERROR: Local server failed to start on port 8787.
    pause
    exit /b 1
  )
)
echo   Local server OK.
echo.

REM Kill old localtunnel
echo [2/4] Stopping old tunnel...
for /f "tokens=5" %%a in ('netstat -aon ^| findstr ":8787" ^| findstr "LISTENING"') do (
  REM Only kill node processes that might be lt
)
taskkill /f /im node.exe /fi "WINDOWTITLE eq localtunnel*" >nul 2>nul
echo   Done.
echo.

REM Start localtunnel and capture URL
echo [3/4] Starting localtunnel...
set "TUNNEL_URL="
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ErrorActionPreference='Stop'; $root='E:\personal\Desktop\Project\system-observatory'; $lt='C:\Users\Yue\AppData\Roaming\npm\lt.cmd'; $urlFile=Join-Path $root 'tunnel-url.txt'; $errFile=Join-Path $root 'tunnel-err.txt'; $p = Start-Process -FilePath $lt -ArgumentList '--port','8787' -RedirectStandardOutput $urlFile -RedirectStandardError $errFile -NoNewWindow -PassThru; Start-Sleep -Seconds 8; if (Test-Path $urlFile) { $content = Get-Content $urlFile -Raw; if ($content -match 'https://[a-z0-9-]+\.loca\.lt') { $matches[0] | Out-File (Join-Path $root 'current-tunnel.txt') -Encoding UTF8; Write-Host $matches[0] } else { Write-Host 'FAILED' } } else { Write-Host 'FAILED' }"

for /f "delims=" %%a in ('type tunnel-url.txt 2^>nul ^| findstr "https://"') do (
  set "TUNNEL_URL=%%a"
)

if "!TUNNEL_URL!"=="" (
  for /f "delims=" %%a in ('type current-tunnel.txt 2^>nul') do (
    set "TUNNEL_URL=%%a"
  )
)

if "!TUNNEL_URL!"=="" (
  echo   ERROR: Failed to get tunnel URL.
  echo   Check tunnel-err.txt for details.
  pause
  exit /b 1
)

echo   Tunnel URL: !TUNNEL_URL!
echo.

REM Update settings.ini
echo [4/4] Updating settings...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$root='E:\personal\Desktop\Project\system-observatory'; $ini=Join-Path $root 'settings.ini'; $lines=Get-Content $ini; $url='!TUNNEL_URL!'; $updated=$false; for ($i=0;$i -lt $lines.Count;$i++) { if ($lines[$i] -match '^relayUrl=') { $lines[$i]='relayUrl='+$url; $updated=$true } }; if ($updated) { $lines | Set-Content $ini -Encoding UTF8; Write-Host 'Settings updated.' } else { Write-Host 'relayUrl not found in settings.ini' }"

echo.
echo ============================================
echo   Tunnel is running!
echo.
echo   Public URL: !TUNNEL_URL!
echo.
echo   Android settings:
echo     Address: !TUNNEL_URL!
echo     Key:     (see Windows app settings)
echo.
echo   Keep this window open to maintain tunnel.
echo   Closing it will disconnect Android.
echo ============================================
echo.
echo Press Ctrl+C to stop the tunnel.
echo.

REM Keep window open
cmd /k
