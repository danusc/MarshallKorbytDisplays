[CmdletBinding()]
param(
    [string]$ConfigPath = 'C:\ProgramData\MarshallDigitalSignage\Agent\signage-agent.json',
    [switch]$LaunchChrome
)

$ErrorActionPreference = 'Continue'
$AgentRoot = Split-Path -Parent $ConfigPath
$TokenPath = Join-Path $AgentRoot 'device-token.txt'
$AgentScript = Join-Path $AgentRoot 'SignageAgent.ps1'
$results = New-Object System.Collections.Generic.List[object]

function Add-Result {
    param([string]$Check, [bool]$Passed, [string]$Detail)
    $results.Add([pscustomobject]@{ Check = $Check; Passed = $Passed; Detail = $Detail })
}

$config = $null
if (Test-Path -LiteralPath $ConfigPath) {
    $config = Get-Content -LiteralPath $ConfigPath -Raw | ConvertFrom-Json
    Add-Result 'Config exists' $true $ConfigPath
}
else {
    Add-Result 'Config exists' $false $ConfigPath
}

$chromePath = if ($config -and $config.ChromePath) { $config.ChromePath } else { 'C:\Program Files\Google\Chrome\Application\chrome.exe' }
Add-Result 'Chrome exists' (Test-Path -LiteralPath $chromePath) $chromePath
Add-Result 'Token exists' (Test-Path -LiteralPath $TokenPath) $TokenPath
Add-Result 'Kiosk user path exists' (Test-Path -LiteralPath 'C:\Users\axistvuser') 'C:\Users\axistvuser'

foreach ($taskName in @('Marshall Signage - Launch', 'Marshall Signage - Monitor')) {
    $task = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
    Add-Result "Scheduled task $taskName" ($null -ne $task) ($task.State 2>$null)
}

if ($config) {
    try {
        Invoke-RestMethod -Uri ($config.ApiBaseUrl.TrimEnd('/') + '/swagger/index.html') -Method Get -TimeoutSec 15 | Out-Null
        Add-Result 'Azure App Service API reachable' $true $config.ApiBaseUrl
    }
    catch {
        Add-Result 'Azure App Service API reachable' $false $_.Exception.Message
    }

    if ((Test-Path -LiteralPath $TokenPath) -and (Test-Path -LiteralPath $AgentScript)) {
        try {
            $response = & pwsh -NoProfile -ExecutionPolicy Bypass -File $AgentScript -Mode CheckIn -ConfigPath $ConfigPath -Verbose:$false | ConvertFrom-Json
            Add-Result 'Can retrieve assigned URL' ($null -ne $response) $response.desiredUrl
            $allowed = $false
            if ($response.desiredUrl -and $response.allowedDomains) {
                $hostName = ([Uri]$response.desiredUrl).Host.ToLowerInvariant()
                foreach ($domain in $response.allowedDomains) {
                    $pattern = $domain.Trim().TrimEnd('.').ToLowerInvariant()
                    if (($pattern.StartsWith('*.') -and ($hostName -eq $pattern.Substring(2) -or $hostName.EndsWith('.' + $pattern.Substring(2)))) -or $hostName -eq $pattern) {
                        $allowed = $true
                    }
                }
            }
            Add-Result 'URL is allowed' $allowed $response.desiredUrl
        }
        catch {
            Add-Result 'Can retrieve assigned URL' $false $_.Exception.Message
            Add-Result 'URL is allowed' $false 'No assigned URL retrieved'
        }
    }
}

if ($LaunchChrome -and (Test-Path -LiteralPath $AgentScript)) {
    try {
        & pwsh -NoProfile -ExecutionPolicy Bypass -File $AgentScript -Mode Launch -ConfigPath $ConfigPath -WhatIfLaunch
        Add-Result 'Can launch Chrome in test mode' $true 'Launch command generated'
    }
    catch {
        Add-Result 'Can launch Chrome in test mode' $false $_.Exception.Message
    }
}

$results | Format-Table -AutoSize
if ($results.Where({ -not $_.Passed }).Count -gt 0) {
    exit 1
}
