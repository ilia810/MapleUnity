# PowerShell script to test character positioning in Unity

$unityPath = "C:\Program Files\Unity\Hub\Editor\2023.2.20f1\Editor\Unity.exe"
$projectPath = "C:\Users\me\MapleUnity"
$logFile = "$projectPath\positioning-test.log"

Write-Host "Starting Unity positioning test..."
Write-Host "Unity: $unityPath"
Write-Host "Project: $projectPath"
Write-Host "Log: $logFile"

# Run Unity with the test
$args = @(
    "-batchmode",
    "-nographics", 
    "-quit",
    "-ignoreCompilerErrors",
    "-noHub",
    "-silent-crashes",
    "-projectPath", "`"$projectPath`"",
    "-executeMethod", "VerifyPositioningTest.RunTest",
    "-logFile", "`"$logFile`""
)

Write-Host "`nExecuting Unity with args:"
Write-Host ($args -join " ")

$process = Start-Process -FilePath $unityPath -ArgumentList $args -PassThru -NoNewWindow -Wait

Write-Host "`nUnity exit code: $($process.ExitCode)"

# Check if results file was created
$resultsFile = "$projectPath\positioning-verification-results.txt"
if (Test-Path $resultsFile) {
    Write-Host "`n=== POSITIONING TEST RESULTS ==="
    Get-Content $resultsFile
} else {
    Write-Host "`nNo results file found. Checking log tail..."
    if (Test-Path $logFile) {
        Write-Host "`n=== LOG TAIL (last 50 lines) ==="
        Get-Content $logFile -Tail 50
    }
}