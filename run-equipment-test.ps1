# Kill any existing Unity processes
Write-Host "Killing existing Unity processes..."
Get-Process Unity -ErrorAction SilentlyContinue | Stop-Process -Force

# Wait a moment
Start-Sleep -Seconds 2

# Run Unity with the test
Write-Host "Starting Unity to run equipment test..."
$unityPath = "C:\Program Files\Unity\Hub\Editor\2023.2.20f1\Editor\Unity.exe"
$projectPath = "C:\Users\me\MapleUnity"
$logPath = "$projectPath\equipment-test-run.log"

# Start Unity and wait for it to compile and run the test
$process = Start-Process -FilePath $unityPath -ArgumentList `
    "-batchmode", `
    "-nographics", `
    "-noHub", `
    "-silent-crashes", `
    "-projectPath", $projectPath, `
    "-executeMethod", "TestCharacterWithEquipment.RunTest", `
    "-quit", `
    "-logFile", $logPath `
    -PassThru -NoNewWindow

# Wait up to 5 minutes for completion
Write-Host "Waiting for test to complete (max 5 minutes)..."
if (!$process.WaitForExit(300000)) {
    Write-Host "Test timed out, killing Unity..."
    $process.Kill()
}

Write-Host "Test completed with exit code: $($process.ExitCode)"

# Check if the character equipment test log was created
$equipmentLogPath = "$projectPath\character-equipment-test.log"
if (Test-Path $equipmentLogPath) {
    Write-Host "`nEquipment test results:"
    Get-Content $equipmentLogPath
} else {
    Write-Host "`nNo equipment test log found. Checking Unity log..."
    if (Test-Path $logPath) {
        Get-Content $logPath | Select-String -Pattern "ERROR|error|Equipment|equipment" -Context 2,2
    }
}