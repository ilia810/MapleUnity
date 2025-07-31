@echo off
echo Running simple attachment point test...
"C:\Program Files\Unity\Hub\Editor\2023.2.20f1\Editor\Unity.exe" ^
    -batchmode ^
    -nographics ^
    -noHub ^
    -ignoreCompilerErrors ^
    -silent-crashes ^
    -projectPath "C:\Users\me\MapleUnity" ^
    -executeMethod SimpleAttachmentTest.RunTest ^
    -quit ^
    -logFile "C:\Users\me\MapleUnity\Temp\simple_attachment_test.log"

echo Unity exit code: %ERRORLEVEL%

if exist "C:\Users\me\MapleUnity\Temp\simple_attachment_test.log" (
    echo.
    echo Log file contents:
    type "C:\Users\me\MapleUnity\Temp\simple_attachment_test.log"
) else (
    echo Log file not found
)