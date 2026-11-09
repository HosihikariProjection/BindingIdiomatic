param(
    [Parameter(Mandatory = $true)]
    [string]$ServerRoot,
    [string]$LlvmRoot = "$env:LOCALAPPDATA\Temp\hosihikari-llvm\clang+llvm-23.1.0-x86_64-pc-windows-msvc",
    [string]$GeneratorRoot = "",
    [int]$TimeoutSeconds = 120
)

$ErrorActionPreference = "Stop"

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$ServerRoot = (Resolve-Path $ServerRoot).Path
$WorkspaceRoot = (Split-Path $RepoRoot -Parent)
if ([string]::IsNullOrWhiteSpace($GeneratorRoot)) {
    $GeneratorRoot = Join-Path $WorkspaceRoot "Binding.Generator"
} else {
    $GeneratorRoot = (Resolve-Path $GeneratorRoot).Path
}

$ServerExecutable = Join-Path $ServerRoot "bedrock_server_mod.exe"
if (-not (Test-Path $ServerExecutable)) {
    throw "bedrock_server_mod.exe was not found under $ServerRoot."
}

$AbiProject = Join-Path $WorkspaceRoot "Binding.Abi\src\Hosihikari.Bindings.Abi\Hosihikari.Bindings.Abi.csproj"
$LeviProject = Join-Path $WorkspaceRoot "Binding.LeviLamina\src\Hosihikari.Bindings.LeviLamina.Interop\Hosihikari.Bindings.LeviLamina.Interop.csproj"

dotnet build (Join-Path $RepoRoot "Hosihikari.Binding.slnx") -c Release --nologo -v:q `
    -p:AbiProjectPath=$AbiProject -p:LeviLaminaInteropProjectPath=$LeviProject
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed with exit code $LASTEXITCODE."
}

# The shim and the two managed layers come from the LeviLamina binding repository.
$LeviRepo = Join-Path $WorkspaceRoot "Binding.LeviLamina"
& (Join-Path $LeviRepo "native\BindingShim\build.ps1") -LlvmRoot $LlvmRoot -GeneratorRoot $GeneratorRoot
if ($LASTEXITCODE -ne 0) {
    throw "the shim build failed with exit code $LASTEXITCODE."
}

$InteropDll = Join-Path $LeviRepo "src\Hosihikari.Bindings.LeviLamina.Interop\bin\Release\net10.0\Hosihikari.Bindings.LeviLamina.Interop.dll"
$AbiDll = Join-Path (Split-Path $InteropDll -Parent) "Hosihikari.Bindings.Abi.dll"
$BaseDll = Join-Path $RepoRoot "src\Hosihikari.Binding\bin\Release\net10.0\Hosihikari.Binding.dll"
$LeviIdiomaticDll = Join-Path $RepoRoot "src\Hosihikari.Binding.LeviLamina\bin\Release\net10.0\Hosihikari.Binding.LeviLamina.dll"
$ShimDll = Join-Path $LeviRepo "artifacts\native\Hosihikari.Bindings.LeviLamina.Shim.dll"
$PluginSource = Join-Path $RepoRoot "tests\runtime-smoke"
$PluginTarget = Join-Path $ServerRoot "plugins\idiomatic-smoke"
$DataTarget = Join-Path $ServerRoot "data\hosihikari\idiomatic-smoke"

foreach ($file in @($InteropDll, $AbiDll, $BaseDll, $LeviIdiomaticDll, $ShimDll, (Join-Path $PluginSource "main.cs"), (Join-Path $PluginSource "manifest.json"))) {
    if (-not (Test-Path $file)) {
        throw "Runtime smoke input is missing: $file"
    }
}

if (Test-Path $PluginTarget) {
    Remove-Item $PluginTarget -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $PluginTarget | Out-Null
Copy-Item (Join-Path $PluginSource "main.cs") $PluginTarget -Force
Copy-Item (Join-Path $PluginSource "manifest.json") $PluginTarget -Force
Copy-Item $InteropDll $PluginTarget -Force
Copy-Item $AbiDll $PluginTarget -Force
Copy-Item $BaseDll $PluginTarget -Force
Copy-Item $LeviIdiomaticDll $PluginTarget -Force
Copy-Item $ShimDll $PluginTarget -Force

if (Test-Path $DataTarget) {
    Remove-Item $DataTarget -Recurse -Force
}

Remove-Item (Join-Path $ServerRoot "plugins\idiomatic_smoke.dll") -Force -ErrorAction SilentlyContinue

Set-Content -Path (Join-Path $ServerRoot "eula.txt") -Value "eula=true"
$LogDirectory = Join-Path $RepoRoot "artifacts"
New-Item -ItemType Directory -Force -Path $LogDirectory | Out-Null
$StandardOutput = Join-Path $LogDirectory "runtime-smoke.out.log"
$StandardError = Join-Path $LogDirectory "runtime-smoke.err.log"
Remove-Item $StandardOutput, $StandardError -Force -ErrorAction SilentlyContinue

$Process = Start-Process `
    -FilePath $ServerExecutable `
    -WorkingDirectory $ServerRoot `
    -RedirectStandardOutput $StandardOutput `
    -RedirectStandardError $StandardError `
    -PassThru `
    -NoNewWindow

$ResultPath = Join-Path $DataTarget "idiomatic-smoke.txt"
$Deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
while ([DateTime]::UtcNow -lt $Deadline) {
    if (Test-Path $ResultPath) {
        break
    }

    if ($Process.HasExited) {
        break
    }

    Start-Sleep -Seconds 1
}

if (-not $Process.HasExited) {
    $Process.Kill()
    $Process.WaitForExit(10000) | Out-Null
}

if (-not (Test-Path $ResultPath)) {
    throw "The runtime smoke did not produce $ResultPath within $TimeoutSeconds seconds. See $StandardOutput."
}

$Result = Get-Content $ResultPath -Raw
Write-Output $Result.TrimEnd()
if ($Result -notmatch '(?m)^result=OK\s*$') {
    throw "Idiomatic runtime smoke failed; see $ResultPath."
}

Write-Output "Idiomatic runtime smoke passed."
