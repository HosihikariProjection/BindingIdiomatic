param(
    [Parameter(Mandatory = $true)]
    [string]$ServerRoot
)
# Refuses to start while anything already holds the port this server root is configured for.
# The process-name check cannot separate two roots: they run the same executable, and a second
# session may have started one from a different root. Each root is configured with its own
# port, so the port read from that root's server.properties is the reliable signal and roots on
# different ports do not block each other.
#
# BDS binds both an IPv4 and an IPv6 port, so both are checked; the IPv6 one is optional and a
# root without it is simply probed on IPv4 alone.

$ErrorActionPreference = "Stop"

$ServerRoot = (Resolve-Path $ServerRoot).Path
$Properties = Join-Path $ServerRoot "server.properties"
if (-not (Test-Path $Properties)) {
    throw "server.properties was not found under $ServerRoot."
}

function Get-ConfiguredPort([string]$Key) {
    $match = Select-String -Path $Properties -Pattern "^$Key=(\d+)" | Select-Object -First 1
    if ($match) { return $match.Matches.Groups[1].Value }
    return $null
}

$Ports = @(Get-ConfiguredPort "server-port") + @(Get-ConfiguredPort "server-portv6") |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
    Select-Object -Unique
if ($Ports.Count -eq 0) {
    throw "server.properties under $ServerRoot does not set server-port."
}

$Holders = @()
foreach ($port in $Ports) {
    $Holders += Get-NetUDPEndpoint -LocalPort $port -ErrorAction SilentlyContinue
    $Holders += Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
}

if ($Holders) {
    $Ids = $Holders | ForEach-Object { $_.OwningProcess } | Sort-Object -Unique
    $Owners = ($Ids | ForEach-Object {
        $process = Get-Process -Id $_ -ErrorAction SilentlyContinue
        if ($process) { "$($process.ProcessName) (PID $_)" } else { "PID $_" }
    }) -join ', '
    throw "Port $($Ports -join ' / ') already held by $Owners; another server is running from this machine. Stop it and retry."
}

Write-Output "Port $($Ports -join ' / ') is free."
