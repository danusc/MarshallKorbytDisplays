[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ApiBaseUrl,

    [string]$KioskUser = 'axistvuser',
    [string]$InstallRoot = 'C:\ProgramData\MarshallDigitalSignage\Agent',
    [switch]$RemoveOldShortcut
)

$ErrorActionPreference = 'Stop'

function Assert-Admin {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw 'Install-SignageAgent.ps1 must be run as Administrator.'
    }
}

Assert-Admin

$source = Split-Path -Parent $MyInvocation.MyCommand.Path
$logRoot = 'C:\ProgramData\MarshallDigitalSignage\Logs'
$chromeProfile = 'C:\ProgramData\MarshallDigitalSignage\ChromeProfile'
New-Item -ItemType Directory -Force -Path $InstallRoot, $logRoot, $chromeProfile | Out-Null

Copy-Item -LiteralPath (Join-Path $source 'SignageAgent.ps1') -Destination $InstallRoot -Force
Copy-Item -LiteralPath (Join-Path $source 'Test-SignageAgent.ps1') -Destination $InstallRoot -Force
Copy-Item -LiteralPath (Join-Path $source 'Uninstall-SignageAgent.ps1') -Destination $InstallRoot -Force

$configPath = Join-Path $InstallRoot 'signage-agent.json'
$config = [ordered]@{
    ApiBaseUrl = $ApiBaseUrl.TrimEnd('/')
    LaunchMode = 'kiosk'
    PollSeconds = 300
    ChromePath = 'C:\Program Files\Google\Chrome\Application\chrome.exe'
    ChromeProfilePath = $chromeProfile
    KioskUser = $KioskUser
}
$config | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $configPath

$agentScript = Join-Path $InstallRoot 'SignageAgent.ps1'
$powerShell = Join-Path $PSHOME 'pwsh.exe'
if (-not (Test-Path -LiteralPath $powerShell)) {
    $powerShell = "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe"
}

$launchAction = New-ScheduledTaskAction -Execute $powerShell -Argument "-NoProfile -ExecutionPolicy Bypass -File `"$agentScript`" -Mode Launch"
$launchTrigger = New-ScheduledTaskTrigger -AtLogOn -User $KioskUser
$launchPrincipal = New-ScheduledTaskPrincipal -UserId $KioskUser -LogonType Interactive -RunLevel LeastPrivilege
Register-ScheduledTask -TaskName 'Marshall Signage - Launch' -Action $launchAction -Trigger $launchTrigger -Principal $launchPrincipal -Description 'Launch Marshall signage Chrome kiosk at logon.' -Force | Out-Null

$monitorAction = New-ScheduledTaskAction -Execute $powerShell -Argument "-NoProfile -ExecutionPolicy Bypass -File `"$agentScript`" -Mode Monitor"
$monitorTrigger = New-ScheduledTaskTrigger -Once -At (Get-Date).AddMinutes(1) -RepetitionInterval (New-TimeSpan -Minutes 5)
$monitorPrincipal = New-ScheduledTaskPrincipal -UserId $KioskUser -LogonType Interactive -RunLevel LeastPrivilege
Register-ScheduledTask -TaskName 'Marshall Signage - Monitor' -Action $monitorAction -Trigger $monitorTrigger -Principal $monitorPrincipal -Description 'Check in and keep Marshall signage Chrome kiosk running.' -Force | Out-Null

if ($RemoveOldShortcut) {
    $startupPath = "C:\Users\$KioskUser\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup"
    if (Test-Path -LiteralPath $startupPath) {
        Get-ChildItem -LiteralPath $startupPath -Filter '*Chrome*.lnk' -ErrorAction SilentlyContinue |
            Rename-Item -NewName { "$($_.BaseName).old$($_.Extension)" } -Force
    }
}

& $powerShell -NoProfile -ExecutionPolicy Bypass -File $agentScript -Mode Register

Write-Host 'Marshall Signage agent installed.'
Write-Host "Config: $configPath"
Get-ScheduledTask -TaskName 'Marshall Signage - Launch','Marshall Signage - Monitor' | Select-Object TaskName, State
