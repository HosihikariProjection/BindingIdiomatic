param(
    [Parameter(Mandatory = $true)]
    [string]$ServerRoot,
    [string]$Python = "",
    [string]$LlvmRoot = "$env:LOCALAPPDATA\Temp\hosihikari-llvm\clang+llvm-23.1.0-x86_64-pc-windows-msvc",
    [string]$GeneratorRoot = "",
    [int]$TimeoutSeconds = 180
)

$ErrorActionPreference = "Stop"

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$ServerRoot = (Resolve-Path $ServerRoot).Path
$WorkspaceRoot = (Split-Path $RepoRoot -Parent)
$FrameworkRoot = Split-Path $WorkspaceRoot -Parent
if ([string]::IsNullOrWhiteSpace($GeneratorRoot)) {
    $GeneratorRoot = Join-Path $WorkspaceRoot "Binding.Generator"
} else {
    $GeneratorRoot = (Resolve-Path $GeneratorRoot).Path
}
if ([string]::IsNullOrWhiteSpace($Python)) {
    $Python = Join-Path $WorkspaceRoot "Binding.Endstone\artifacts\venv\Scripts\python.exe"
}
if (-not (Test-Path $Python)) {
    throw "The Endstone interpreter was not found at $Python; pass -Python."
}

$EndstoneRoot = Join-Path $ServerRoot "bedrock_server.exe"
if (-not (Test-Path $EndstoneRoot)) {
    throw "bedrock_server.exe was not found under $ServerRoot."
}

$AbiProject = Join-Path $WorkspaceRoot "Binding.Abi\src\Hosihikari.Bindings.Abi\Hosihikari.Bindings.Abi.csproj"
$EndstoneProject = Join-Path $WorkspaceRoot "Binding.Endstone\src\Hosihikari.Bindings.Endstone.Interop\Hosihikari.Bindings.Endstone.Interop.csproj"

