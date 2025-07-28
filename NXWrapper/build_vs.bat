@echo off
echo Building NXWrapper DLL with Visual Studio...

REM Set paths
set VS_PATH="C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
set OUTPUT_DIR=..\Assets\Plugins

REM Check if VS exists
if not exist %VS_PATH% (
    echo Visual Studio 2022 not found at expected location
    echo Please update VS_PATH in this script
    pause
    exit /b 1
)

REM Create output directory
if not exist %OUTPUT_DIR% mkdir %OUTPUT_DIR%

REM Build using MSBuild
echo Building Release x64...
%VS_PATH% NXWrapper.vcxproj /p:Configuration=Release /p:Platform=x64

REM Check if build succeeded
if exist x64\Release\NXWrapper.dll (
    echo Build successful!
    copy x64\Release\NXWrapper.dll %OUTPUT_DIR%\
    echo Copied NXWrapper.dll to Unity Plugins folder
    
    REM Copy lz4 dll if needed
    set LZ4_DLL=..\..\HeavenClient\MapleStory-Client\includes\NoLifeNx\nlnx\includes\lz4_v1_8_2_win64\dll\liblz4.so.1.8.2.dll
    if exist %LZ4_DLL% (
        copy %LZ4_DLL% %OUTPUT_DIR%\
        echo Copied lz4 dll to Unity Plugins folder
    )
) else (
    echo Build failed!
)

pause