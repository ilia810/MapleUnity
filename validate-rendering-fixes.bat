@echo off
echo === Running Character Rendering Validation Test ===
echo.

set UNITY_PATH="C:\Program Files\Unity\Hub\Editor\2023.2.20f1\Editor\Unity.exe"
set PROJECT_PATH="C:\Users\me\MapleUnity"
set LOG_FILE="C:\Users\me\MapleUnity\validation-test.log"

echo Starting Unity in batch mode...
echo Log will be saved to: %LOG_FILE%
echo.

%UNITY_PATH% ^
    -batchmode ^
    -nographics ^
    -noHub ^
    -ignoreCompilerErrors ^
    -silent-crashes ^
    -projectPath %PROJECT_PATH% ^
    -executeMethod ValidateCharacterRenderingFixes.RunValidation ^
    -quit ^
    -logFile %LOG_FILE% 2>&1

echo.
echo Unity execution completed with exit code: %ERRORLEVEL%
echo.

if exist %LOG_FILE% (
    echo === Displaying last 100 lines of log ===
    powershell -Command "Get-Content '%LOG_FILE%' -Tail 100"
) else (
    echo ERROR: Log file not found!
)

pause