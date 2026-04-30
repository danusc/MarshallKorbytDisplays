[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Register', 'Launch', 'Monitor', 'CheckIn', 'RestartChrome')]
    [string]$Mode,

    [string]$ConfigPath = 'C:\ProgramData\MarshallDigitalSignage\Agent\signage-agent.json',
    [switch]$WhatIfLaunch
)

$ErrorActionPreference = 'Stop'
$AgentRoot = 'C:\ProgramData\MarshallDigitalSignage\Agent'
$TokenPath = Join-Path $AgentRoot 'device-token.txt'
$CachePath = Join-Path $AgentRoot 'last-known-good.json'
$LogRoot = 'C:\ProgramData\MarshallDigitalSignage\Logs'
$LogPath = Join-Path $LogRoot ("signage-agent-{0:yyyyMMdd}.log" -f (Get-Date))

function Write-AgentLog {
    param([string]$Message, [string]$Level = 'INFO')
    New-Item -ItemType Directory -Force -Path $LogRoot | Out-Null
    $line = '{0:u} [{1}] {2}' -f (Get-Date), $Level, $Message
    Add-Content -Path $LogPath -Value $line
    Write-Verbose $line
}

function Get-AgentConfig {
    if (-not (Test-Path -LiteralPath $ConfigPath)) {
        throw "Config file not found: $ConfigPath"
    }

    Get-Content -LiteralPath $ConfigPath -Raw | ConvertFrom-Json
}

function Get-ComputerSerialNumber {
    try {
        (Get-CimInstance -ClassName Win32_BIOS).SerialNumber
    }
    catch {
        Write-AgentLog "Could not read serial number: $($_.Exception.Message)" 'WARN'
        $null
    }
}

function Get-PrimaryMacAddress {
    try {
        Get-CimInstance -ClassName Win32_NetworkAdapterConfiguration |
            Where-Object { $_.IPEnabled -and $_.MACAddress } |
            Select-Object -First 1 -ExpandProperty MACAddress
    }
    catch {
        Write-AgentLog "Could not read MAC address: $($_.Exception.Message)" 'WARN'
        $null
    }
}

function Get-DeviceToken {
    if (Test-Path -LiteralPath $TokenPath) {
        return (Get-Content -LiteralPath $TokenPath -Raw).Trim()
    }

    return $null
}

function Save-DeviceToken {
    param([string]$Token)
    New-Item -ItemType Directory -Force -Path $AgentRoot | Out-Null
    Set-Content -LiteralPath $TokenPath -Value $Token -NoNewline
}

function Invoke-AgentApi {
    param(
        [Parameter(Mandatory = $true)][object]$Config,
        [Parameter(Mandatory = $true)][string]$Path,
        [ValidateSet('Get', 'Post')][string]$Method = 'Post',
        [object]$Body,
        [switch]$UseDeviceAuth
    )

    $uri = '{0}{1}' -f $Config.ApiBaseUrl.TrimEnd('/'), $Path
    $headers = @{}
    if ($UseDeviceAuth) {
        $token = Get-DeviceToken
        if (-not $token) {
            throw 'Device token is missing. Run -Mode Register first.'
        }

        $headers['X-Display-ComputerName'] = $env:COMPUTERNAME
        $headers['X-Display-Token'] = $token
    }

    $parameters = @{
        Uri = $uri
        Method = $Method
        Headers = $headers
        TimeoutSec = 30
    }

    if ($PSBoundParameters.ContainsKey('Body')) {
        $parameters.ContentType = 'application/json'
        $parameters.Body = ($Body | ConvertTo-Json -Depth 8)
    }

    Invoke-RestMethod @parameters
}

function Register-Display {
    param([object]$Config)

    $body = @{
        computerName = $env:COMPUTERNAME
        serialNumber = Get-ComputerSerialNumber
        macAddress = Get-PrimaryMacAddress
        agentVersion = '1.0.0'
    }

    $response = Invoke-AgentApi -Config $Config -Path '/api/agent/register' -Body $body
    if ($response.token) {
        Save-DeviceToken -Token $response.token
        Write-AgentLog "Registration completed and token saved for $env:COMPUTERNAME"
    }
    else {
        Write-AgentLog "Registration completed. Server did not return a new token."
    }

    $response
}

