@echo off
echo === Testing Unity Compilation Status ===
echo.

"C:\Program Files\Unity\Hub\Editor\2023.2.20f1\Editor\Unity.exe" ^
    -batchmode ^
    -nographics ^
    -noHub ^
    -ignoreCompilerErrors ^
    -silent-crashes ^
    -projectPath "C:\Users\me\MapleUnity" ^
    -quit ^
    -logFile "C:\Users\me\MapleUnity\compilation-test.log" 2>&1

echo.
echo Unity exit code: %ERRORLEVEL%
echo.

if exist "C:\Users\me\MapleUnity\compilation-test.log" (
    echo === Last 50 lines of log ===
    powershell -Command "Get-Content 'C:\Users\me\MapleUnity\compilation-test.log' -Tail 50"
) else (
    echo No log file generated
)