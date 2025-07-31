@echo off
echo ===============================================
echo Running Equipment Loading Test (Batch Mode)
echo ===============================================
echo.

set UNITY_PATH="C:\Program Files\Unity\Hub\Editor\2023.2.20f1\Editor\Unity.exe"
set PROJECT_PATH="C:\Users\me\MapleUnity"
set LOG_FILE="%PROJECT_PATH%\Temp\equipment_batch_test.log"

echo Attempting to run test in batch mode...
echo If Unity Editor is open, this may fail. Please close Unity Editor if needed.
echo.

%UNITY_PATH% -batchmode -nographics -noHub -ignoreCompilerErrors -silent-crashes -projectPath %PROJECT_PATH% -executeMethod QuickEquipmentTest.RunBatchTest -quit -logFile %LOG_FILE% 2>&1

set EXIT_CODE=%ERRORLEVEL%
echo.
echo Unity exited with code: %EXIT_CODE%

:: Check for results file
if exist "%PROJECT_PATH%\equipment_test_results.txt" (
    echo.
    echo === Test Results ===
    type "%PROJECT_PATH%\equipment_test_results.txt"
) else (
    echo.
    echo No results file found. Checking log...
    if exist %LOG_FILE% (
        echo === Log Tail ===
        powershell -Command "Get-Content %LOG_FILE% -Tail 50"
    )
)

pause