param(
    [string]$Root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
)

$ErrorActionPreference = 'Stop'

function Require-Text {
    param(
        [string]$Path,
        [string]$Pattern,
        [string]$Message
    )

    $fullPath = Join-Path $Root $Path
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw "Missing file: $Path"
    }

    $text = Get-Content -LiteralPath $fullPath -Raw
    if ($text -notmatch $Pattern) {
        throw $Message
    }
}

function Forbid-Text {
    param(
        [string]$Path,
        [string]$Pattern,
        [string]$Message
    )

    $fullPath = Join-Path $Root $Path
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw "Missing file: $Path"
    }

    $text = Get-Content -LiteralPath $fullPath -Raw
    if ($text -match $Pattern) {
        throw $Message
    }
}

Require-Text 'UI\UiTypes.cs' 'ConfigureAndroidLink' 'MemoryToolsAction must include ConfigureAndroidLink.'
Require-Text 'UI\UiTypes.cs' 'CopyAndroidDeviceKey' 'MemoryToolsAction must include CopyAndroidDeviceKey.'
Require-Text 'UI\UiTypes.cs' 'AndroidLink' 'MonitorCard must include AndroidLink for hover handling.'
Require-Text 'UI\MonitorRenderer.cs' 'AndroidLinkCardRect' 'MonitorRenderer must define an Android link card rectangle.'
Require-Text 'UI\MonitorRenderer.cs' 'AndroidLinkActionRect' 'MonitorRenderer must define an Android link action rectangle.'
Require-Text 'UI\MonitorRenderer.cs' 'AndroidDeviceKeyValueRect' 'MonitorRenderer must define the device-key value area.'
Require-Text 'UI\MonitorRenderer.cs' 'MemoryToolsAction\.ConfigureAndroidLink' 'Toolbox hit testing must return ConfigureAndroidLink.'
Require-Text 'UI\MonitorRenderer.cs' 'MemoryToolsAction\.CopyAndroidDeviceKey' 'Toolbox hit testing must return CopyAndroidDeviceKey.'
Require-Text 'UI\MonitorRenderer.cs' 'Android 连接' 'Toolbox page must render an Android connection card.'
Require-Text 'UI\MonitorRenderer.cs' '点击复制' 'Device-key value area must communicate click-to-copy behavior.'
Require-Text 'UI\MonitorRenderer.cs' 'EmbeddedServerState' 'Toolbox Android link card must receive the embedded server state.'
Require-Text 'UI\MonitorRenderer.cs' 'PortPromptText' 'Toolbox Android link card must render a local-port status prompt.'
Require-Text 'UI\MonitorRenderer.cs' '8787 已开启' 'Toolbox Android link card must show when local port 8787 is open.'
Require-Text 'UI\MonitorRenderer.cs' '8787 未开启' 'Toolbox Android link card must show when local port 8787 is not open.'
Forbid-Text 'UI\MonitorRenderer.cs' '复制密钥' 'Device-key copy must not be rendered as a separate button.'
Forbid-Text 'UI\MonitorRenderer.cs' 'DrawKeyCopyIcon' 'Device-key copy must not render a separate key-copy button icon.'
Require-Text 'UI\MonitorRenderer.cs' 'peizhikapian\.Width, 268' 'Android link card must be tall enough to keep helper text clear of the buttons.'
Require-Text 'UI\MainForm.cs' 'MemoryToolsAction\.ConfigureAndroidLink' 'MainForm must handle the Android link toolbox action.'
Require-Text 'UI\MainForm.cs' 'MemoryToolsAction\.CopyAndroidDeviceKey' 'MainForm must handle the device-key copy toolbox action.'
Require-Text 'UI\MainForm.cs' 'OpenRelaySettingsDialog\(\);' 'Android link action should reuse the existing remote sync dialog.'
Require-Text 'UI\MainForm.cs' 'Clipboard\.SetText' 'Device-key copy action must use the Windows clipboard.'
Require-Text 'UI\MainForm.cs' '_neiqianfuwuqi\.zhuangtai' 'MainForm must pass the embedded server state to the renderer.'
Require-Text 'Hardware\EmbeddedServer.cs' 'http://127\.0\.0\.1:\{duankou\}/' 'Embedded server must listen on localhost without requiring URL ACL elevation.'
Forbid-Text 'Hardware\EmbeddedServer.cs' 'http://\+:\{duankou\}/' 'Embedded server must not use wildcard HttpListener prefixes that require URL ACL elevation.'

Write-Host 'Toolbar Android link contract passed.'