dotnet build (Join-Path $RepoRoot "Hosihikari.Binding.slnx") -c Release --nologo -v:q `
    -p:AbiProjectPath=$AbiProject -p:EndstoneInteropProjectPath=$EndstoneProject
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed with exit code $LASTEXITCODE."
}

$BindingRoot = Join-Path $WorkspaceRoot "Binding.Endstone"
& (Join-Path $BindingRoot "native\BindingShim\build.ps1") -EndstoneRoot (Join-Path $WorkspaceRoot "Third\Endstone") -GeneratorRoot $GeneratorRoot -LlvmRoot $LlvmRoot
if ($LASTEXITCODE -ne 0) {
    throw "the Endstone shim build failed with exit code $LASTEXITCODE."
}

# The host and the managed core come from the framework; deploy them once per server root.
$HostPlugin = Join-Path $ServerRoot "plugins\hosihikari.dll"
if (-not (Test-Path $HostPlugin)) {
    throw "The Endstone host is not deployed under $ServerRoot; run HostingShim\carriers\endstone\deploy.bat first."
}

# The C# loader is itself a plugin package.
$LoaderRepo = Join-Path $FrameworkRoot "Framework\PluginLoader.CSharp"
dotnet build (Join-Path $LoaderRepo "src\Hosihikari.PluginLoader.CSharp.csproj") -c Release --nologo -v:q
if ($LASTEXITCODE -ne 0) {
    throw "the C# loader build failed with exit code $LASTEXITCODE."
}
$LoaderOut = Join-Path $LoaderRepo "src\bin\Release\net10.0"
$LoaderPackage = Join-Path $ServerRoot "plugins\hosihikari-csharp"
if (Test-Path $LoaderPackage) { Remove-Item $LoaderPackage -Recurse -Force }
New-Item -ItemType Directory -Force -Path $LoaderPackage | Out-Null
Copy-Item (Join-Path $LoaderOut "*.dll") $LoaderPackage
Copy-Item (Join-Path $LoaderOut "*.json") $LoaderPackage
Copy-Item (Join-Path $LoaderRepo "pack\manifest.json") $LoaderPackage -ErrorAction SilentlyContinue
@{
    name         = 'hosihikari-csharp'
    entry        = 'Hosihikari.PluginLoader.CSharp.dll'
    type         = 'hosihikari'
    version      = '1.1.0'
    dependencies = @(@{ name = 'hosihikari' })
    extraInfo    = [ordered]@{ api_version = '2'; manifest_version = '1'; loader = 'assembly' }
} | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $LoaderPackage "manifest.json") -Encoding UTF8

$InteropDll = Join-Path $WorkspaceRoot "Binding.Endstone\src\Hosihikari.Bindings.Endstone.Interop\bin\Release\net10.0\Hosihikari.Bindings.Endstone.Interop.dll"
$AbiDll = Join-Path (Split-Path $InteropDll -Parent) "Hosihikari.Bindings.Abi.dll"
$BaseDll = Join-Path $RepoRoot "src\Hosihikari.Binding\bin\Release\net10.0\Hosihikari.Binding.dll"
$EndstoneIdiomaticDll = Join-Path $RepoRoot "src\Hosihikari.Binding.Endstone\bin\Release\net10.0\Hosihikari.Binding.Endstone.dll"
$ShimDll = Join-Path $BindingRoot "artifacts\native\Hosihikari.Bindings.Endstone.Shim.dll"
$PluginSource = Join-Path $RepoRoot "tests\runtime-smoke-endstone"
$PluginTarget = Join-Path $ServerRoot "plugins\idiomatic-endstone-smoke"
$DataTarget = Join-Path $ServerRoot "data\hosihikari\idiomatic-endstone-smoke"

foreach ($file in @($InteropDll, $AbiDll, $BaseDll, $EndstoneIdiomaticDll, $ShimDll, (Join-Path $PluginSource "main.cs"), (Join-Path $PluginSource "manifest.json"))) {
    if (-not (Test-Path $file)) {
        throw "Runtime smoke input is missing: $file"
    }
}

if (Test-Path $PluginTarget) { Remove-Item $PluginTarget -Recurse -Force }
New-Item -ItemType Directory -Force -Path $PluginTarget | Out-Null
Copy-Item (Join-Path $PluginSource "main.cs") $PluginTarget -Force
Copy-Item (Join-Path $PluginSource "manifest.json") $PluginTarget -Force
Copy-Item $InteropDll, $AbiDll, $BaseDll, $EndstoneIdiomaticDll, $ShimDll $PluginTarget -Force

if (Test-Path $DataTarget) { Remove-Item $DataTarget -Recurse -Force }

Set-Content -Path (Join-Path $ServerRoot "eula.txt") -Value "eula=true"
$LogDirectory = Join-Path $RepoRoot "artifacts"
New-Item -ItemType Directory -Force -Path $LogDirectory | Out-Null
$StandardOutput = Join-Path $LogDirectory "runtime-smoke-endstone.out.log"
$StandardError = Join-Path $LogDirectory "runtime-smoke-endstone.err.log"
Remove-Item $StandardOutput, $StandardError -Force -ErrorAction SilentlyContinue

# Endstone stops as soon as its stdin closes, so the server runs behind a pipe
# that stays open for the whole wait; the piped "stop" is the clean shutdown.
$Driver = Join-Path $env:TEMP "hosihikari-idiomatic-endstone.cmd"
$DriverLines = @(
    "@echo off"
    "( ping -n $($TimeoutSeconds + 5) 127.0.0.1 >nul & echo stop ) | `"$Python`" -m endstone -s `"$ServerRoot`" -y > `"$StandardOutput`" 2> `"$StandardError`""
)
Set-Content -Path $Driver -Value $DriverLines -Encoding ASCII

$Process = Start-Process `
    -FilePath $Driver `
    -WorkingDirectory $ServerRoot `
    -PassThru `
    -NoNewWindow

$ResultPath = Join-Path $DataTarget "idiomatic-endstone-smoke.txt"
$Deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
while ([DateTime]::UtcNow -lt $Deadline) {
    if (Test-Path $ResultPath) { break }
    if ($Process.HasExited) { break }
    Start-Sleep -Seconds 1
}

if (-not $Process.HasExited) {
    $Process.Kill()
    $Process.WaitForExit(10000) | Out-Null
}

if (-not (Test-Path $ResultPath)) {
    throw "The Endstone runtime smoke did not produce $ResultPath within $TimeoutSeconds seconds. See $StandardOutput."
}

$Result = Get-Content $ResultPath -Raw
Write-Output $Result.TrimEnd()
if ($Result -notmatch '(?m)^result=OK\s*$') {
    throw "The Endstone Idiomatic runtime smoke failed; see $ResultPath."
}

Write-Output "Endstone Idiomatic runtime smoke passed."
