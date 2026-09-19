param(
  [Parameter(Mandatory=$true)][string]$LogPath,
  [Parameter(Mandatory=$true)][string]$OutputPath
)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
if(-not (Test-Path -LiteralPath $LogPath -PathType Leaf)){ throw 'ordinary CI hosted log missing' }
$lines=@(Get-Content -LiteralPath $LogPath)
$start=-1
for($i=0;$i -lt $lines.Count;$i++){
  $line=[string]$lines[$i]
  if($line -match '\[FAIL\]' -or $line -match '^\s*failed\s+'){ $start=$i; break }
}
if($start -lt 0){
  for($i=0;$i -lt $lines.Count;$i++){
    if(([string]$lines[$i]) -match 'failed with 1 error'){ $start=[Math]::Max(0,$i-12); break }
  }
}
if($start -lt 0){ $start=[Math]::Max(0,$lines.Count-80) }
$end=[Math]::Min($lines.Count-1,$start+40)
$out=@()
$out+='source=single hosted ordinary-ci execution; no rerun; no test selection; no failure suppression'
$out+=('captured-lines='+$start+'..'+$end)
for($i=$start;$i -le $end;$i++){ $out+=([string]$lines[$i]) }
$parent=Split-Path -Parent $OutputPath
if($parent -and -not (Test-Path -LiteralPath $parent)){ New-Item -ItemType Directory -Force -Path $parent | Out-Null }
$out | Set-Content -LiteralPath $OutputPath -Encoding UTF8
Write-Host ('Hosted ordinary first-failure extract written: '+$OutputPath) -ForegroundColor Yellow
