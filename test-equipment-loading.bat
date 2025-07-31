@echo off
echo ===============================================
echo Running Equipment Loading Test
echo ===============================================
echo.
echo NOTE: Close Unity Editor if it's open with this project!
echo Attempting to run test anyway with -ignoreCompilerErrors...
echo.

set UNITY_PATH="C:\Program Files\Unity\Hub\Editor\2023.2.20f1\Editor\Unity.exe"
set PROJECT_PATH="C:\Users\me\MapleUnity"
set LOG_FILE="%PROJECT_PATH%\Temp\equipment_loading_test.log"

echo Starting Unity in batch mode...
%UNITY_PATH% -batchmode -nographics -noHub -ignoreCompilerErrors -silent-crashes -projectPath %PROJECT_PATH% -executeMethod RunEquipmentLoadingTest.RunTest -quit -logFile %LOG_FILE% 2>&1

echo.
echo Test execution attempted. Check the log file at:
echo %LOG_FILE%
echo.

if exist %LOG_FILE% (
    echo === Last 50 lines of log ===
    powershell -Command "Get-Content %LOG_FILE% -Tail 50"
)

pause