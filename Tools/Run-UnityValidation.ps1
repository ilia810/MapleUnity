[CmdletBinding()]
param(
    [ValidateSet('EditMode', 'Smoke', 'PlayerCompile')]
    [string]$Mode = 'EditMode',
    [string]$TestFilter = '',
    [string]$ProjectPath = (Join-Path $PSScriptRoot '..'),
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\2023.2.20f1\Editor\Unity.exe',
    [ValidateRange(1, 120)] [int]$TimeoutMinutes = 15
)
$ErrorActionPreference = 'Stop'
$unityProcess = $null

function Quote-Argument([string]$Value) {
    $escaped = [regex]::Replace($Value, '(\\*)"', '$1$1\"')
    return '"' + [regex]::Replace($escaped, '(\\+)$', '$1$1') + '"'
}

try {
    $projectRoot = (Resolve-Path -LiteralPath $ProjectPath).Path
    $unityExecutable = (Resolve-Path -LiteralPath $UnityPath).Path
    foreach ($folder in @('Assets', 'Packages', 'ProjectSettings')) {
        if (-not (Test-Path -LiteralPath (Join-Path $projectRoot $folder) -PathType Container)) {
            throw "Missing Unity project folder: $folder"
        }
    }
    if ($Mode -ne 'EditMode' -and $TestFilter) { throw '-TestFilter is supported only in EditMode.' }
    $runId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0, 8)
    $snapshot = Join-Path $projectRoot ('Temp/UnityValidation-' + $runId)
    $logs = Join-Path $projectRoot 'Logs'
    New-Item -ItemType Directory -Path $snapshot | Out-Null
    New-Item -ItemType Directory -Path $logs -Force | Out-Null
    foreach ($folder in @('Assets', 'Packages', 'ProjectSettings')) {
        Copy-Item -LiteralPath (Join-Path $projectRoot $folder) -Destination $snapshot -Recurse
    }
    $prefix = Join-Path $logs ('unity-' + $Mode.ToLowerInvariant() + '-' + $runId)
    $logPath = $prefix + '.log'
    $resultsPath = $prefix + '.xml'
    $screenshotPath = $prefix + '.png'
    $unityArguments = @('-batchmode', '-projectPath', $snapshot, '-logFile', $logPath)
    switch ($Mode) {
        'EditMode' {
            $unityArguments += @('-nographics', '-runTests', '-testPlatform', 'EditMode', '-testResults', $resultsPath)
            if ($TestFilter) { $unityArguments += @('-testFilter', $TestFilter) }
        }
        'Smoke' {
            $unityArguments += @('-runTests', '-testPlatform', 'PlayMode', '-testFilter',
                'MapleClient.Tests.PlayMode.RecoverySceneSmokeTests;MapleClient.Tests.PlayMode.MapTransitionSmokeTests;MapleClient.Tests.PlayMode.GeneratedSceneSmokeTests;MapleClient.Tests.PlayMode.ActorOcclusionSmokeTests;MapleClient.Tests.PlayMode.MonsterSceneSmokeTests;MapleClient.Tests.PlayMode.PlayerCombatSceneSmokeTests;MapleClient.Tests.PlayMode.InventorySceneSmokeTests;MapleClient.Tests.PlayMode.WeaponSceneSmokeTests;MapleClient.Tests.PlayMode.AfterimageSceneSmokeTests;MapleClient.Tests.PlayMode.AttackMovementSceneSmokeTests;MapleClient.Tests.PlayMode.CombatStatSceneSmokeTests;MapleClient.Tests.PlayMode.SkillSceneSmokeTests;MapleClient.Tests.PlayMode.MeleeSkillSceneSmokeTests;MapleClient.Tests.PlayMode.MagicSkillSceneSmokeTests;MapleClient.Tests.PlayMode.RangedCombatSceneSmokeTests;MapleClient.Tests.PlayMode.RangedSkillSceneSmokeTests;MapleClient.Tests.PlayMode.BackgroundSceneSmokeTests;MapleClient.Tests.PlayMode.SwimmingSceneSmokeTests;MapleClient.Tests.PlayMode.StanceAnimationSceneSmokeTests;MapleClient.Tests.PlayMode.FaceAnimationSceneSmokeTests;MapleClient.Tests.PlayMode.LocalLoopSceneSmokeTests;MapleClient.Tests.PlayMode.ClassicUISceneSmokeTests;MapleClient.Tests.PlayMode.ClassicNavigationSceneSmokeTests;MapleClient.Tests.PlayMode.QuestSceneSmokeTests;MapleClient.Tests.PlayMode.ClassicCombatVisualSceneSmokeTests;MapleClient.Tests.PlayMode.ClassicNpcPresentationSceneSmokeTests;MapleClient.Tests.PlayMode.ClassicVisualFinishSceneSmokeTests;MapleClient.Tests.PlayMode.ClassicShopTooltipSceneSmokeTests;MapleClient.Tests.PlayMode.ClassicQuestHelperSceneSmokeTests;MapleClient.Tests.PlayMode.ClassicQuickslotSceneSmokeTests;MapleClient.Tests.PlayMode.ClassicKeyboardScrollbarSceneSmokeTests;MapleClient.Tests.PlayMode.ClassicFullKeyboardSceneSmokeTests;MapleClient.Tests.PlayMode.ClassicMessageHistorySceneSmokeTests;MapleClient.Tests.PlayMode.ClassicReferenceVisualSceneSmokeTests;MapleClient.Tests.PlayMode.SupportSkillSceneSmokeTests;MapleClient.Tests.PlayMode.CustomSkillSceneSmokeTests;MapleClient.Tests.PlayMode.UtilitySkillSceneSmokeTests;MapleClient.Tests.PlayMode.ThiefSkillSceneSmokeTests;MapleClient.Tests.PlayMode.RangedUtilitySceneSmokeTests;MapleClient.Tests.PlayMode.InventorySlotSceneSmokeTests;MapleClient.Tests.PlayMode.CharacterProgressionSceneSmokeTests', '-testResults', $resultsPath,
                '-recoveryScreenshot', $screenshotPath)
        }
        'PlayerCompile' {
            $unityArguments += @('-nographics', '-executeMethod', 'PlayerCompilationValidation.Run')
        }
    }
    Write-Host "Snapshot: $snapshot"
    Write-Host "Unity log: $logPath"
    $argumentLine = ($unityArguments | ForEach-Object { Quote-Argument $_ }) -join ' '
    $unityProcess = Start-Process -FilePath $unityExecutable -ArgumentList $argumentLine -WorkingDirectory $snapshot -WindowStyle Hidden -PassThru
    $deadline = [DateTime]::UtcNow.AddMinutes($TimeoutMinutes)
    while (-not $unityProcess.WaitForExit(1000)) {
        if ([DateTime]::UtcNow -ge $deadline) { throw "Unity timed out after $TimeoutMinutes minutes. See $logPath" }
    }
    $exitCode = $unityProcess.ExitCode
    if ($Mode -ne 'PlayerCompile' -and (Test-Path -LiteralPath $resultsPath)) {
        [xml]$results = Get-Content -LiteralPath $resultsPath -Raw
        $run = $results.'test-run'
        Write-Host "Tests: $($run.total) total, $($run.passed) passed, $($run.failed) failed, $($run.skipped) skipped"
        Write-Host "Results: $resultsPath"
    }
    if ($exitCode -ne 0) { throw "Unity exited with code $exitCode. See $logPath" }
    if ($Mode -eq 'PlayerCompile') {
        if (-not (Select-String -LiteralPath $logPath -SimpleMatch 'PLAYER_COMPILATION_VALIDATION_PASSED:' -Quiet)) {
            throw 'Unity did not confirm successful player compilation.'
        }
    } else {
        if (-not (Test-Path -LiteralPath $resultsPath) -or [int]$run.passed -lt 1 -or $run.result -ne 'Passed') {
            throw 'The test run did not report a passing result with at least one executed test.'
        }
        if ($Mode -eq 'Smoke') {
            if (-not (Test-Path -LiteralPath $screenshotPath) -or (Get-Item -LiteralPath $screenshotPath).Length -eq 0) {
                throw 'Scene smoke did not produce a rendered screenshot.'
            }
            Write-Host "Screenshot: $screenshotPath"
        }
    }
    Write-Host "$Mode validation passed. Snapshot and artifacts retained."
    exit 0
} catch {
    Write-Error $_ -ErrorAction Continue
    exit 1
} finally {
    if ($null -ne $unityProcess -and -not $unityProcess.HasExited) {
        Stop-Process -Id $unityProcess.Id -Force -ErrorAction SilentlyContinue
    }
}
