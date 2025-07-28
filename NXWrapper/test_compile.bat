@echo off
echo Testing include paths...

REM First, let's verify the files exist
if exist "..\..\HeavenClient\MapleStory-Client\includes\NoLifeNx\nlnx\nx.hpp" (
    echo ✓ Found nx.hpp at expected location
) else (
    echo ✗ Cannot find nx.hpp at expected location
    echo Expected: ..\..\HeavenClient\MapleStory-Client\includes\NoLifeNx\nlnx\nx.hpp
)

echo.
echo To compile manually from Developer Command Prompt:
echo.
echo cl /c /I"..\..\HeavenClient\MapleStory-Client\includes\NoLifeNx" /DNXWRAPPER_EXPORTS /std:c++17 /EHsc NXWrapper.cpp
echo.
pause