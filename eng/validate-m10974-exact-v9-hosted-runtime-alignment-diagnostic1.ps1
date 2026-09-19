$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$ContractPath = Join-Path $Root 'eng\m10974-exact-v9-hosted-runtime-alignment-diagnostic1-contract.json'
$Problems = New-Object 'System.Collections.Generic.List[string]'

function Add-Problem([string]$Message) { [void]$Problems.Add($Message) }
function Check([bool]$Condition, [string]$Message) { if (-not $Condition) { Add-Problem $Message } }
function Sha([string]$RelativePath) {
    $Path = Join-Path $Root ($RelativePath -replace '/', '\')
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return $null }
    $Stream = $null
    $Hasher = $null
    try {
        $Stream = [System.IO.File]::OpenRead($Path)
        $Hasher = [System.Security.Cryptography.SHA256]::Create()
        $Bytes = $Hasher.ComputeHash($Stream)
        return ([System.BitConverter]::ToString($Bytes)).Replace('-', '').ToUpperInvariant()
    } finally {
        if ($null -ne $Stream) { $Stream.Dispose() }
        if ($null -ne $Hasher) { $Hasher.Dispose() }
    }
}
function Validate-Manifest([string]$ManifestRelativePath, [string]$TreeRelativePath, [string]$ExcludedRelativePath) {
    $ManifestPath = Join-Path $Root ($ManifestRelativePath -replace '/', '\')
    if (-not (Test-Path -LiteralPath $ManifestPath -PathType Leaf)) {
        Add-Problem "manifest missing: $ManifestRelativePath"
        return
    }
    $Rows = Import-Csv -LiteralPath $ManifestPath -Delimiter "`t"
    $Expected = @{}
    foreach ($Row in $Rows) { $Expected[[string]$Row.path] = ([string]$Row.sha256).ToUpperInvariant() }
    $TreeRoot = Join-Path $Root ($TreeRelativePath -replace '/', '\')
    $ActualPaths = @()
    if (Test-Path -LiteralPath $TreeRoot -PathType Container) {
        $ActualPaths = @(Get-ChildItem -LiteralPath $TreeRoot -File -Recurse | ForEach-Object {
            $rel = $_.FullName.Substring($Root.Length + 1).Replace('\','/')
            $isGeneratedBuildOutput = $rel -match '(^|/)(bin|obj)(/|$)'
            if ((-not $isGeneratedBuildOutput) -and ($rel -ne $ExcludedRelativePath)) { $rel }
        })
    }
    Check ($ActualPaths.Count -eq $Expected.Count) "manifest cardinality drift: $ManifestRelativePath"
    foreach ($RelativePath in $ActualPaths) {
        if (-not $Expected.ContainsKey($RelativePath)) {
            Add-Problem "unfrozen file present: $RelativePath"
            continue
        }
        $ActualSha = Sha $RelativePath
        if ($ActualSha -ne $Expected[$RelativePath]) { Add-Problem "frozen file drift: $RelativePath" }
    }
    foreach ($RelativePath in $Expected.Keys) {
        if ($ActualPaths -notcontains $RelativePath) { Add-Problem "frozen file missing: $RelativePath" }
    }
}
function Exact-Line-Count([string]$Text, [string]$Line) {
    $Lines = [regex]::Split($Text, '\r?\n')
    return (@($Lines | Where-Object { $_.Trim() -eq $Line })).Count
}

if (-not (Test-Path -LiteralPath $ContractPath -PathType Leaf)) {
    Add-Problem 'contract missing'
    $Contract = $null
} else {
    try { $Contract = Get-Content -LiteralPath $ContractPath -Raw | ConvertFrom-Json }
    catch { Add-Problem ('contract JSON invalid: ' + $_.Exception.Message); $Contract = $null }
}

if ($null -ne $Contract) {
    Check ([string]$Contract.schema -eq 'm10974-exact-v9-hosted-runtime-alignment-diagnostic1') 'contract schema drift'
    Check ([string]$Contract.status -eq 'TEST-ONLY-HOSTED-RUNTIME-ALIGNMENT-DIAGNOSTIC') 'contract status drift'
    Check (-not [bool]$Contract.frozen_boundaries.production_change_authorized) 'production change must remain unauthorized'
    Check (-not [bool]$Contract.frozen_boundaries.test_source_change_authorized) 'test source change must remain unauthorized'
    Check (-not [bool]$Contract.frozen_boundaries.fingerprint_v1_golden_change_authorized) 'V1 golden change must remain unauthorized'
    Check (-not [bool]$Contract.frozen_boundaries.exact_v9_golden_change_authorized) 'Exact-V9 golden change must remain unauthorized'
    Check (-not [bool]$Contract.frozen_boundaries.physics_change_authorized) 'physics change must remain unauthorized'
    Check (-not [bool]$Contract.frozen_boundaries.tolerance_change_authorized) 'tolerance change must remain unauthorized'
    Check (-not [bool]$Contract.frozen_boundaries.ordinary_ci_contract_change_authorized) 'permanent ordinary CI change must remain unauthorized'
    Check (-not [bool]$Contract.frozen_boundaries.repository_global_json_change_authorized) 'repository global.json change must remain unauthorized'
    Check (-not [bool]$Contract.frozen_boundaries.vr2_r3_change_authorized) 'VR2/R3 change must remain unauthorized'
    Check ([string]$Contract.alignment.sdk_version -eq '10.0.105') 'aligned SDK contract drift'
    Check ([string]$Contract.alignment.runtime_version -eq '10.0.5') 'aligned runtime contract drift'
    Check ([string]$Contract.alignment.runtime_roll_forward -eq 'Disable') 'runtime roll-forward contract drift'
    Check ([bool]$Contract.decision.no_third_branch) 'runtime-alignment decision must remain binary'

    foreach ($Property in $Contract.frozen_sha256.PSObject.Properties) {
        $Actual = Sha ([string]$Property.Name)
        Check ($Actual -eq ([string]$Property.Value).ToUpperInvariant()) ('frozen SHA drift: ' + [string]$Property.Name)
    }

    Validate-Manifest ([string]$Contract.manifests.src) 'src' ''
    $TargetRelative = 'tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/M10FinalExactV9ProductionActivationDecisionTests.cs'
    Validate-Manifest ([string]$Contract.manifests.tests_excluding_target) 'tests' $TargetRelative

    $WorkflowPath = Join-Path $Root (([string]$Contract.workflow.path) -replace '/', '\')
    if (-not (Test-Path -LiteralPath $WorkflowPath -PathType Leaf)) {
        Add-Problem 'runtime alignment workflow missing'
    } else {
        $WorkflowText = [IO.File]::ReadAllText($WorkflowPath, [Text.Encoding]::UTF8).Replace('\','/')
        foreach ($Marker in $Contract.workflow.markers) {
            Check ((Exact-Line-Count $WorkflowText ('# ' + [string]$Marker)) -eq 1) ('workflow marker cardinality drift: ' + [string]$Marker)
        }
        Check ([regex]::IsMatch($WorkflowText, '(?m)^\s*uses:\s*actions/checkout@v7\s*$')) 'workflow checkout action drift'
        Check ([regex]::IsMatch($WorkflowText, '(?m)^\s*uses:\s*actions/setup-dotnet@v6\s*$')) 'workflow setup-dotnet action drift'
        Check ([regex]::IsMatch($WorkflowText, "(?m)^\s*dotnet-version:\s*'10\.0\.105'\s*$")) 'workflow SDK pin drift'
        Check ([regex]::IsMatch($WorkflowText, '(?m)^\s*uses:\s*actions/upload-artifact@v7\s*$')) 'workflow upload action drift'
        Check ([regex]::IsMatch($WorkflowText, '(?m)^\s*name:\s*exact-v9-runtime-alignment-diagnostic\s*$')) 'workflow artifact name drift'
        Check ([regex]::IsMatch($WorkflowText, '(?m)^\s*if:\s*always\(\)\s*$')) 'workflow artifact upload must remain always()'
        Check (([regex]::Matches($WorkflowText, 'invoke-m10974-exact-v9-hosted-runtime-alignment-diagnostic1\.ps1')).Count -eq 1) 'runtime alignment invoker cardinality drift'
        Check (([regex]::Matches($WorkflowText, 'eng/ci-ordinary\.cmd')).Count -eq 0) 'one-shot workflow must not invoke permanent ordinary CI'
    }

    $InvokerRelative = 'eng/invoke-m10974-exact-v9-hosted-runtime-alignment-diagnostic1.ps1'
    $InvokerPath = Join-Path $Root ($InvokerRelative -replace '/', '\')
    if (-not (Test-Path -LiteralPath $InvokerPath -PathType Leaf)) {
        Add-Problem 'runtime alignment invoker missing'
    } else {
        $InvokerText = [IO.File]::ReadAllText($InvokerPath, [Text.Encoding]::UTF8)
        Check (([regex]::Matches($InvokerText, "'10\.0\.105'")).Count -ge 1) 'invoker SDK pin missing'
        Check (([regex]::Matches($InvokerText, "RuntimeFrameworkVersion = '10\.0\.5'")).Count -eq 1) 'invoker runtime version environment pin drift'
        Check (([regex]::Matches($InvokerText, "DOTNET_ROLL_FORWARD = 'Disable'")).Count -eq 1) 'invoker runtime roll-forward pin drift'
        Check (([regex]::Matches($InvokerText, '-p:RuntimeFrameworkVersion=10\.0\.5')).Count -eq 3) 'restore/build/test runtime framework binding drift'
        Check (([regex]::Matches($InvokerText, '-p:RollForward=Disable')).Count -eq 3) 'restore/build/test roll-forward binding drift'
        Check (([regex]::Matches($InvokerText, 'runtimeOptions\.rollForward')).Count -ge 2) 'runtimeconfig roll-forward verification missing'
        Check (([regex]::Matches($InvokerText, [regex]::Escape([string]$Contract.alignment.target_method))).Count -eq 1) 'Exact-V9 target method binding drift'
        Check (([regex]::Matches($InvokerText, [regex]::Escape([string]$Contract.baseline.frozen_exact_v9))).Count -eq 2) 'frozen Exact-V9 trace assertion drift'
        Check (([regex]::Matches($InvokerText, "traceFramework -ne '\.NET 10\.0\.5'")).Count -eq 1) 'executed framework assertion missing'
        Check (([regex]::Matches($InvokerText, 'Get-FileHash')).Count -eq 0) 'invoker must not depend on Get-FileHash'
    }

    $DocRelative = 'docs/M10974_EXACT_V9_HOSTED_RUNTIME_ALIGNMENT_DIAGNOSTIC1.md'
    $DocPath = Join-Path $Root ($DocRelative -replace '/', '\')
    if (-not (Test-Path -LiteralPath $DocPath -PathType Leaf)) {
        Add-Problem 'runtime alignment document missing'
    } else {
        $DocText = [IO.File]::ReadAllText($DocPath, [Text.Encoding]::UTF8)
        Check ((Exact-Line-Count $DocText '<!-- NRS-MARKER:M10974-EXACT-V9-HOSTED-RUNTIME-ALIGNMENT-DIAGNOSTIC1 -->') -eq 1) 'runtime alignment document marker cardinality drift'
    }
}

$ValidatorBytes = [IO.File]::ReadAllBytes($MyInvocation.MyCommand.Path)
Check ((@($ValidatorBytes | Where-Object { $_ -gt 127 })).Count -eq 0) 'validator must remain ASCII-only'
$RunnerRelative = 'scripts/run-m10974-exact-v9-hosted-runtime-alignment-diagnostic1.cmd'
$RunnerPath = Join-Path $Root ($RunnerRelative -replace '/', '\')
if (-not (Test-Path -LiteralPath $RunnerPath -PathType Leaf)) {
    Add-Problem 'local static runner missing'
} else {
    $RunnerBytes = [IO.File]::ReadAllBytes($RunnerPath)
    Check ((@($RunnerBytes | Where-Object { $_ -gt 127 })).Count -eq 0) 'local static runner must remain ASCII-only'
}

if ($Problems.Count -gt 0) {
    Write-Host 'M10974 Exact-V9 Hosted Runtime Alignment Diagnostic 1 validator: FAIL'
    foreach ($Problem in $Problems) { Write-Host (' - ' + $Problem) }
    throw ("validator found {0} problem(s)" -f $Problems.Count)
}

Write-Host 'M10974 Exact-V9 Hosted Runtime Alignment Diagnostic 1 validator: PASS'
Write-Host 'Permanent ordinary CI, production/test source, frozen golden and VR2/R3 remain unchanged.'
