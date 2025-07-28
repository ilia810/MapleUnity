# NXWrapper - C++ NoLifeNx Library Wrapper for Unity

This wrapper provides P/Invoke access to the C++ NoLifeNx library from Unity, allowing us to read NX file data (including origin points) that the C# reNX library cannot access.

## Compilation Instructions

### Option 1: Using Developer Command Prompt (Recommended)

1. Open **"Developer Command Prompt for VS 2022"** (or your Visual Studio version)
   - You can find this in the Start Menu under Visual Studio folder

2. Navigate to this directory:
   ```
   cd C:\Users\me\MapleUnity\NXWrapper
   ```

3. Run the compilation command:
   ```
   cl /LD /DNXWRAPPER_EXPORTS /std:c++17 /EHsc /I"..\..\HeavenClient\MapleStory-Client\includes\NoLifeNx" NXWrapper.cpp /link /LIBPATH:"..\..\HeavenClient\MapleStory-Client\includes\NoLifeNx\nlnx\x64\Release" /LIBPATH:"..\..\HeavenClient\MapleStory-Client\includes\NoLifeNx\nlnx\includes\lz4_v1_8_2_win64\static" NoLifeNx.lib liblz4_static.lib /OUT:NXWrapper.dll
   ```

4. Copy the generated `NXWrapper.dll` to Unity:
   ```
   copy NXWrapper.dll ..\Assets\Plugins\
   ```

### Option 2: Using Visual Studio Project

1. Open `NXWrapper.vcxproj` in Visual Studio 2022

2. Set configuration to **Release** and platform to **x64**

3. Build the project (Ctrl+Shift+B)

4. The DLL will be in `x64\Release\NXWrapper.dll`

5. Copy it to `..\Assets\Plugins\`

### Option 3: Using CMake

1. Open command prompt in this directory

2. Run:
   ```
   mkdir build
   cd build
   cmake .. -G "Visual Studio 17 2022" -A x64
   cmake --build . --config Release
   ```

3. The DLL will be copied automatically to Unity Plugins folder

## Testing

After compilation:

1. Return to Unity
2. Open the menu: **MapleUnity > Debug > Test C++ NX Wrapper**
3. Click "Test C++ NX Library" button
4. Check the console for output

The test should show that objects (like guide signs) have origin data that was previously missing.

## Troubleshooting

If you get linking errors:
- Make sure the NoLifeNx.lib path is correct
- Verify Visual Studio has C++ development tools installed
- Try using the x86 version if x64 doesn't work

If Unity can't load the DLL:
- Make sure it's in Assets/Plugins folder
- Check that it's compiled for the correct architecture (x64)
- Verify all dependencies are available