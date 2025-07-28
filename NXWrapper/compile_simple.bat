@echo off
echo ===================================
echo NXWrapper DLL Compilation Script
echo ===================================
echo.
echo This script will compile NXWrapper.dll for use with Unity.
echo.
echo PREREQUISITES:
echo 1. Visual Studio 2019 or later installed
echo 2. Desktop development with C++ workload installed
echo.
echo INSTRUCTIONS:
echo 1. Open "Developer Command Prompt for VS 2022" (or your VS version)
echo 2. Navigate to this directory: cd "%~dp0"
echo 3. Run this command:
echo.
echo cl /LD /DNXWRAPPER_EXPORTS /std:c++17 /EHsc /I"..\..\HeavenClient\MapleStory-Client\includes\NoLifeNx" NXWrapper.cpp /link /LIBPATH:"..\..\HeavenClient\MapleStory-Client\includes\NoLifeNx\nlnx\x64\Release" /LIBPATH:"..\..\HeavenClient\MapleStory-Client\includes\NoLifeNx\nlnx\includes\lz4_v1_8_2_win64\static" NoLifeNx.lib liblz4_static.lib /OUT:NXWrapper.dll
echo.
echo 4. Copy NXWrapper.dll to: ..\Assets\Plugins\
echo.
pause