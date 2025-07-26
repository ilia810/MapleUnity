@echo off
setlocal enabledelayedexpansion

if "%~1"=="" (
    echo Usage: extract_messages.bat ^<logfile^>
    echo.
    echo Extracts only the log messages from Unity console output,
    echo removing stack traces and metadata.
    exit /b 1
)

if not exist "%~1" (
    echo Error: File "%~1" not found
    exit /b 1
)

set "in_message=1"
for /f "usebackq delims=" %%A in ("%~1") do (
    set "line=%%A"
    
    rem Check if line starts with "UnityEngine.Debug:" - this marks the start of stack trace
    echo !line! | findstr /b "UnityEngine.Debug:" >nul
    if !errorlevel! equ 0 (
        set "in_message=0"
    ) else (
        rem Check if line is empty - this marks the end of a log entry
        if "!line!"=="" (
            set "in_message=1"
            echo.
        ) else (
            rem If we're in the message part, print the line
            if !in_message! equ 1 (
                echo !line!
            )
        )
    )
)

endlocal