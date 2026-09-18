param(
    [Parameter(Mandatory = $true)]
    [string]$RepositoryRoot
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

$utf8 = New-Object System.Text.UTF8Encoding($false)
$artifactRoot = Join-Path $RepositoryRoot 'artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1c-c4-full-domain-performance-confirmation2'
$requiredFingerprint = '931EAC000C09399B8EAABEB0316F16980620CAC45D1FC7F13516CE55DAF69336'
$requiredPowerGuid = '8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c'
$hostFingerprintVariable = 'NRS_FDPC2_HOST_FINGERPRINT_SHA256'
$optInVariable = 'NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1C_C4_FULL_DOMAIN_PERF2'
$runIndexVariable = 'NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1C_C4_PERF_RUN_INDEX'
$runtimeVariables = @(
    'DOTNET_TieredCompilation',
    'DOTNET_TieredPGO',
    'DOTNET_TC_QuickJit',
    'DOTNET_TC_QuickJitForLoops',
    'DOTNET_ReadyToRun',
    'COMPlus_TieredCompilation',
    'COMPlus_TieredPGO',
    'COMPlus_TC_QuickJit',
    'COMPlus_TC_QuickJitForLoops',
    'COMPlus_ReadyToRun'
)
$simulationProject = Join-Path $RepositoryRoot 'tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj'
$method = 'NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.M10FinalVr2EngineeringRepairPlanning1Rp1cC4FullDomainPerformanceConfirmation2Tests.Rp1cC4FullDomainPerformanceConfirmation2_MeasuresOneFreshProcess'

function Normalize-OneLine([object]$Value) {
    if ($null -eq $Value) { return '' }
    return ([string]$Value).Replace("`r", ' ').Replace("`n", ' ').Trim()
}
function Get-Sha256Text([string]$Text) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($Text)
        return ([System.BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '')
    }
    finally { $sha.Dispose() }
}
function Get-ActivePowerScheme {
    $output = & powercfg.exe /GETACTIVESCHEME 2>&1
    if ($LASTEXITCODE -ne 0) { throw 'Unable to capture active Windows power scheme.' }
    $line = (($output | ForEach-Object { Normalize-OneLine $_ }) -join ' ').Trim()
    if ([string]::IsNullOrWhiteSpace($line)) { throw 'Active Windows power scheme output was empty.' }
    return $line
}
function Get-DotnetSdkVersion {
    $output = & dotnet --version 2>&1
    if ($LASTEXITCODE -ne 0) { throw 'Unable to capture dotnet SDK version.' }
    return (($output | Select-Object -First 1).ToString()).Trim()
}
function Get-HostSnapshot {
    $systemProduct = Get-CimInstance -ClassName Win32_ComputerSystemProduct
    $processor = Get-CimInstance -ClassName Win32_Processor | Select-Object -First 1
    $computer = Get-CimInstance -ClassName Win32_ComputerSystem
    $os = Get-CimInstance -ClassName Win32_OperatingSystem
    if ($null -eq $systemProduct -or $null -eq $processor -or $null -eq $computer -or $null -eq $os) {
        throw 'Required CIM host provenance could not be captured.'
    }
    $uuid = Normalize-OneLine $systemProduct.UUID
    $cpu = Normalize-OneLine $processor.Name
    $manufacturer = Normalize-OneLine $computer.Manufacturer
    $model = Normalize-OneLine $computer.Model
    $logical = [int]$computer.NumberOfLogicalProcessors
    $memory = [UInt64]$computer.TotalPhysicalMemory
    $osVersion = Normalize-OneLine $os.Version
    $osBuild = Normalize-OneLine $os.BuildNumber
    $fingerprintSource = @(
        $uuid,
        $cpu,
        $manufacturer,
        $model,
        $logical.ToString([System.Globalization.CultureInfo]::InvariantCulture),
        $memory.ToString([System.Globalization.CultureInfo]::InvariantCulture),
        $osVersion,
        $osBuild
    ) -join "`n"
    [pscustomobject]@{
        Fingerprint = Get-Sha256Text $fingerprintSource
        CpuName = $cpu
        Manufacturer = $manufacturer
        Model = $model
        LogicalProcessors = $logical
        TotalPhysicalMemory = $memory
        OsVersion = $osVersion
        OsBuild = $osBuild
        ProcessArchitecture = [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString()
        PowerScheme = Get-ActivePowerScheme
        HypervisorPresent = [bool]$computer.HypervisorPresent
        DotnetSdkVersion = Get-DotnetSdkVersion
        StopwatchFrequency = [System.Diagnostics.Stopwatch]::Frequency
    }
}
function Bool-Text([bool]$Value) { if ($Value) { return 'True' }; return 'False' }
function Write-Utf8Lines([string]$Path, [string[]]$Lines) { [System.IO.File]::WriteAllLines($Path, $Lines, $utf8) }
function Invoke-OneProcess([int]$RunIndex, [string]$HostFingerprint) {
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = 'dotnet'
    $psi.WorkingDirectory = $RepositoryRoot
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    foreach ($name in $runtimeVariables) { $psi.EnvironmentVariables.Remove($name) }
    $psi.EnvironmentVariables[$optInVariable] = '1'
    $psi.EnvironmentVariables[$runIndexVariable] = $RunIndex.ToString([System.Globalization.CultureInfo]::InvariantCulture)
    $psi.EnvironmentVariables[$hostFingerprintVariable] = $HostFingerprint
    $psi.Arguments = 'test --project "' + $simulationProject + '" --configuration Release --no-build -- --explicit only --filter-method "' + $method + '" --parallel none'
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $psi
    if (-not $process.Start()) { throw ('Unable to start FDPC2 process {0}.' -f $RunIndex) }
    $stdoutTask = $process.StandardOutput.ReadToEndAsync()
    $stderrTask = $process.StandardError.ReadToEndAsync()
    $process.WaitForExit()
    $stdout = $stdoutTask.Result
    $stderr = $stderrTask.Result
    $exitCode = $process.ExitCode
    $process.Dispose()
    Write-Host $stdout
    if (-not [string]::IsNullOrWhiteSpace($stderr)) { Write-Host $stderr }
    if ($exitCode -ne 0) { throw ('FDPC2 focused process {0} failed with exit code {1}.' -f $RunIndex,$exitCode) }
}

$startHost = Get-HostSnapshot
if ($startHost.Fingerprint -ne $requiredFingerprint) { throw ('FDPC2 host fingerprint mismatch. Expected {0}; actual {1}.' -f $requiredFingerprint,$startHost.Fingerprint) }
if ($startHost.PowerScheme.ToLowerInvariant().IndexOf($requiredPowerGuid) -lt 0) { throw ('FDPC2 active power scheme mismatch. Expected GUID {0}; actual {1}.' -f $requiredPowerGuid,$startHost.PowerScheme) }

if (Test-Path -LiteralPath $artifactRoot) { Remove-Item -LiteralPath $artifactRoot -Recurse -Force }
New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null

for ($runIndex = 1; $runIndex -le 5; $runIndex++) {
    Write-Host ('---- FDPC2 process {0} of 5 ----' -f $runIndex)
    Invoke-OneProcess $runIndex $startHost.Fingerprint
    $processDir = Join-Path $artifactRoot ('process-' + $runIndex.ToString('00',[System.Globalization.CultureInfo]::InvariantCulture))
    foreach ($name in @('01-process-contract.txt','02-exact-v9-call-timing.csv','03-seam-call-timing.csv','04-runtime-context.txt','05-process-summary.txt')) {
        if (-not (Test-Path -LiteralPath (Join-Path $processDir $name) -PathType Leaf)) {
            throw ('FDPC2 required process artifact missing: process-{0}/{1}' -f $runIndex,$name)
        }
    }
}

$endHost = Get-HostSnapshot
$fingerprintMatch = $endHost.Fingerprint -eq $startHost.Fingerprint
$powerStable = $endHost.PowerScheme -eq $startHost.PowerScheme
if (-not $fingerprintMatch) { throw 'FDPC2 host fingerprint changed during the gate.' }
if (-not $powerStable) { throw 'FDPC2 active power scheme changed during the gate.' }
if ($endHost.Fingerprint -ne $requiredFingerprint) { throw 'FDPC2 end host fingerprint does not match the frozen A2 host.' }
if ($endHost.PowerScheme.ToLowerInvariant().IndexOf($requiredPowerGuid) -lt 0) { throw 'FDPC2 end power scheme does not match the frozen A2 scheme.' }

Write-Utf8Lines (Join-Path $artifactRoot '02-execution-host-provenance.txt') @(
    'status=COMPLETE-HOST-PROVENANCE-CAPTURE',
    ('start-host-fingerprint-sha256={0}' -f $startHost.Fingerprint),
    ('end-host-fingerprint-sha256={0}' -f $endHost.Fingerprint),
    ('host-fingerprint-match={0}' -f (Bool-Text $fingerprintMatch)),
    ('cpu-name={0}' -f $startHost.CpuName),
    ('computer-manufacturer={0}' -f $startHost.Manufacturer),
    ('computer-model={0}' -f $startHost.Model),
    ('logical-processor-count={0}' -f $startHost.LogicalProcessors),
    ('total-physical-memory-bytes={0}' -f $startHost.TotalPhysicalMemory),
    ('os-version={0}' -f $startHost.OsVersion),
    ('os-build={0}' -f $startHost.OsBuild),
    ('process-architecture={0}' -f $startHost.ProcessArchitecture),
    ('dotnet-sdk-version={0}' -f $startHost.DotnetSdkVersion),
    ('stopwatch-frequency={0}' -f $startHost.StopwatchFrequency),
    ('hypervisor-present={0}' -f (Bool-Text $startHost.HypervisorPresent)),
    ('start-active-power-scheme={0}' -f $startHost.PowerScheme),
    ('end-active-power-scheme={0}' -f $endHost.PowerScheme),
    ('active-power-scheme-stable={0}' -f (Bool-Text $powerStable)),
    'raw-system-uuid-retained=False',
    'machine-name-retained=False',
    'user-name-retained=False'
)
Write-Utf8Lines (Join-Path $artifactRoot '03-host-consistency-audit.txt') @(
    'status=PASS-SINGLE-HOST-INTEGRITY',
    ('host-fingerprint-sha256={0}' -f $startHost.Fingerprint),
    'runtime-profile=AMBIENT-UNSET',
    ('start-end-fingerprint-match={0}' -f (Bool-Text $fingerprintMatch)),
    ('active-power-scheme-stable={0}' -f (Bool-Text $powerStable)),
    'all-five-processes-same-host-required=True',
    'caller-dotnet-control-variables-unset=True',
    'caller-complus-aliases-unset=True',
    'cross-host-evidence-mixing-detected=False'
)
Write-Host 'FDPC2 child evidence collection completed.' -ForegroundColor Green
exit 0
