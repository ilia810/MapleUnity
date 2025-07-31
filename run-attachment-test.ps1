$unityPath = "C:\Program Files\Unity\Hub\Editor\2023.2.20f1\Editor\Unity.exe"
$projectPath = "C:\Users\me\MapleUnity"
$logPath = "$projectPath\Temp\attachment_test.log"

Write-Host "Starting Unity batch mode test for attachment points..."

$process = Start-Process -FilePath $unityPath `
    -ArgumentList "-batchmode","-nographics","-noHub","-ignoreCompilerErrors","-silent-crashes", `
    "-projectPath","$projectPath", `
    "-executeMethod","TestAttachmentPointSystem.RunTest", `
    "-quit","-logFile","$logPath" `
    -PassThru -NoNewWindow

# Wait up to 5 minutes
$timeout = 300000
if (!$process.WaitForExit($timeout)) {
    Write-Host "Unity test timed out after 5 minutes, killing process..."
    $process.Kill()
    exit 1
}

Write-Host "Unity process exited with code: $($process.ExitCode)"

# Display log contents
if (Test-Path $logPath) {
    Write-Host "`nLog file contents:"
    Get-Content $logPath
} else {
    Write-Host "Log file not found at: $logPath"
}