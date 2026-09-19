param(
    [Parameter(Mandatory = $true)]
    [string]$RepositoryRoot
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

$utf8 = New-Object System.Text.UTF8Encoding($false)
$artifactRoot = Join-Path $RepositoryRoot 'artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1c-c4-runtime-configuration-impact-assessment1'
$workRoot = Join-Path $artifactRoot '_work'
$hostFingerprintVariable = 'NRS_A2_HOST_FINGERPRINT_SHA256'
$runtimeVariables = @(
    'DOTNET_TieredCompilation',
    'DOTNET_TieredPGO',
    'DOTNET_TC_QuickJit',
    'DOTNET_TC_QuickJitForLoops',
    'DOTNET_ReadyToRun'
)
$runtimeAliasVariables = @(
    'COMPlus_TieredCompilation',
    'COMPlus_TieredPGO',
    'COMPlus_TC_QuickJit',
    'COMPlus_TC_QuickJitForLoops',
    'COMPlus_ReadyToRun'
)
$a2Prefix = 'NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1C_C4_'
$exactOptIn = 'NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1C_C4_RUNTIME_CONFIGURATION_IMPACT_ASSESSMENT1'
$exactProfile = 'NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1C_C4_A2_PROFILE'
$exactRunIndex = 'NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1C_C4_A2_RUN_INDEX'
$exactMethod = 'NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.M10FinalVr2EngineeringRepairPlanning1Rp1cC4RuntimeConfigurationImpactAssessment1Tests.Rp1cC4RuntimeConfigurationImpactAssessment1_MeasuresOneFreshProfileProcess'
$simulationProject = Join-Path $RepositoryRoot 'tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj'
$applicationProject = Join-Path $RepositoryRoot 'tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj'
$hotPathSource = Join-Path $RepositoryRoot 'artifacts\m10972-hotfix2-ten-ms-hot-path'

function Write-Utf8Lines([string]$Path, [string[]]$Lines) {
    [System.IO.File]::WriteAllLines($Path, $Lines, $utf8)
}

function Write-Utf8Text([string]$Path, [string]$Text) {
    [System.IO.File]::WriteAllText($Path, $Text, $utf8)
}

function Bool-Text([bool]$Value) {
    if ($Value) { return 'True' }
    return 'False'
}

function Quote-Arg([string]$Value) {
    return '"' + $Value.Replace('"', '\"') + '"'
}

function Get-Sha256Text([string]$Text) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($Text)
        return ([System.BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '')
    }
    finally {
        $sha.Dispose()
    }
}

function Normalize-OneLine([object]$Value) {
    if ($null -eq $Value) { return '' }
    return ([string]$Value).Replace("`r", ' ').Replace("`n", ' ').Trim()
}

function Get-ActivePowerScheme {
    $output = & powercfg.exe /GETACTIVESCHEME 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to capture active Windows power scheme.'
    }
    $line = (($output | ForEach-Object { Normalize-OneLine $_ }) -join ' ').Trim()
    if ([string]::IsNullOrWhiteSpace($line)) {
        throw 'Active Windows power scheme output was empty.'
    }
    return $line
}

function Get-DotnetSdkVersion {
    $output = & dotnet --version 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to capture dotnet SDK version.'
    }
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

