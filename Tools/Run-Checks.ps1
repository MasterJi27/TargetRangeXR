param(
    [string]$Unity = 'C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe',
    [switch]$TestsOnly
)
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$isRange = Test-Path (Join-Path $projectRoot 'Assets\Scripts\Core\XRDesktopBridge.cs')
$prefix = if ($isRange) { 'TargetRange' } else { 'ChromaStrike' }
$reportDirectory = Join-Path $projectRoot 'Docs\Validation'
$logDirectory = Join-Path $projectRoot 'Logs'
New-Item -ItemType Directory -Force $reportDirectory, $logDirectory | Out-Null
if (-not (Test-Path -LiteralPath $Unity)) { throw "Unity editor not found: $Unity" }

function Invoke-UnityCheck([string]$stage, [string[]]$extra) {
    $logPath = Join-Path $logDirectory ("Release-" + $stage + '.log')
    $arguments = @('-batchmode', '-projectPath', ('"' + $projectRoot + '"'), '-logFile', ('"' + $logPath + '"')) + $extra
    Write-Host "Running $prefix $stage..."
    $process = Start-Process -FilePath $Unity -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait
    if ($process.ExitCode -ne 0) { throw "Unity $stage exited $($process.ExitCode). Read $logPath. No release success is claimed." }
}

foreach ($platform in @('EditMode', 'PlayMode')) {
    $resultPath = Join-Path $reportDirectory ("Unity-" + $platform + '.xml')
    $started = Get-Date
    $arguments = @('-runTests', '-testPlatform', $platform, '-testResults', ('"' + $resultPath + '"'))
    if ($platform -eq 'PlayMode') {
        $filter = if ($isRange) { 'TargetRange.Tests.PlayMode.RangeTests;TargetRange.Tests.PlayMode.DesktopTests' } else { 'ChromaStrike.Tests.PlayMode.GameplayTests' }
        $arguments += @('-testFilter', ('"' + $filter + '"'))
    }
    Invoke-UnityCheck $platform $arguments
    if (-not (Test-Path -LiteralPath $resultPath)) { throw "No $platform test report was produced." }
    if ((Get-Item -LiteralPath $resultPath).LastWriteTime -lt $started) { throw "Stale $platform test report. Do not count it as a pass." }
    [xml]$report = Get-Content -LiteralPath $resultPath -Raw
    $run = $report.'test-run'
    if ($run.result -ne 'Passed' -or [int]$run.failed -ne 0 -or [int]$run.total -eq 0) { throw "Tests did not all pass: $resultPath" }
    Write-Host "$platform passed: $($run.total) tests."
}
if (-not $TestsOnly) {
    Invoke-UnityCheck 'Build' @('-quit', '-executeMethod', "$prefix.EditorTools.BuildCommands.BuildWindows")
    Write-Host 'Windows player built. Manual input, visual, and capture checks in Docs/TESTING.md are still required.'
    if (-not $isRange) { Write-Host 'Task 1: rebuild Installer/ChromaStrike.iss with Inno Setup against this new player, then test the installer.' }
}
