@echo off
echo === Running Character Rendering Debug ===

REM Delete old log
del character-rendering-runtime.log 2>nul

REM Run Unity for 10 seconds in play mode
echo Starting Unity in play mode...
start "" "C:\Program Files\Unity\Hub\Editor\2023.2.20f1\Editor\Unity.exe" ^
    -projectPath "C:\Users\me\MapleUnity"

echo Unity is starting. The debugger will automatically log character rendering info.
echo Check character-rendering-runtime.log for results.
echo.
echo Press any key to continue...
pause > nul