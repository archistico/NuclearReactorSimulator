[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'Medium')]
param()

$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$projectExtensions = @('.csproj', '.vbproj', '.fsproj')

$projectFiles = Get-ChildItem -LiteralPath $repositoryRoot -Recurse -File |
    Where-Object {
        $_.Extension -in $projectExtensions -and
        $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
    }

if ($projectFiles.Count -eq 0) {
    Write-Host 'Nessun progetto trovato.'
    return
}

$directoriesToRemove = foreach ($projectFile in $projectFiles) {
    $projectDirectory = $projectFile.Directory.FullName

    foreach ($directoryName in @('bin', 'obj')) {
        $directoryPath = Join-Path $projectDirectory $directoryName

        if (Test-Path -LiteralPath $directoryPath -PathType Container) {
            Get-Item -LiteralPath $directoryPath
        }
    }
}

$directoriesToRemove = $directoriesToRemove |
    Sort-Object -Property FullName -Unique

if ($directoriesToRemove.Count -eq 0) {
    Write-Host 'Nessuna cartella bin o obj da eliminare.'
    return
}

foreach ($directory in $directoriesToRemove) {
    $relativePath = [System.IO.Path]::GetRelativePath(
        $repositoryRoot,
        $directory.FullName
    )

    if ($PSCmdlet.ShouldProcess($relativePath, 'Elimina cartella e contenuto')) {
        Remove-Item -LiteralPath $directory.FullName -Recurse -Force
        Write-Host "Eliminata: $relativePath"
    }
}

if ($WhatIfPreference) {
    Write-Host 'Simulazione completata: nessuna cartella è stata eliminata.'
}
else {
    Write-Host 'Pulizia completata.'
}
