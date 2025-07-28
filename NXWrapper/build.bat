@echo off
echo Building NXWrapper DLL...

REM Create build directory
if not exist build mkdir build
cd build

REM Generate Visual Studio project
cmake .. -G "Visual Studio 17 2022" -A x64

REM Build the project
cmake --build . --config Release

REM Check if build succeeded
if exist Release\NXWrapper.dll (
    echo Build successful!
    echo DLL location: %cd%\Release\NXWrapper.dll
    
    REM Copy to Unity Plugins folder
    if not exist "..\..\Assets\Plugins" mkdir "..\..\Assets\Plugins"
    copy Release\NXWrapper.dll "..\..\Assets\Plugins\"
    echo Copied to Unity Plugins folder
) else (
    echo Build failed!
)

cd ..
pause