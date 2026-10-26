$ErrorActionPreference = 'Stop'
$gameRoot = Split-Path $PSScriptRoot -Parent
$toolFolder = Join-Path $gameRoot 'Builds/Windows-Werkzeuge'
$runtimeExe = Join-Path $toolFolder 'Runtime/TreasureRunnerTools.exe'
New-Item -ItemType Directory -Force "$toolFolder/Pruefung" | Out-Null
foreach ($toolMode in @('editor','admin')) {
    $startedAt = Get-Date
    $runArgs = '-batchmode --level-' + $toolMode + ' --tools-smoke -logFile "' + $toolFolder + '/Pruefung/' + $toolMode + '-Unity.log"'
    $runtimeCheck = Start-Process -FilePath $runtimeExe -ArgumentList $runArgs -WorkingDirectory $toolFolder -WindowStyle Hidden -PassThru
    if (-not $runtimeCheck.WaitForExit(120000)) {
        Stop-Process -Id $runtimeCheck.Id
        throw "Zeitlimit beim eigenen Werkzeugtest $toolMode."
    }
    $reportPath = Join-Path $toolFolder "Pruefung/$toolMode.txt"
    $report = Get-Item -LiteralPath $reportPath
    Get-Content -LiteralPath $reportPath
    if ($runtimeCheck.ExitCode -ne 0 -or $report.LastWriteTime -lt $startedAt -or -not (Select-String -LiteralPath $reportPath -SimpleMatch 'SMOKE PASSED' -Quiet)) {
        throw "Werkzeugtest fehlgeschlagen: $toolMode"
    }
}