function Apply-ProfileEnvironment([System.Diagnostics.ProcessStartInfo]$StartInfo, [string]$ProfileId, [bool]$ExactProcess, [int]$RunIndex) {
    foreach ($key in @($StartInfo.EnvironmentVariables.Keys)) {
        if ($key.StartsWith($a2Prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            $StartInfo.EnvironmentVariables.Remove($key)
        }
    }

    foreach ($name in @($runtimeVariables + $runtimeAliasVariables)) {
        $StartInfo.EnvironmentVariables.Remove($name)
    }

    if ($ProfileId -eq 'EXPLICIT-REFERENCE-ALL-ON') {
        foreach ($name in $runtimeVariables) {
            $StartInfo.EnvironmentVariables[$name] = '1'
        }
    }
    elseif ($ProfileId -ne 'AMBIENT-UNSET') {
        throw ('Unknown A2 runtime profile: {0}' -f $ProfileId)
    }

    $StartInfo.EnvironmentVariables[$hostFingerprintVariable] = $script:StartHost.Fingerprint

    if ($ExactProcess) {
        $StartInfo.EnvironmentVariables[$exactOptIn] = '1'
        $StartInfo.EnvironmentVariables[$exactProfile] = $ProfileId
        $StartInfo.EnvironmentVariables[$exactRunIndex] = $RunIndex.ToString([System.Globalization.CultureInfo]::InvariantCulture)
    }
}

function Invoke-CapturedProcess(
    [string]$FileName,
    [string]$Arguments,
    [string]$WorkingDirectory,
    [string]$ProfileId,
    [bool]$ExactProcess = $false,
    [int]$RunIndex = 0
) {
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $FileName
    $psi.Arguments = $Arguments
    $psi.WorkingDirectory = $WorkingDirectory
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    Apply-ProfileEnvironment $psi $ProfileId $ExactProcess $RunIndex

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $psi
    if (-not $process.Start()) {
        throw ('Failed to start child process: {0}' -f $FileName)
    }
    $stdoutTask = $process.StandardOutput.ReadToEndAsync()
    $stderrTask = $process.StandardError.ReadToEndAsync()
    $process.WaitForExit()
    $stdout = $stdoutTask.Result
    $stderr = $stderrTask.Result
    $exitCode = $process.ExitCode
    $process.Dispose()

    [pscustomobject]@{
        ExitCode = $exitCode
        StdOut = $stdout
        StdErr = $stderr
        Combined = ($stdout + $(if ([string]::IsNullOrEmpty($stderr)) { '' } else { "`r`n[stderr]`r`n" + $stderr }))
    }
}

function Read-XunitReport([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        throw ('Structured xUnit report was not produced: {0}' -f $Path)
    }
    [xml]$xml = [System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8)
    $assemblies = @($xml.assemblies.assembly)
    if ($assemblies.Count -eq 0) {
        throw ('Structured xUnit report contains no assembly node: {0}' -f $Path)
    }
    $total = 0; $passed = 0; $failed = 0; $skipped = 0; $errors = 0; $notRun = 0
    foreach ($assembly in $assemblies) {
        $total += [int]$assembly.total
        $passed += [int]$assembly.passed
        $failed += [int]$assembly.failed
        $skipped += [int]$assembly.skipped
        if ($null -ne $assembly.errors -and [string]$assembly.errors -ne '') { $errors += [int]$assembly.errors }
        if ($null -ne $assembly.'not-run' -and [string]$assembly.'not-run' -ne '') { $notRun += [int]$assembly.'not-run' }
    }
    $executed = $passed + $failed + $skipped
    $accountingMode = $null
    if ($total -eq $executed) {
        $accountingMode = 'TOTAL-EQUALS-EXECUTED'
    }
    elseif ($total -eq ($executed + $notRun)) {
        $accountingMode = 'TOTAL-INCLUDES-NOT-RUN'
    }
    else {
        throw ('Structured xUnit report run-count accounting is inconsistent: total={0}; executed={1}; passed={2}; failed={3}; skipped={4}; not-run={5}; path={6}' -f $total,$executed,$passed,$failed,$skipped,$notRun,$Path)
    }
    $schemaVersion = [string]$xml.assemblies.'schema-version'
    [pscustomobject]@{ Total=$total; Executed=$executed; Passed=$passed; Failed=$failed; Skipped=$skipped; Errors=$errors; NotRun=$notRun; AccountingMode=$accountingMode; SchemaVersion=$schemaVersion }
}

function Invoke-XunitProject(
    [string]$Project,
    [string]$ProfileId,
    [string]$WorkDirectory,
    [string]$ReportName,
    [string[]]$RunnerArguments
) {
    if (Test-Path -LiteralPath $WorkDirectory) { Remove-Item -LiteralPath $WorkDirectory -Recurse -Force }
    New-Item -ItemType Directory -Path $WorkDirectory -Force | Out-Null
    $arguments = @('test', '--project', (Quote-Arg $Project), '--configuration', 'Release', '--no-build', '--results-directory', (Quote-Arg $WorkDirectory), '--minimum-expected-tests', '1', '--') + $RunnerArguments + @('--report-xunit', '--report-xunit-filename', $ReportName)
    $result = Invoke-CapturedProcess 'dotnet' ($arguments -join ' ') $RepositoryRoot $ProfileId
    $report = Join-Path $WorkDirectory $ReportName
    $counts = Read-XunitReport $report
    if ($counts.Executed -le 0) {
        throw ('Structured xUnit report discovered no executed tests for project/filter invocation: {0}; reported-total={1}; executed={2}; not-run={3}; accounting-mode={4}' -f $Project,$counts.Total,$counts.Executed,$counts.NotRun,$counts.AccountingMode)
    }
    [pscustomobject]@{ Result=$result; Counts=$counts; Report=$report }
}

function Add-Counts($Accumulator, $Counts) {
    $Accumulator.Total += $Counts.Total
    $Accumulator.Executed += $Counts.Executed
    $Accumulator.Passed += $Counts.Passed
    $Accumulator.Failed += $Counts.Failed
    $Accumulator.Skipped += $Counts.Skipped
    $Accumulator.Errors += $Counts.Errors
    $Accumulator.NotRun += $Counts.NotRun
}

function New-Counts {
    [pscustomobject]@{ Total=0; Executed=0; Passed=0; Failed=0; Skipped=0; Errors=0; NotRun=0 }
}

function Write-FamilyContract([string]$Path, [string]$Family, [string]$ProfileId, [string[]]$Extra) {
    $runtime = if ($ProfileId -eq 'AMBIENT-UNSET') { 'UNSET,UNSET,UNSET,UNSET,UNSET' } else { '1,1,1,1,1' }
    Write-Utf8Lines $Path (@(
        'gate=RP1C-C4-RUNTIME-CONFIGURATION-IMPACT-ASSESSMENT1',
        ('family={0}' -f $Family),
        ('profile-id={0}' -f $ProfileId),
        ('host-fingerprint-sha256={0}' -f $script:StartHost.Fingerprint),
        ('runtime-five-tuple={0}' -f $runtime),
        'same-physical-host-required=True',
        'engineering-negative-is-infrastructure-red=False',
        'automatic-selection-authorized=False',
        'production-runtime-change-authorized=False'
    ) + $Extra)
}

function Invoke-OrdinaryFamily([string]$ProfileId) {
    $dir = Join-Path $artifactRoot ('ordinary\profile-' + $ProfileId)
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
    Write-FamilyContract (Join-Path $dir '01-run-contract.txt') 'ORDINARY-RELEASE-SUITE' $ProfileId @('suite-run-count=1','structured-result-format=XUNIT-XML-V2PLUS')

    $projects = @(
        'tests\NuclearReactorSimulator.Domain.Tests\NuclearReactorSimulator.Domain.Tests.csproj',
        'tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj',
        'tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj',
        'tests\NuclearReactorSimulator.Infrastructure.Tests\NuclearReactorSimulator.Infrastructure.Tests.csproj',
        'tests\NuclearReactorSimulator.App.Tests\NuclearReactorSimulator.App.Tests.csproj'
    )
    $counts = New-Counts
    $log = New-Object System.Collections.Generic.List[string]
    $allExitZero = $true
    $index = 0
    foreach ($relative in $projects) {
        $index++
        $project = Join-Path $RepositoryRoot $relative
        $work = Join-Path $workRoot ('ordinary-' + $ProfileId + '-' + $index)
        $run = Invoke-XunitProject $project $ProfileId $work 'result.xml' @()
        Add-Counts $counts $run.Counts
        if ($run.Result.ExitCode -ne 0) { $allExitZero = $false }
        $log.Add(('===== {0} / exit {1} =====' -f $relative, $run.Result.ExitCode))
        $log.Add(('STRUCTURED-XUNIT schema={0}; accounting-mode={1}; reported-total={2}; executed={3}; not-run={4}' -f $run.Counts.SchemaVersion,$run.Counts.AccountingMode,$run.Counts.Total,$run.Counts.Executed,$run.Counts.NotRun))
        $log.Add($run.Result.Combined)
    }
    Write-Utf8Text (Join-Path $dir '02-run-log.txt') ($log -join "`r`n")
    $engineeringPass = $allExitZero -and $counts.Failed -eq 0 -and $counts.Errors -eq 0
    Write-Utf8Lines (Join-Path $dir '03-run-summary.txt') @(
        'status=COMPLETE-ENGINEERING-EVIDENCE',
        ('profile-id={0}' -f $ProfileId),
        ('host-fingerprint-sha256={0}' -f $script:StartHost.Fingerprint),
        ('reported-total={0}' -f $counts.Total),
        ('executed={0}' -f $counts.Executed),
        ('passed={0}' -f $counts.Passed),
        ('failed={0}' -f $counts.Failed),
        ('skipped={0}' -f $counts.Skipped),
        ('errors={0}' -f $counts.Errors),
        ('not-run={0}' -f $counts.NotRun),
        ('all-project-exit-codes-zero={0}' -f (Bool-Text $allExitZero)),
        ('engineering-pass={0}' -f (Bool-Text $engineeringPass)),
        'summary-source=XUNIT-XML-V2PLUS',
        'console-language-independent=True'
    )
}

function Invoke-ReplayFamily([string]$ProfileId) {
    $dir = Join-Path $artifactRoot ('replay\profile-' + $ProfileId)
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
    Write-FamilyContract (Join-Path $dir '01-run-contract.txt') 'M10-REPLAY-DETERMINISM' $ProfileId @('focused-classes=4','structured-result-format=XUNIT-XML-V2PLUS')

    $classes = @(
        [pscustomobject]@{ Project=$simulationProject; Class='NuclearReactorSimulator.Simulation.Tests.Runtime.SimulationReplayTests' },
        [pscustomobject]@{ Project=$simulationProject; Class='NuclearReactorSimulator.Simulation.Tests.Runtime.SimulationLongRunDeterminismTests' },
        [pscustomobject]@{ Project=$applicationProject; Class='NuclearReactorSimulator.Application.Tests.Scenarios.Challenges.Replay.M10965ChallengeReplayCheckpointClosureTests' },
        [pscustomobject]@{ Project=$applicationProject; Class='NuclearReactorSimulator.Application.Tests.ControlRoom.Automation.M10984ReplayCheckpointSameSeedIntegrityTests' }
    )
    $counts = New-Counts
    $log = New-Object System.Collections.Generic.List[string]
    $allExitZero = $true
    $index = 0
    foreach ($item in $classes) {
        $index++
        $work = Join-Path $workRoot ('replay-' + $ProfileId + '-' + $index)
        $run = Invoke-XunitProject $item.Project $ProfileId $work 'result.xml' @('--filter-class', $item.Class, '--parallel', 'none')
        Add-Counts $counts $run.Counts
        if ($run.Result.ExitCode -ne 0) { $allExitZero = $false }
        $log.Add(('===== {0} / exit {1} =====' -f $item.Class, $run.Result.ExitCode))
        $log.Add(('STRUCTURED-XUNIT schema={0}; accounting-mode={1}; reported-total={2}; executed={3}; not-run={4}' -f $run.Counts.SchemaVersion,$run.Counts.AccountingMode,$run.Counts.Total,$run.Counts.Executed,$run.Counts.NotRun))
        $log.Add($run.Result.Combined)
    }
    Write-Utf8Text (Join-Path $dir '02-run-log.txt') ($log -join "`r`n")
    $engineeringPass = $allExitZero -and $counts.Failed -eq 0 -and $counts.Errors -eq 0
    Write-Utf8Lines (Join-Path $dir '03-run-summary.txt') @(
        'status=COMPLETE-ENGINEERING-EVIDENCE',
        ('profile-id={0}' -f $ProfileId),
        ('host-fingerprint-sha256={0}' -f $script:StartHost.Fingerprint),
        ('reported-total={0}' -f $counts.Total),
        ('executed={0}' -f $counts.Executed),
        ('passed={0}' -f $counts.Passed),
        ('failed={0}' -f $counts.Failed),
        ('skipped={0}' -f $counts.Skipped),
        ('errors={0}' -f $counts.Errors),
        ('not-run={0}' -f $counts.NotRun),
        ('all-class-exit-codes-zero={0}' -f (Bool-Text $allExitZero)),
        ('engineering-pass={0}' -f (Bool-Text $engineeringPass)),
        'summary-source=XUNIT-XML-V2PLUS',
        'console-language-independent=True'
    )
}

function Invoke-HotPathProcess([string]$ProfileId, [int]$RunIndex) {
    $dir = Join-Path $artifactRoot ('non-vr2\profile-' + $ProfileId + '\process-' + $RunIndex.ToString('00'))
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
    if (Test-Path -LiteralPath $hotPathSource) { Remove-Item -LiteralPath $hotPathSource -Recurse -Force }
    $work = Join-Path $workRoot ('hotpath-' + $ProfileId + '-' + $RunIndex)
    $run = Invoke-XunitProject $applicationProject $ProfileId $work 'result.xml' @('--explicit', 'only', '--filter-class', 'NuclearReactorSimulator.Application.Tests.Milestones.M10972Hotfix2TenMillisecondHotPathHardeningTests', '--parallel', 'none')
    $engineeringPass = $run.Result.ExitCode -eq 0 -and $run.Counts.Failed -eq 0 -and $run.Counts.Errors -eq 0
    Write-FamilyContract (Join-Path $dir '01-run-contract.txt') 'M10972-HOT-PATH' $ProfileId @(
        ('run-index={0}' -f $RunIndex),
        ('test-reported-total={0}' -f $run.Counts.Total),
        ('test-executed={0}' -f $run.Counts.Executed),
        ('test-passed={0}' -f $run.Counts.Passed),
        ('test-failed={0}' -f $run.Counts.Failed),
        ('test-skipped={0}' -f $run.Counts.Skipped),
        ('test-errors={0}' -f $run.Counts.Errors),
        ('test-not-run={0}' -f $run.Counts.NotRun),
        ('test-xunit-schema-version={0}' -f $run.Counts.SchemaVersion),
        ('test-accounting-mode={0}' -f $run.Counts.AccountingMode),
        ('child-exit-code={0}' -f $run.Result.ExitCode),
        ('engineering-pass={0}' -f (Bool-Text $engineeringPass)),
        ('runner-output-sha256={0}' -f (Get-Sha256Text $run.Result.Combined)),
        'summary-source=XUNIT-XML-V2PLUS'
    )

    $sourceSummary = Join-Path $hotPathSource '01-m10972-hotfix2-ten-millisecond-hot-path-hardening.summary.txt'
    $sourceMetrics = Join-Path $hotPathSource '02-m10972-hotfix2-ten-millisecond-hot-path-metrics.csv'
    if ((Test-Path -LiteralPath $sourceSummary) -and (Test-Path -LiteralPath $sourceMetrics)) {
        Copy-Item -LiteralPath $sourceSummary -Destination (Join-Path $dir '02-hotpath-summary.txt') -Force
        Copy-Item -LiteralPath $sourceMetrics -Destination (Join-Path $dir '03-hotpath-metrics.csv') -Force
    }
    elseif ($engineeringPass) {
        throw ('Hot-path process passed but expected source evidence was not produced for {0} run {1}.' -f $ProfileId, $RunIndex)
    }
    else {
        Write-Utf8Lines (Join-Path $dir '02-hotpath-summary.txt') @(
            'status=SOURCE-EVIDENCE-NOT-PRODUCED-DUE-ENGINEERING-FAILURE',
            ('profile-id={0}' -f $ProfileId),
            ('run-index={0}' -f $RunIndex),
            ('child-exit-code={0}' -f $run.Result.ExitCode),
            ('failed={0}' -f $run.Counts.Failed),
            ('errors={0}' -f $run.Counts.Errors)
        )
        Write-Utf8Lines (Join-Path $dir '03-hotpath-metrics.csv') @(
            'metric,optimized,reference,ratio',
            'engineering_failure_no_metrics,NA,NA,NA'
        )
    }
    if (Test-Path -LiteralPath $hotPathSource) { Remove-Item -LiteralPath $hotPathSource -Recurse -Force }
}

function Invoke-ExactProcess([string]$ProfileId, [int]$RunIndex) {
    $work = Join-Path $workRoot ('exact-' + $ProfileId + '-' + $RunIndex)
    if (Test-Path -LiteralPath $work) { Remove-Item -LiteralPath $work -Recurse -Force }
    New-Item -ItemType Directory -Path $work -Force | Out-Null
    $arguments = 'test --project {0} --configuration Release --no-build -- --explicit only --filter-method {1} --parallel none' -f (Quote-Arg $simulationProject), $exactMethod
    $run = Invoke-CapturedProcess 'dotnet' $arguments $RepositoryRoot $ProfileId $true $RunIndex
    Write-Utf8Text (Join-Path $work 'run.log') $run.Combined
    if ($run.ExitCode -ne 0) {
        throw ('Exact-v9 focused process failed infrastructure/harness assertions: {0} run {1}. Temporary log: {2}' -f $ProfileId, $RunIndex, (Join-Path $work 'run.log'))
    }
    $processDir = Join-Path $artifactRoot ('profile-' + $ProfileId + '\process-' + $RunIndex.ToString('00'))
    foreach ($name in @('01-process-contract.txt','02-exact-v9-call-timing.csv','03-runtime-context.txt','04-process-summary.txt')) {
        if (-not (Test-Path -LiteralPath (Join-Path $processDir $name))) {
            throw ('Exact-v9 process evidence missing: {0} / {1}' -f $processDir, $name)
        }
    }
}

if (Test-Path -LiteralPath $artifactRoot) { Remove-Item -LiteralPath $artifactRoot -Recurse -Force }
New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
New-Item -ItemType Directory -Path $workRoot -Force | Out-Null

$script:StartHost = Get-HostSnapshot

# Exact-v9 profile comparison in counterbalanced blocked order.
$schedule = @(
    @('AMBIENT-UNSET',1), @('EXPLICIT-REFERENCE-ALL-ON',1),
    @('EXPLICIT-REFERENCE-ALL-ON',2), @('AMBIENT-UNSET',2),
    @('AMBIENT-UNSET',3), @('EXPLICIT-REFERENCE-ALL-ON',3),
    @('EXPLICIT-REFERENCE-ALL-ON',4), @('AMBIENT-UNSET',4),
    @('AMBIENT-UNSET',5), @('EXPLICIT-REFERENCE-ALL-ON',5)
)
foreach ($entry in $schedule) {
    Invoke-ExactProcess ([string]$entry[0]) ([int]$entry[1])
}

# Project-wide functional and determinism impact families.
Invoke-OrdinaryFamily 'AMBIENT-UNSET'
Invoke-OrdinaryFamily 'EXPLICIT-REFERENCE-ALL-ON'
Invoke-ReplayFamily 'AMBIENT-UNSET'
Invoke-ReplayFamily 'EXPLICIT-REFERENCE-ALL-ON'

# Representative non-VR2 performance owner, counterbalanced by run.
foreach ($entry in @(
    @('AMBIENT-UNSET',1), @('EXPLICIT-REFERENCE-ALL-ON',1),
    @('EXPLICIT-REFERENCE-ALL-ON',2), @('AMBIENT-UNSET',2),
    @('AMBIENT-UNSET',3), @('EXPLICIT-REFERENCE-ALL-ON',3)
)) {
    Invoke-HotPathProcess ([string]$entry[0]) ([int]$entry[1])
}

$endHost = Get-HostSnapshot
$fingerprintMatch = [string]::Equals($script:StartHost.Fingerprint, $endHost.Fingerprint, [System.StringComparison]::Ordinal)
$powerStable = [string]::Equals($script:StartHost.PowerScheme, $endHost.PowerScheme, [System.StringComparison]::Ordinal)
Write-Utf8Lines (Join-Path $artifactRoot '02-execution-host-provenance.txt') @(
    'status=COMPLETE-HOST-PROVENANCE-CAPTURE',
    ('start-host-fingerprint-sha256={0}' -f $script:StartHost.Fingerprint),
    ('end-host-fingerprint-sha256={0}' -f $endHost.Fingerprint),
    ('host-fingerprint-match={0}' -f (Bool-Text $fingerprintMatch)),
    ('cpu-name={0}' -f $script:StartHost.CpuName),
    ('computer-manufacturer={0}' -f $script:StartHost.Manufacturer),
    ('computer-model={0}' -f $script:StartHost.Model),
    ('logical-processor-count={0}' -f $script:StartHost.LogicalProcessors),
    ('total-physical-memory-bytes={0}' -f $script:StartHost.TotalPhysicalMemory),
    ('os-version={0}' -f $script:StartHost.OsVersion),
    ('os-build={0}' -f $script:StartHost.OsBuild),
    ('process-architecture={0}' -f $script:StartHost.ProcessArchitecture),
    ('dotnet-sdk-version={0}' -f $script:StartHost.DotnetSdkVersion),
    ('stopwatch-frequency={0}' -f $script:StartHost.StopwatchFrequency),
    ('hypervisor-present={0}' -f (Bool-Text $script:StartHost.HypervisorPresent)),
    ('start-active-power-scheme={0}' -f $script:StartHost.PowerScheme),
    ('end-active-power-scheme={0}' -f $endHost.PowerScheme),
    ('active-power-scheme-stable={0}' -f (Bool-Text $powerStable)),
    'raw-system-uuid-retained=False',
    'machine-name-retained=False',
    'user-name-retained=False'
)

if (-not $fingerprintMatch) { throw 'INFRASTRUCTURE-RED-HOST-PROVENANCE-MISMATCH' }
if (-not $powerStable) { throw 'INFRASTRUCTURE-RED-POWER-SCHEME-CHANGED' }

if (Test-Path -LiteralPath $workRoot) { Remove-Item -LiteralPath $workRoot -Recurse -Force }
Write-Host 'A2 child evidence collection completed.'
