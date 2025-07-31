@echo off
echo Testing Henesys scene with collision fixes...
echo.

REM Clear the debug log
echo Testing collision fixes... > debug-log.txt

REM Run Unity in batch mode with our test
"C:\Program Files\Unity\Hub\Editor\2023.2.20f1\Editor\Unity.exe" -batchmode -projectPath "%cd%" -executeMethod TestHenesysScene.RunHenesysTest -quit -logFile unity-batch-test.log

echo.
echo Test complete! Check debug-log.txt for results.
pause