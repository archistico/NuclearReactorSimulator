$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$ContractPath = Join-Path $Root 'eng\m10974-exact-v9-cross-host-admission-train-shared-node-provenance-diagnostic4-contract.json'
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

if (-not (Test-Path -LiteralPath $ContractPath -PathType Leaf)) {
    Add-Problem 'contract missing'
} else {
    try { $Contract = Get-Content -LiteralPath $ContractPath -Raw | ConvertFrom-Json }
    catch { Add-Problem ('contract JSON invalid: ' + $_.Exception.Message); $Contract = $null }
}

if ($null -ne $Contract) {
    Check ([string]$Contract.schema -eq 'm10974-exact-v9-cross-host-admission-train-shared-node-provenance-diagnostic4') 'contract schema drift'
    Check ([int]$Contract.revision -eq 0) 'contract revision drift'
    Check (-not [bool]$Contract.frozen_boundaries.production_change_authorized) 'production change must remain unauthorized'
    Check (-not [bool]$Contract.frozen_boundaries.fingerprint_v1_golden_change_authorized) 'V1 golden change must remain unauthorized'
    Check (-not [bool]$Contract.frozen_boundaries.exact_v9_golden_change_authorized) 'Exact-V9 golden change must remain unauthorized'
    Check (-not [bool]$Contract.frozen_boundaries.physics_change_authorized) 'physics change must remain unauthorized'
    Check (-not [bool]$Contract.frozen_boundaries.tolerance_change_authorized) 'tolerance change must remain unauthorized'
    Check (-not [bool]$Contract.frozen_boundaries.vr2_r3_change_authorized) 'VR2/R3 change must remain unauthorized'
    Check (-not [bool]$Contract.frozen_boundaries.workflow_change_authorized) 'workflow change must remain unauthorized'
    Check ([int]$Contract.baseline.first_divergent_step -eq 126) 'first divergent step drift'
    Check ([string]$Contract.baseline.diagnostic3_adjudication -eq 'PASS-AS-AUTHORED') 'Diagnostic 3 adjudication drift'
    Check ([string]$Contract.baseline.selected_visible_limiter -eq 'ADMISSION_TRAIN') 'visible limiter drift'
    Check ([string]$Contract.baseline.selected_valve_limiter -eq 'STOP') 'STOP limiter drift'
    Check ([int]$Contract.baseline.reconstructed_stop_out_pressure_ulp_delta -eq 1) 'stop-out one-ULP evidence drift'

    foreach ($Property in $Contract.critical_sha256.PSObject.Properties) {
        $Actual = Sha ([string]$Property.Name)
        Check ($Actual -eq ([string]$Property.Value).ToUpperInvariant()) ("critical SHA drift: " + [string]$Property.Name)
    }

    $TargetRelative = 'tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/M10FinalExactV9ProductionActivationDecisionTests.cs'
    $Target = Join-Path $Root ($TargetRelative -replace '/', '\')
    if (Test-Path -LiteralPath $Target -PathType Leaf) {
        $Text = [IO.File]::ReadAllText($Target, [Text.Encoding]::UTF8)
        $ExactLines = [regex]::Split($Text, '\r?\n')
        $MarkerLine = '// ' + [string]$Contract.diagnostic.marker
        Check ((@($ExactLines | Where-Object { $_.Trim() -eq $MarkerLine })).Count -eq 1) 'Diagnostic 4 marker cardinality drift'
        $Golden = [regex]::Escape([string]$Contract.baseline.exact_v9_frozen_aggregate)
        Check (([regex]::Matches($Text, $Golden)).Count -eq 1) 'frozen Exact-V9 anchor cardinality drift'
        foreach ($Name in @(
            'stage-shared-node-selector.tsv',
            'stage-shared-node-direct.tsv',
            'stage-shared-node-energy-selector.tsv',
            'stage-shared-node-energy-direct.tsv')) {
            Check (([regex]::Matches($Text, [regex]::Escape($Name))).Count -eq 1) ("Diagnostic 4 output binding drift: " + $Name)
        }
        Check (([regex]::Matches($Text, 'stageCausalSnapshot = engine\.LatestCanonicalSnapshot;')).Count -eq 1) 'step-126 snapshot reference capture drift'
        Check (([regex]::Matches($Text, 'BuildExactV9AdmissionTrainSharedNodeDiagnostic')).Count -eq 2) 'shared-node diagnostic builder cardinality drift'
        Check (([regex]::Matches($Text, 'BuildExactV9ValveEnergyDiagnostic')).Count -eq 4) 'shared-node valve-energy builder cardinality drift'
        Check (([regex]::Matches($Text, 'var controlOutPressurePa = turbineInletPressurePa \+ admissionPressureDifferencePa;')).Count -eq 1) 'control-out pressure reconstruction drift'
        Check (([regex]::Matches($Text, 'var stopOutPressurePa = controlOutPressurePa \+ controlPressureDifferencePa;')).Count -eq 1) 'stop-out pressure reconstruction drift'
        Check (([regex]::Matches($Text, 'var headerPressurePa = stopOutPressurePa \+ stopPressureDifferencePa;')).Count -eq 1) 'header pressure reconstruction drift'
        Check (([regex]::Matches($Text, 'internalEnergyFlowW / massFlowKgPerS')).Count -eq 1) 'specific internal-energy reconstruction drift'
        Check (([regex]::Matches($Text, 'flowWorkRateW / massFlowKgPerS')).Count -eq 1) 'specific flow-work reconstruction drift'
        Check (([regex]::Matches($Text, 'advectedEnergyFlowW / massFlowKgPerS')).Count -eq 1) 'specific advected-energy reconstruction drift'
        Check (([regex]::Matches($Text, 'reconstructedUpstreamPressurePa / specificFlowWorkJPerKg')).Count -eq 1) 'density provenance reconstruction drift'
        Check (([regex]::Matches($Text, 'foreach \(var entry in selector\.Entries\)')).Count -eq 0) 'all-step Diagnostic 4 selector traversal must remain absent'
        Check (([regex]::Matches($Text, 'foreach \(var entry in direct\.Entries\)')).Count -eq 0) 'all-step Diagnostic 4 direct traversal must remain absent'
    } else { Add-Problem 'Exact-V9 target test missing' }

    Validate-Manifest ([string]$Contract.manifests.src) 'src' ''
    Validate-Manifest ([string]$Contract.manifests.tests_excluding_target) 'tests' $TargetRelative

    Check ((Sha ([string]$Contract.evidence.local_diagnostic3_artifacts)) -eq ([string]$Contract.evidence.local_diagnostic3_artifacts_sha256).ToUpperInvariant()) 'local Diagnostic 3 evidence SHA drift'
    Check ((Sha ([string]$Contract.evidence.hosted_diagnostic3_outer)) -eq ([string]$Contract.evidence.hosted_diagnostic3_outer_sha256).ToUpperInvariant()) 'hosted Diagnostic 3 evidence SHA drift'
}

if ($Problems.Count -gt 0) {
    Write-Host 'M10974 Exact-V9 Cross-Host Admission-Train Shared-Node Provenance Diagnostic 4 validator: FAIL'
    foreach ($Problem in $Problems) { Write-Host (" - " + $Problem) }
    throw ("validator found {0} problem(s)" -f $Problems.Count)
}
Write-Host 'M10974 Exact-V9 Cross-Host Admission-Train Shared-Node Provenance Diagnostic 4 validator: PASS'