function Test-UrlAllowed {
    param([string]$Url, [string[]]$AllowedDomains)

    if (-not [Uri]::TryCreate($Url, [UriKind]::Absolute, [ref]([Uri]$null))) {
        return $false
    }

    $uri = [Uri]$Url
    if ($uri.Scheme -notin @('http', 'https')) {
        return $false
    }

    $hostName = $uri.Host.ToLowerInvariant()
    foreach ($domain in $AllowedDomains) {
        $pattern = $domain.Trim().TrimEnd('.').ToLowerInvariant()
        if ($pattern.StartsWith('*.')) {
            $suffix = $pattern.Substring(2)
            if ($hostName -eq $suffix -or $hostName.EndsWith(".$suffix")) {
                return $true
            }
        }
        elseif ($hostName -eq $pattern) {
            return $true
        }
    }

    return $false
}

function Get-CachedConfig {
    if (Test-Path -LiteralPath $CachePath) {
        Get-Content -LiteralPath $CachePath -Raw | ConvertFrom-Json
    }
}

function Save-CachedConfig {
    param([object]$ConfigResponse)
    New-Item -ItemType Directory -Force -Path $AgentRoot | Out-Null
    $ConfigResponse | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $CachePath
}

function Get-ChromeProcesses {
    param([object]$Config)
    $profile = [Regex]::Escape($Config.ChromeProfilePath)
    Get-CimInstance Win32_Process -Filter "Name = 'chrome.exe'" |
        Where-Object { $_.CommandLine -match $profile }
}

function Stop-SignageChrome {
    param([object]$Config)
    Get-ChromeProcesses -Config $Config | ForEach-Object {
        Write-AgentLog "Stopping Chrome process $($_.ProcessId)"
        Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue
    }
}

function Start-SignageChrome {
    param([object]$Config, [string]$Url)

    if (-not (Test-Path -LiteralPath $Config.ChromePath)) {
        throw "Chrome not found at $($Config.ChromePath)"
    }

    New-Item -ItemType Directory -Force -Path $Config.ChromeProfilePath | Out-Null
    $modeArg = if ($Config.LaunchMode -eq 'fullscreen') { '--start-fullscreen' } else { '--kiosk' }
    $arguments = @(
        $modeArg,
        '--autoplay-policy=no-user-gesture-required',
        '--no-first-run',
        '--disable-session-crashed-bubble',
        ('--user-data-dir="{0}"' -f $Config.ChromeProfilePath),
        ('"{0}"' -f $Url)
    )

    Write-AgentLog "Launching Chrome to $Url"
    if (-not $WhatIfLaunch) {
        Start-Process -FilePath $Config.ChromePath -ArgumentList $arguments
    }
}

function Get-CurrentConfig {
    param([object]$Config, [switch]$CheckIn)

    try {
        if (-not (Get-DeviceToken)) {
            Register-Display -Config $Config | Out-Null
        }

        if ($CheckIn) {
            $chromeRunning = @(Get-ChromeProcesses -Config $Config).Count -gt 0
            $cached = Get-CachedConfig
            $body = @{
                serialNumber = Get-ComputerSerialNumber
                macAddress = Get-PrimaryMacAddress
                agentVersion = '1.0.0'
                currentUrl = $cached.desiredUrl
                chromeRunning = $chromeRunning
                lastError = $null
            }
            $response = Invoke-AgentApi -Config $Config -Path '/api/agent/checkin' -Body $body -UseDeviceAuth
        }
        else {
            $response = Invoke-AgentApi -Config $Config -Path '/api/agent/config' -Method Get -UseDeviceAuth
        }

        if ($response.desiredUrl) {
            if (-not (Test-UrlAllowed -Url $response.desiredUrl -AllowedDomains $response.allowedDomains)) {
                throw "Server returned URL outside allowed domains: $($response.desiredUrl)"
            }

            Save-CachedConfig -ConfigResponse $response
        }

        return $response
    }
    catch {
        Write-AgentLog "API unavailable or invalid response: $($_.Exception.Message)" 'WARN'
        $cached = Get-CachedConfig
        if ($cached) {
            Write-AgentLog "Using cached last-known-good URL."
            return $cached
        }

        throw
    }
}

