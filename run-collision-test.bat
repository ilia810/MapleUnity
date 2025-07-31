@echo off
echo Running Unity collision test...
echo Please wait for Unity to start, then:
echo 1. Open the henesys.unity scene
echo 2. Press Play
echo 3. The RuntimeCollisionTest will run automatically
echo 4. Check debug-log.txt for results
echo.

REM Clear debug log
echo [TEST] Starting collision test... > debug-log.txt

REM Start Unity with proper flags
"C:\Program Files\Unity\Hub\Editor\2023.2.20f1\Editor\Unity.exe" -projectPath "%cd%" -ignoreCompilerErrors

echo.
echo Unity has been launched. Follow the steps above.
pause