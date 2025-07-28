@echo off
echo Fixing Visual Studio project include paths...

REM Create a user property sheet to ensure include paths work
echo ^<?xml version="1.0" encoding="utf-8"?^> > NXWrapper.props
echo ^<Project ToolsVersion="4.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003"^> >> NXWrapper.props
echo   ^<ImportGroup Label="PropertySheets" /^> >> NXWrapper.props
echo   ^<PropertyGroup Label="UserMacros" /^> >> NXWrapper.props
echo   ^<ItemDefinitionGroup^> >> NXWrapper.props
echo     ^<ClCompile^> >> NXWrapper.props
echo       ^<AdditionalIncludeDirectories^>$(ProjectDir)..\..\HeavenClient\MapleStory-Client\includes\NoLifeNx;%%(AdditionalIncludeDirectories)^</AdditionalIncludeDirectories^> >> NXWrapper.props
echo     ^</ClCompile^> >> NXWrapper.props
echo   ^</ItemDefinitionGroup^> >> NXWrapper.props
echo ^</Project^> >> NXWrapper.props

echo Created NXWrapper.props with correct include paths
echo.
echo Please:
echo 1. Close Visual Studio if it's open
echo 2. Open NXWrapper.vcxproj in Visual Studio again
echo 3. Try building again
echo.
echo If it still fails, try building from Developer Command Prompt with:
echo cl /c /I"%cd%\..\..\HeavenClient\MapleStory-Client\includes\NoLifeNx" /DNXWRAPPER_EXPORTS /std:c++17 /EHsc NXWrapper.cpp
echo.
pause