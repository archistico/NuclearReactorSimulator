$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest

function Require-D1([bool]$Condition,[string]$Message){if(-not $Condition){throw $Message}}
function Read-KvD1([string]$Path){
    Require-D1 (Test-Path -LiteralPath $Path -PathType Leaf) ("Missing artifact: {0}" -f $Path)
    $Map=@{}
    foreach($Line in [IO.File]::ReadAllLines($Path,[Text.Encoding]::UTF8)){
        if([string]::IsNullOrWhiteSpace($Line)){continue}
        $Index=$Line.IndexOf('=')
        Require-D1 ($Index -gt 0) ("Malformed key/value line in {0}: {1}" -f $Path,$Line)
        $Key=$Line.Substring(0,$Index)
        Require-D1 (-not $Map.ContainsKey($Key)) ("Duplicate key in {0}: {1}" -f $Path,$Key)
        $Map[$Key]=$Line.Substring($Index+1)
    }
    $Map
}
function Need-D1([hashtable]$Map,[string]$Key,[string]$Expected){
    Require-D1 ($Map.ContainsKey($Key)) ("Missing key: {0}" -f $Key)
    Require-D1 ([string]$Map[$Key] -eq $Expected) ("Unexpected {0}: expected {1}, actual {2}" -f $Key,$Expected,$Map[$Key])
}

$Root=Split-Path -Parent $PSScriptRoot
Set-Location $Root
$Contract=Get-Content 'eng\m10-final-vr2-r3-mode2-branch-continuity-fusion-failure-diagnostic1-contract.json' -Raw | ConvertFrom-Json
$ArtifactRoot=Join-Path $Root ([string]$Contract.diagnostic.artifact_root).Replace('/','\')

$Matrix=Read-KvD1 (Join-Path $ArtifactRoot '01-failure-state-resolution-matrix.txt')
$Mechanism=Read-KvD1 (Join-Path $ArtifactRoot '02-fusion-mechanism-summary.txt')

Need-D1 $Matrix 'node-id' 'turbine-inlet'
Need-D1 $Matrix 'resolver-success' 'True'
Need-D1 $Matrix 'resolver-path' 'C2MixturePrefix'
Need-D1 $Matrix 'resolver-phase' 'SaturatedMixture'
Need-D1 $Matrix 'direct-mode2-success' 'True'
Need-D1 $Matrix 'direct-mode2-exception' 'none'
Need-D1 $Matrix 'legacy-diagnostic-success' 'True'
Need-D1 $Matrix 'legacy-diagnostic-selected-branch' 'none'
Need-D1 $Matrix 'legacy-diagnostic-multiple-roots' 'False'
Need-D1 $Matrix 'legacy-diagnostic-any-root' 'False'
Need-D1 $Matrix 'fused-same-instance-success' 'False'
Need-D1 $Matrix 'fused-same-instance-exception' 'NuclearReactorSimulator.Simulation.Physics.Fluids.WaterSteamStateOutOfRangeException'
Need-D1 $Matrix 'split-distinct-instance-success' 'True'
Need-D1 $Matrix 'split-distinct-instance-exception' 'none'
Need-D1 $Matrix 'split-equals-direct' 'True'

Need-D1 $Mechanism 'resolver-mode2-success' 'True'
Need-D1 $Mechanism 'direct-mode2-success' 'True'
Need-D1 $Mechanism 'legacy-diagnostic-any-root' 'False'
Need-D1 $Mechanism 'same-instance-fused-success' 'False'
Need-D1 $Mechanism 'distinct-instance-nonfused-success' 'True'
Need-D1 $Mechanism 'nonfused-equals-direct' 'True'
Need-D1 $Mechanism 'production-repair-applied' 'False'
Need-D1 $Mechanism 'r3-remains-blocking' 'True'
Need-D1 $Mechanism 'r4-planning-authorized' 'False'

[IO.File]::WriteAllLines((Join-Path $ArtifactRoot '03-diagnostic-summary.txt'),@(
    'status=PASS-DIAGNOSTIC-MODE2-FUSION-BYPASS-CONFIRMED',
    'returned-r3-classification=R3-SHADOW-COMPOSITION-BLOCKING',
    'failure-state-resolver-path=C2MixturePrefix',
    'direct-mode2-resolve=PASS',
    'same-instance-fused-continuity=BLOCKING',
    'distinct-instance-nonfused-continuity=PASS',
    'nonfused-state-equals-direct=True',
    'root-cause-classification=H28.1-E-FUSED-PATH-NOT-MODE2-AWARE',
    'production-repair-authorized=False',
    'r4-planning-authorized=False'
),[Text.Encoding]::UTF8)

[IO.File]::WriteAllLines((Join-Path $ArtifactRoot '04-pre-repair-review.txt'),@(
    'status=PASS-DIAGNOSTIC-ONLY',
    'repair-planning-next=R3-MODE2-BRANCH-CONTINUITY-FUSION-REPAIR-PLANNING1',
    'candidate-repair=DISABLE-H28.1-E-FUSION-FOR-MODE2-ONLY',
    'mode0-mode1-fused-path-must-remain-UNCHANGED',
    'mode2-production-resolver-must-remain-ReferenceConsistentTabulatedInverseResolver',
    'threshold-change-authorized=False',
    'default-mode2-activation-authorized=False',
    'exact-v9-change-authorized=False',
    'r4-planning-authorized=False'
),[Text.Encoding]::UTF8)

Write-Host 'R3 mode-2 branch-continuity fusion failure diagnostic adjudication: PASS' -ForegroundColor Green
Write-Host 'Diagnostic only. Production repair remains unauthorized pending repair planning.'
