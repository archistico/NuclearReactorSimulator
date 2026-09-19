$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$ContractPath = Join-Path $Root 'eng\m10974-exact-v9-cross-host-stage-mass-flow-resolver-causal-seam-diagnostic3-contract.json'
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
    Check ([string]$Contract.schema -eq 'm10974-exact-v9-cross-host-stage-mass-flow-resolver-causal-seam-diagnostic3') 'contract schema drift'
    Check ([int]$Contract.revision -eq 0) 'contract revision drift'
    Check (-not [bool]$Contract.frozen_boundaries.production_change_authorized) 'production change must remain unauthorized'
    Check (-not [bool]$Contract.frozen_boundaries.fingerprint_v1_golden_change_authorized) 'V1 golden change must remain unauthorized'
    Check (-not [bool]$Contract.frozen_boundaries.exact_v9_golden_change_authorized) 'Exact-V9 golden change must remain unauthorized'
    Check (-not [bool]$Contract.frozen_boundaries.physics_change_authorized) 'physics change must remain unauthorized'
    Check (-not [bool]$Contract.frozen_boundaries.tolerance_change_authorized) 'tolerance change must remain unauthorized'
    Check (-not [bool]$Contract.frozen_boundaries.vr2_r3_change_authorized) 'VR2/R3 change must remain unauthorized'
    Check (-not [bool]$Contract.frozen_boundaries.workflow_change_authorized) 'workflow change must remain unauthorized'
    Check ([int]$Contract.baseline.first_divergent_step -eq 126) 'first divergent step drift'
    Check ([int]$Contract.baseline.commanded_ulp_delta -eq 19) 'Diagnostic 2 commanded ULP evidence drift'
    Check ([string]$Contract.baseline.diagnostic2_adjudication -eq 'PASS-AS-AUTHORED') 'Diagnostic 2 adjudication drift'

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
        Check ((@($ExactLines | Where-Object { $_.Trim() -eq $MarkerLine })).Count -eq 1) 'Diagnostic 3 marker cardinality drift'
        $Golden = [regex]::Escape([string]$Contract.baseline.exact_v9_frozen_aggregate)
        Check (([regex]::Matches($Text, $Golden)).Count -eq 1) 'frozen Exact-V9 anchor cardinality drift'
        Check (([regex]::Matches($Text, 'stage-resolver-selector.tsv')).Count -eq 1) 'selector resolver output binding drift'
        Check (([regex]::Matches($Text, 'stage-resolver-direct.tsv')).Count -eq 1) 'direct resolver output binding drift'
        Check (([regex]::Matches($Text, 'stage-resolver-valves-selector.tsv')).Count -eq 1) 'selector valve output binding drift'
        Check (([regex]::Matches($Text, 'stage-resolver-valves-direct.tsv')).Count -eq 1) 'direct valve output binding drift'
        Check (([regex]::Matches($Text, 'stageCausalSnapshot = engine.LatestCanonicalSnapshot;')).Count -eq 1) 'step-126 resolver output reference capture drift'
        Check (([regex]::Matches($Text, 'BuildExactV9StageMassFlowResolverDiagnostic')).Count -eq 2) 'resolver diagnostic builder cardinality drift'
        Check (([regex]::Matches($Text, 'BuildExactV9ValveFlowDiagnostic')).Count -eq 4) 'valve diagnostic builder cardinality drift'
        Check (([regex]::Matches($Text, 'Math\.Min\(hydraulicCandidate, admissionTrainCandidate\)')).Count -eq 1) 'visible resolver min semantic mirror drift'
        Check (([regex]::Matches($Text, 'Math\.Sqrt\(drivingPressurePa / expansionResistance\)')).Count -eq 1) 'pressure-driven sqrt semantic mirror drift'
        Check (([regex]::Matches($Text, '2d \* visibleCandidate \* deltaTime\.TotalSeconds')).Count -eq 1) 'drainable exact-tie threshold evidence drift'
        Check (([regex]::Matches($Text, 'definition\.Pipe\.Resistance\.PascalSecondsSquaredPerKilogramSquared')).Count -eq 1) 'valve base resistance capture drift'
        Check (([regex]::Matches($Text, 'snapshot\.FlowCoefficient\.Fraction \* snapshot\.FlowCoefficient\.Fraction')).Count -eq 1) 'valve coefficient-squared semantic mirror drift'
        Check (([regex]::Matches($Text, 'Math\.Abs\(snapshot\.PressureDifference\.Pascals\) / effectiveResistance')).Count -eq 1) 'valve squared-flow semantic mirror drift'
        Check (([regex]::Matches($Text, 'BitConverter\.DoubleToInt64Bits')).Count -eq 1) 'IEEE-754 bit capture helper drift'
        Check (([regex]::Matches($Text, 'foreach \(var entry in selector\.Entries\)')).Count -eq 0) 'all-step resolver selector traversal must remain absent'
        Check (([regex]::Matches($Text, 'foreach \(var entry in direct\.Entries\)')).Count -eq 0) 'all-step resolver direct traversal must remain absent'
    } else { Add-Problem 'Exact-V9 target test missing' }

    Validate-Manifest ([string]$Contract.manifests.src) 'src' ''
    Validate-Manifest ([string]$Contract.manifests.tests_excluding_target) 'tests' $TargetRelative

    Check ((Sha ([string]$Contract.evidence.local_diagnostic2_artifacts)) -eq ([string]$Contract.evidence.local_diagnostic2_artifacts_sha256).ToUpperInvariant()) 'local Diagnostic 2 evidence SHA drift'
    Check ((Sha ([string]$Contract.evidence.hosted_diagnostic2_outer)) -eq ([string]$Contract.evidence.hosted_diagnostic2_outer_sha256).ToUpperInvariant()) 'hosted Diagnostic 2 evidence SHA drift'
}

if ($Problems.Count -gt 0) {
    Write-Host 'M10974 Exact-V9 Cross-Host Stage Mass-Flow Resolver Causal-Seam Diagnostic 3 validator: FAIL'
    foreach ($Problem in $Problems) { Write-Host (" - " + $Problem) }
    throw ("validator found {0} problem(s)" -f $Problems.Count)
}
Write-Host 'M10974 Exact-V9 Cross-Host Stage Mass-Flow Resolver Causal-Seam Diagnostic 3 validator: PASS'