function Complete-Command {
    param([object]$Config, [int]$CommandId, [string]$Status, [string]$ResultMessage)
    Invoke-AgentApi -Config $Config -Path "/api/agent/commands/$CommandId/complete" -Body @{
        status = $Status
        resultMessage = $ResultMessage
    } -UseDeviceAuth | Out-Null
}

function Invoke-Launch {
    param([object]$Config, [switch]$CheckIn)

    $cachedBefore = Get-CachedConfig
    $serverConfig = Get-CurrentConfig -Config $Config -CheckIn:$CheckIn
    if (-not $serverConfig.enabled) {
        Write-AgentLog 'Display is disabled by server config.'
        Stop-SignageChrome -Config $Config
        return
    }

    if (-not $serverConfig.desiredUrl) {
        Write-AgentLog 'No desired URL assigned yet.' 'WARN'
        return
    }

    $running = @(Get-ChromeProcesses -Config $Config).Count -gt 0
    $desiredChanged = $cachedBefore -and $cachedBefore.desiredUrl -and
        -not [string]::Equals($cachedBefore.desiredUrl, $serverConfig.desiredUrl, [StringComparison]::OrdinalIgnoreCase)

    if ($desiredChanged) {
        Write-AgentLog "Desired URL changed from $($cachedBefore.desiredUrl) to $($serverConfig.desiredUrl)"
        Stop-SignageChrome -Config $Config
        $running = $false
    }

    if (-not $running) {
        Start-SignageChrome -Config $Config -Url $serverConfig.desiredUrl
    }

    foreach ($command in @($serverConfig.commands)) {
        if ($command.commandType -eq 'RestartChrome') {
            try {
                Stop-SignageChrome -Config $Config
                Start-SignageChrome -Config $Config -Url $serverConfig.desiredUrl
                Complete-Command -Config $Config -CommandId $command.id -Status 'Completed' -ResultMessage 'Chrome restarted'
            }
            catch {
                Complete-Command -Config $Config -CommandId $command.id -Status 'Failed' -ResultMessage $_.Exception.Message
                throw
            }
        }
    }
}

$config = Get-AgentConfig
if (-not $config.ChromePath) { $config | Add-Member -NotePropertyName ChromePath -NotePropertyValue 'C:\Program Files\Google\Chrome\Application\chrome.exe' }
if (-not $config.ChromeProfilePath) { $config | Add-Member -NotePropertyName ChromeProfilePath -NotePropertyValue 'C:\ProgramData\MarshallDigitalSignage\ChromeProfile' }
if (-not $config.LaunchMode) { $config | Add-Member -NotePropertyName LaunchMode -NotePropertyValue 'kiosk' }

try {
    switch ($Mode) {
        'Register' { Register-Display -Config $config | ConvertTo-Json -Depth 8 }
        'Launch' { Invoke-Launch -Config $config }
        'Monitor' { Invoke-Launch -Config $config -CheckIn }
        'CheckIn' { Get-CurrentConfig -Config $config -CheckIn | ConvertTo-Json -Depth 8 }
        'RestartChrome' {
            $serverConfig = Get-CurrentConfig -Config $config
            Stop-SignageChrome -Config $config
            if ($serverConfig.desiredUrl) {
                Start-SignageChrome -Config $config -Url $serverConfig.desiredUrl
            }
        }
    }
}
catch {
    Write-AgentLog $_.Exception.Message 'ERROR'
    throw
}
