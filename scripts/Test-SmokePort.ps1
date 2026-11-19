param(
    [Parameter(Mandatory = $true)]
    [string]$ServerRoot,
    [int]$TimeoutSeconds = 120
)
# Refuses to start while anything already holds the port this server root is configured for.
# The process-name check cannot separate two roots: they run the same executable, and a second
# session may have started one from a different root. BDS binds a fixed port regardless of cwd,
# so the port is the reliable signal. The port comes from the root's own server.properties, so
# roots on different ports do not block each other.

$ErrorActionPreference = "Stop"

$ServerRoot = (Resolve-Path $ServerRoot).Path
$Properties = Join-Path $ServerRoot "server.properties"
if (-not (Test-Path $Properties)) {
    throw "server.properties was not found under $ServerRoot."
}

$Port = (Select-String -Path $Properties -Pattern '^server-port=(\d+)' |
    Select-Object -First 1).Matches.Groups[1].Value
if ([string]::IsNullOrWhiteSpace($Port)) {
    throw "server.properties under $ServerRoot does not set server-port."
}

$Holders = @()
$Holders += Get-NetUDPEndpoint -LocalPort $Port -ErrorAction SilentlyContinue
$Holders += Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
if ($Holders) {
    $Owners = ($Holders | ForEach-Object { $_.OwningProcess } | Sort-Object -Unique) -join ', '
    throw "Port $Port is already held (PID $Owners); another server is running from this machine. Stop it and retry."
}

Write-Output "Port $Port is free."
