param(
    [Parameter(Mandatory = $true)]
    [string]$ServerRoot,
    [int]$TimeoutSeconds = 180,
    # The shim rebuild costs minutes and only changes when the generated sources do.
    [switch]$SkipShimBuild,
    # A separate checkout to build the Bedrock surface from, when the default one is shared
    # with another session that regenerates it. Defaults to the sibling checkout.
    [string]$BedrockRoot = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($PSScriptRoot)) {
    $PSScriptRoot = Split-Path $MyInvocation.MyCommand.Path -Parent
}
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$ServerRoot = (Resolve-Path $ServerRoot).Path
$WorkspaceRoot = (Split-Path $RepoRoot -Parent)

$ServerExecutable = Join-Path $ServerRoot "bedrock_server_mod.exe"
if (-not (Test-Path $ServerExecutable)) {
    throw "bedrock_server_mod.exe was not found under $ServerRoot."
}

$AbiProject = Join-Path $WorkspaceRoot "Binding.Abi\src\Hosihikari.Bindings.Abi\Hosihikari.Bindings.Abi.csproj"

$BedrockRepo = if ([string]::IsNullOrWhiteSpace($BedrockRoot)) {
    Join-Path $WorkspaceRoot "Binding.Bedrock"
} else {
    (Resolve-Path $BedrockRoot).Path
}
$BedrockProject = Join-Path $BedrockRepo "src\Hosihikari.Bindings.Bedrock.Interop\Hosihikari.Bindings.Bedrock.Interop.csproj"

dotnet build $BedrockProject -c Release --nologo -v:q -p:AbiProjectPath=$AbiProject
if ($LASTEXITCODE -ne 0) {
    throw "the Bedrock Interop build failed with exit code $LASTEXITCODE."
}

# The Interop surface is regenerated between runs; an incremental build has been observed
# to keep a stale assembly when only generated sources changed.
dotnet build (Join-Path $RepoRoot "Hosihikari.Binding.slnx") -c Release --nologo -v:q `
    -p:AbiProjectPath=$AbiProject -p:BedrockInteropProjectPath=$BedrockProject
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed with exit code $LASTEXITCODE."
}

# The shim compiles carrier inline bodies and prelinks against the runtime data, so it needs
# both; the carrier root is a workspace checkout, not something it can guess.
$ShimOutput = Join-Path $BedrockRepo "artifacts\runtime-smoke\Hosihikari.Bindings.Bedrock.Shim.dll"
if (-not $SkipShimBuild) {
& (Join-Path $BedrockRepo "native\BindingShim\build.ps1") -Configuration win-x64-server `
    -LeviLaminaRoot (Join-Path $WorkspaceRoot "Third\LeviLamina") `
    -BedrockRuntimeData (Join-Path $ServerRoot "bedrock_runtime_data") `
    -Output $ShimOutput
if ($LASTEXITCODE -ne 0) {
    throw "the Bedrock shim build failed with exit code $LASTEXITCODE."
}
}

$InteropDll = Join-Path $BedrockRepo "src\Hosihikari.Bindings.Bedrock.Interop\bin\Release\net10.0\Hosihikari.Bindings.Bedrock.Interop.dll"
$AbiDll = Join-Path (Split-Path $InteropDll -Parent) "Hosihikari.Bindings.Abi.dll"
$BaseDll = Join-Path $RepoRoot "src\Hosihikari.Binding\bin\Release\net10.0\Hosihikari.Binding.dll"
$BedrockIdiomaticDll = Join-Path $RepoRoot "src\Hosihikari.Binding.Bedrock\bin\Release\net10.0\Hosihikari.Binding.Bedrock.dll"
$PluginSource = Join-Path $RepoRoot "tests\runtime-smoke-bedrock"
$PluginTarget = Join-Path $ServerRoot "plugins\idiomatic-bedrock-smoke"
$DataTarget = Join-Path $ServerRoot "data\hosihikari\idiomatic-bedrock-smoke"

foreach ($file in @($InteropDll, $AbiDll, $BaseDll, $BedrockIdiomaticDll, $ShimOutput, (Join-Path $PluginSource "main.cs"), (Join-Path $PluginSource "manifest.json"))) {
    if (-not (Test-Path $file)) {
        throw "Runtime smoke input is missing: $file"
    }
}

if (Test-Path $PluginTarget) { Remove-Item $PluginTarget -Recurse -Force }
New-Item -ItemType Directory -Force -Path $PluginTarget | Out-Null
Copy-Item (Join-Path $PluginSource "main.cs") $PluginTarget -Force
Copy-Item (Join-Path $PluginSource "manifest.json") $PluginTarget -Force
Copy-Item $InteropDll, $AbiDll, $BaseDll, $BedrockIdiomaticDll, $ShimOutput $PluginTarget -Force

if (Test-Path $DataTarget) { Remove-Item $DataTarget -Recurse -Force }
Remove-Item (Join-Path $ServerRoot "plugins\idiomatic_bedrock_smoke.dll") -Force -ErrorAction SilentlyContinue
# The C# loader caches the compiled assembly next to the plugin's data, keyed by content;
# a stale entry hides edits to main.cs.
Remove-Item (Join-Path $DataTarget "bin") -Recurse -Force -ErrorAction SilentlyContinue

Set-Content -Path (Join-Path $ServerRoot "eula.txt") -Value "eula=true"
$LogDirectory = Join-Path $RepoRoot "artifacts"
New-Item -ItemType Directory -Force -Path $LogDirectory | Out-Null
$StandardOutput = Join-Path $LogDirectory "runtime-smoke-bedrock.out.log"
$StandardError = Join-Path $LogDirectory "runtime-smoke-bedrock.err.log"
Remove-Item $StandardOutput, $StandardError -Force -ErrorAction SilentlyContinue

$Process = Start-Process `
    -FilePath $ServerExecutable `
    -WorkingDirectory $ServerRoot `
    -RedirectStandardOutput $StandardOutput `
    -RedirectStandardError $StandardError `
    -PassThru `
    -NoNewWindow

$ResultPath = Join-Path $DataTarget "idiomatic-bedrock-smoke.txt"
$Deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
while ([DateTime]::UtcNow -lt $Deadline) {
    if (Test-Path $ResultPath) { break }
    if ($Process.HasExited) { break }
    Start-Sleep -Seconds 1
}

if (-not $Process.HasExited) {
    Stop-Process -Id $Process.Id -Force
    Start-Sleep -Seconds 2
}

if (-not (Test-Path $ResultPath)) {
    Get-Content $StandardOutput -Tail 80 -ErrorAction SilentlyContinue
    Get-Content $StandardError -Tail 40 -ErrorAction SilentlyContinue
    throw "The Bedrock runtime smoke did not produce $ResultPath within $TimeoutSeconds seconds."
}

$Result = Get-Content $ResultPath -Raw
Write-Output $Result.TrimEnd()
if ($Result -notmatch '(?m)^result=OK\s*$') {
    throw "The Bedrock Idiomatic runtime smoke failed; see $ResultPath."
}

Write-Output "Bedrock Idiomatic runtime smoke passed."
