$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$ContractPath = Join-Path $Root 'eng\m10974-exact-v9-cross-host-stage-causal-seam-diagnostic2-contract.json'
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
    Check ([string]$Contract.schema -eq 'm10974-exact-v9-cross-host-stage-causal-seam-diagnostic2') 'contract schema drift'
    Check ([int]$Contract.revision -eq 1) 'contract revision drift'
    Check (-not [bool]$Contract.frozen_boundaries.production_change_authorized) 'production change must remain unauthorized'
    Check (-not [bool]$Contract.frozen_boundaries.fingerprint_v1_golden_change_authorized) 'V1 golden change must remain unauthorized'
    Check (-not [bool]$Contract.frozen_boundaries.exact_v9_golden_change_authorized) 'Exact-V9 golden change must remain unauthorized'
    Check (-not [bool]$Contract.frozen_boundaries.physics_change_authorized) 'physics change must remain unauthorized'
    Check (-not [bool]$Contract.frozen_boundaries.tolerance_change_authorized) 'tolerance change must remain unauthorized'
    Check (-not [bool]$Contract.frozen_boundaries.vr2_r3_change_authorized) 'VR2/R3 change must remain unauthorized'
    Check (-not [bool]$Contract.frozen_boundaries.workflow_change_authorized) 'workflow change must remain unauthorized'
    Check ([int]$Contract.baseline.first_divergent_step -eq 126) 'first divergent step drift'
    Check ([int]$Contract.baseline.divergent_step_count -eq 1) 'divergent step count drift'

    foreach ($Property in $Contract.critical_sha256.PSObject.Properties) {
        $Actual = Sha ([string]$Property.Name)
        Check ($Actual -eq ([string]$Property.Value).ToUpperInvariant()) ("critical SHA drift: " + [string]$Property.Name)
    }

    $TargetRelative = 'tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/M10FinalExactV9ProductionActivationDecisionTests.cs'
    $Target = Join-Path $Root ($TargetRelative -replace '/', '\')
    if (Test-Path -LiteralPath $Target -PathType Leaf) {
        $Text = [IO.File]::ReadAllText($Target, [Text.Encoding]::UTF8)
        $MarkerLine = '// ' + [string]$Contract.diagnostic.marker
        $ExactLines = [regex]::Split($Text, '\r?\n')
        Check ((@($ExactLines | Where-Object { $_.Trim() -eq $MarkerLine })).Count -eq 1) 'Diagnostic 2 marker cardinality drift'
        $Golden = [regex]::Escape([string]$Contract.baseline.exact_v9_frozen_aggregate)
        Check (([regex]::Matches($Text, $Golden)).Count -eq 1) 'frozen Exact-V9 anchor cardinality drift'
        Check (([regex]::Matches($Text, 'stage-causal-selector.tsv')).Count -eq 1) 'selector stage-causal output binding drift'
        Check (([regex]::Matches($Text, 'stage-causal-direct.tsv')).Count -eq 1) 'direct stage-causal output binding drift'
        Check (([regex]::Matches($Text, 'BitConverter.DoubleToInt64Bits')).Count -eq 1) 'IEEE-754 bit capture helper drift'
        Check (([regex]::Matches($Text, 'NRS_FINGERPRINT_V1_DIAGNOSTICS_DIR')).Count -eq 1) 'diagnostic environment binding drift'
        $Rev1MarkerLine = '// NRS-MARKER:M10974-EXACT-V9-CROSS-HOST-STAGE-CAUSAL-SEAM-DIAGNOSTIC2-REV1'
        Check ((@($ExactLines | Where-Object { $_.Trim() -eq $Rev1MarkerLine })).Count -eq 1) 'Diagnostic 2 REV1 marker cardinality drift'
        Check (([regex]::Matches($Text, 'private const int StageCausalStep = 126;')).Count -eq 1) 'stage causal step freeze drift'
        Check (([regex]::Matches($Text, 'stageCausalSnapshot = engine.LatestCanonicalSnapshot;')).Count -eq 1) 'post-step reference capture drift'
        Check (([regex]::Matches($Text, 'ResolveDiagnosticVaporMassFraction')).Count -eq 2) 'vapor-mass-fraction semantic helper drift'
        Check (([regex]::Matches($Text, 'TurbineAdmissionPhasePolicy.LegacyUnrestricted')).Count -eq 1) 'admission policy semantic mirror drift'
        Check (([regex]::Matches($Text, 'stage.TripBlocked \? 0d : phaseLimitedFlow')).Count -eq 1) 'trip effective-flow semantic mirror drift'
        Check (([regex]::Matches($Text, 'foreach \(var entry in selector\.Entries\)')).Count -eq 0) 'all-step stage-causal selector traversal must remain absent'
        Check (([regex]::Matches($Text, 'foreach \(var entry in direct\.Entries\)')).Count -eq 0) 'all-step stage-causal direct traversal must remain absent'
    } else { Add-Problem 'Exact-V9 target test missing' }

    Validate-Manifest ([string]$Contract.manifests.src) 'src' ''
    Validate-Manifest ([string]$Contract.manifests.tests_excluding_target) 'tests' $TargetRelative

    Check ((Sha ([string]$Contract.evidence.local_diagnostic1_artifacts)) -eq ([string]$Contract.evidence.local_diagnostic1_artifacts_sha256).ToUpperInvariant()) 'local Diagnostic 1 evidence SHA drift'
    Check ((Sha ([string]$Contract.evidence.hosted_diagnostic1_outer)) -eq ([string]$Contract.evidence.hosted_diagnostic1_outer_sha256).ToUpperInvariant()) 'hosted Diagnostic 1 evidence SHA drift'
}

if ($Problems.Count -gt 0) {
    Write-Host 'M10974 Exact-V9 Cross-Host Stage Causal-Seam Diagnostic 2 validator: FAIL'
    foreach ($Problem in $Problems) { Write-Host (" - " + $Problem) }
    throw ("validator found {0} problem(s)" -f $Problems.Count)
}
Write-Host 'M10974 Exact-V9 Cross-Host Stage Causal-Seam Diagnostic 2 validator: PASS'
