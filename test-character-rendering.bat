@echo off
echo Running Character Rendering Test...
"C:\Program Files\Unity\Hub\Editor\2023.2.20f1\Editor\Unity.exe" ^
    -batchmode ^
    -nographics ^
    -noHub ^
    -ignoreCompilerErrors ^
    -silent-crashes ^
    -quit ^
    -projectPath "C:\Users\me\MapleUnity" ^
    -executeMethod MinimalBatchTest.RunMinimalTest ^
    -logFile "C:\Users\me\MapleUnity\character-test-log.txt"

echo Test complete. Check character-test-log.txt for results.
pause