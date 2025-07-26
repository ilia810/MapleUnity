# reNX Library Setup for Unity

To use the actual MapleStory NX files, you need to add the reNX library to your Unity project:

## Steps to Install reNX:

1. Download reNX.dll from NuGet:
   - Go to https://www.nuget.org/packages/reNX
   - Download the package (version 1.1.8)
   - Extract the .nupkg file (it's a zip file)
   - Find reNX.dll in the lib folder

2. Add to Unity:
   - Create a folder: Assets/Plugins/
   - Copy reNX.dll to Assets/Plugins/
   - Unity will automatically recognize it as a managed plugin

3. Alternative: Build from source
   - Clone https://github.com/angelsl/reNX
   - Build the project to get reNX.dll
   - Copy to Assets/Plugins/

## System.Drawing Dependency

reNX uses System.Drawing for Bitmap handling. In Unity:
- You may need to set Unity's API Compatibility Level to .NET 4.x
- Go to Edit > Project Settings > Player > Configuration
- Set "Api Compatibility Level" to ".NET 4.x"

## Usage

Once installed, the RealNxFile class will automatically use reNX to load actual MapleStory NX files from:
`C:\HeavenClient\MapleStory-Client\nx\`

The implementation handles:
- String, numeric, and point data
- Bitmap conversion to Unity Sprites
- Audio data as byte arrays
- Nested node navigation