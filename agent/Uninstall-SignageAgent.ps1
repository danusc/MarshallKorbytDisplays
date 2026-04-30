[CmdletBinding()]
param(
    [string]$InstallRoot = 'C:\ProgramData\MarshallDigitalSignage\Agent',
    [switch]$RemoveAgentFolder,
    [switch]$RemoveLogsAndCache
)

$ErrorActionPreference = 'Stop'

foreach ($taskName in @('Marshall Signage - Launch', 'Marshall Signage - Monitor')) {
    $task = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
    if ($task) {
        Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
        Write-Host "Removed scheduled task: $taskName"
    }
}

if ($RemoveAgentFolder -and (Test-Path -LiteralPath $InstallRoot)) {
    Remove-Item -LiteralPath $InstallRoot -Recurse -Force
    Write-Host "Removed agent folder: $InstallRoot"
}

if ($RemoveLogsAndCache) {
    foreach ($path in @('C:\ProgramData\MarshallDigitalSignage\Logs', 'C:\ProgramData\MarshallDigitalSignage\ChromeProfile')) {
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Recurse -Force
            Write-Host "Removed $path"
        }
    }
}

Write-Host 'Chrome was not removed.'
