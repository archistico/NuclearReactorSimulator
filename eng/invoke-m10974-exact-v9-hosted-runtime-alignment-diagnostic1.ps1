$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Write-AsciiFile([string]$Path, [string[]]$Lines) {
    [IO.File]::WriteAllLines($Path, $Lines, [Text.Encoding]::ASCII)
}

function Invoke-DotnetRequired([string[]]$Arguments, [string]$LogPath, [string]$Label) {
    $output = & dotnet @Arguments 2>&1
    $exitCode = $LASTEXITCODE
    $output | Tee-Object -FilePath $LogPath
    if ($exitCode -ne 0) {
        throw ("{0} failed with exit code {1}" -f $Label, $exitCode)
    }
}

$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

$OutputRoot = Join-Path $Root 'artifacts\ci\runtime-alignment'
$ProbeRoot = Join-Path $OutputRoot 'probe-sdk'
$TraceRoot = Join-Path $OutputRoot 'fingerprint-v1'
New-Item -ItemType Directory -Force -Path $OutputRoot, $ProbeRoot, $TraceRoot | Out-Null

$ProbeGlobalJson = Join-Path $ProbeRoot 'global.json'
$probeGlobal = @'
{
  "sdk": {
    "version": "10.0.105",
    "rollForward": "disable",
    "allowPrerelease": false
  },
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
'@
[IO.File]::WriteAllText($ProbeGlobalJson, $probeGlobal, (New-Object Text.UTF8Encoding($false)))

$Project = Join-Path $Root 'tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj'
$RuntimeConfig = Join-Path $Root 'tests\NuclearReactorSimulator.Application.Tests\bin\Debug\net10.0\NuclearReactorSimulator.Application.Tests.runtimeconfig.json'
$Method = 'NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalExactV9ProductionActivationDecisionTests.AuthoritativeExactV9_DefaultAndMissionPathsRemainHealthyConservativeDeterministicAndFailClosed'

$oldCi = $env:CI
$oldRollForward = $env:DOTNET_ROLL_FORWARD
$oldRuntimeFrameworkVersion = $env:RuntimeFrameworkVersion
$oldDiag = $env:NRS_FINGERPRINT_V1_DIAGNOSTICS_DIR
$oldPrereqs = $env:NRS_M10_FINAL_V9_ACTIVATION_PREREQUISITES_PASSED
$oldDecision = $env:NRS_M10_FINAL_V9_ACTIVATION_DECISION

$testExitCode = 999
try {
    $env:CI = 'true'
    $env:DOTNET_ROLL_FORWARD = 'Disable'
    $env:RuntimeFrameworkVersion = '10.0.5'
    $env:NRS_FINGERPRINT_V1_DIAGNOSTICS_DIR = $TraceRoot
    $env:NRS_M10_FINAL_V9_ACTIVATION_PREREQUISITES_PASSED = '1'
    $env:NRS_M10_FINAL_V9_ACTIVATION_DECISION = '1'

    Set-Location $ProbeRoot

    $sdkVersion = (& dotnet --version).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Unable to read dotnet SDK version.' }
    if ($sdkVersion -ne '10.0.105') { throw ("Expected SDK 10.0.105, got {0}" -f $sdkVersion) }

    $dotnetInfo = & dotnet --info 2>&1
    $dotnetInfo | Set-Content -LiteralPath (Join-Path $OutputRoot 'dotnet-info.txt') -Encoding utf8
    if ($LASTEXITCODE -ne 0) { throw 'dotnet --info failed.' }

    $runtimeList = & dotnet --list-runtimes 2>&1
    $runtimeList | Set-Content -LiteralPath (Join-Path $OutputRoot 'dotnet-list-runtimes.txt') -Encoding utf8
    if ($LASTEXITCODE -ne 0) { throw 'dotnet --list-runtimes failed.' }
    if (-not (($runtimeList -join "`n") -match '(?m)^Microsoft\.NETCore\.App\s+10\.0\.5\s+')) {
        throw 'Microsoft.NETCore.App 10.0.5 is not installed in the aligned SDK environment.'
    }

    Invoke-DotnetRequired @('restore', $Project, '-p:RuntimeFrameworkVersion=10.0.5', '-p:RollForward=Disable') (Join-Path $OutputRoot 'restore.log') 'runtime-alignment restore'
    Invoke-DotnetRequired @('build', $Project, '--configuration', 'Debug', '--no-restore', '-p:RuntimeFrameworkVersion=10.0.5', '-p:RollForward=Disable') (Join-Path $OutputRoot 'build.log') 'runtime-alignment build'

    if (-not (Test-Path -LiteralPath $RuntimeConfig -PathType Leaf)) {
        throw ('Expected runtimeconfig missing: ' + $RuntimeConfig)
    }
    Copy-Item -LiteralPath $RuntimeConfig -Destination (Join-Path $OutputRoot 'NuclearReactorSimulator.Application.Tests.runtimeconfig.json') -Force
    $rc = Get-Content -LiteralPath $RuntimeConfig -Raw | ConvertFrom-Json
    $runtimeOptions = $rc.runtimeOptions
    $frameworkVersion = $null
    if ($null -ne $runtimeOptions.framework) {
        if ([string]$runtimeOptions.framework.name -eq 'Microsoft.NETCore.App') {
            $frameworkVersion = [string]$runtimeOptions.framework.version
        }
    }
    if (($null -eq $frameworkVersion) -and ($null -ne $runtimeOptions.frameworks)) {
        foreach ($f in $runtimeOptions.frameworks) {
            if ([string]$f.name -eq 'Microsoft.NETCore.App') {
                $frameworkVersion = [string]$f.version
                break
            }
        }
    }
    if ($frameworkVersion -ne '10.0.5') {
        throw ("runtimeconfig Microsoft.NETCore.App version drift: expected 10.0.5, got {0}" -f $frameworkVersion)
    }
    if ([string]$runtimeOptions.rollForward -ne 'Disable') {
        throw ("runtimeconfig rollForward drift: expected Disable, got {0}" -f [string]$runtimeOptions.rollForward)
    }

    $testArgs = @(
        'test', '--project', $Project,
        '--configuration', 'Debug', '--no-build',
        '-p:RuntimeFrameworkVersion=10.0.5', '-p:RollForward=Disable',
        '--', '--explicit', 'only', '--filter-method', $Method, '--parallel', 'none'
    )
    $testOutput = & dotnet @testArgs 2>&1
    $testExitCode = $LASTEXITCODE
    $testOutput | Tee-Object -FilePath (Join-Path $OutputRoot 'exact-v9-runtime-aligned-test.log')

    $traceSummary = Get-ChildItem -LiteralPath $TraceRoot -Recurse -File -Filter 'exact-v9-transitive-summary.txt' -ErrorAction SilentlyContinue | Select-Object -First 1
    $traceFramework = 'MISSING'
    $traceAggregate = 'MISSING'
    if ($null -ne $traceSummary) {
        Copy-Item -LiteralPath $traceSummary.FullName -Destination (Join-Path $OutputRoot 'exact-v9-transitive-summary.txt') -Force
        $traceLines = Get-Content -LiteralPath $traceSummary.FullName
        $frameworkLine = $traceLines | Where-Object { $_ -like 'framework=*' } | Select-Object -First 1
        $aggregateLine = $traceLines | Where-Object { $_ -like 'selector-aggregate=*' } | Select-Object -First 1
        if ($null -ne $frameworkLine) { $traceFramework = $frameworkLine.Substring('framework='.Length) }
        if ($null -ne $aggregateLine) { $traceAggregate = $aggregateLine.Substring('selector-aggregate='.Length) }
    }

    $summary = @(
        'schema=m10974-exact-v9-hosted-runtime-alignment-diagnostic1-summary',
        ('sdk-version={0}' -f $sdkVersion),
        ('required-runtime=10.0.5'),
        ('runtime-roll-forward=Disable'),
        ('runtimeconfig-framework-version={0}' -f $frameworkVersion),
        ('runtimeconfig-roll-forward={0}' -f [string]$runtimeOptions.rollForward),
        ('test-exit-code={0}' -f $testExitCode),
        ('trace-framework={0}' -f $traceFramework),
        ('trace-selector-aggregate={0}' -f $traceAggregate),
        'frozen-exact-v9=7880AD580179B936C584EB0055BE663E0A1CFA65C5191B0DB8A7F3C514DB5418',
        ('classification={0}' -f $(if ($testExitCode -eq 0) { 'RUNTIME-ALIGNMENT-GREEN' } else { 'RUNTIME-ALIGNMENT-STILL-RED' }))
    )
    Write-AsciiFile (Join-Path $OutputRoot 'runtime-alignment-summary.txt') $summary

    if ($traceFramework -ne '.NET 10.0.5') {
        throw ("Trace did not execute on .NET 10.0.5: {0}" -f $traceFramework)
    }

    if ($testExitCode -ne 0) {
        Write-Host 'Hosted runtime alignment diagnostic completed with Exact-V9 still RED.' -ForegroundColor Yellow
        exit $testExitCode
    }

    if ($traceAggregate -ne '7880AD580179B936C584EB0055BE663E0A1CFA65C5191B0DB8A7F3C514DB5418') {
        throw ("Aligned runtime test passed but trace aggregate did not match frozen Exact-V9: {0}" -f $traceAggregate)
    }

    Write-Host 'Hosted runtime alignment diagnostic GREEN on SDK 10.0.105 / runtime 10.0.5.' -ForegroundColor Green
    exit 0
}
finally {
    Set-Location $Root
    $env:CI = $oldCi
    $env:DOTNET_ROLL_FORWARD = $oldRollForward
    $env:RuntimeFrameworkVersion = $oldRuntimeFrameworkVersion
    $env:NRS_FINGERPRINT_V1_DIAGNOSTICS_DIR = $oldDiag
    $env:NRS_M10_FINAL_V9_ACTIVATION_PREREQUISITES_PASSED = $oldPrereqs
    $env:NRS_M10_FINAL_V9_ACTIVATION_DECISION = $oldDecision
}
