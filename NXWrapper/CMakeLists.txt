cmake_minimum_required(VERSION 3.10)
project(NXWrapper)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Set output to build as DLL
add_library(NXWrapper SHARED 
    NXWrapper.cpp
    NXWrapper.h
)

# Add preprocessor definition for exports
target_compile_definitions(NXWrapper PRIVATE NXWRAPPER_EXPORTS)

# Find the nx library
set(NX_INCLUDE_DIR "${CMAKE_CURRENT_SOURCE_DIR}/../../HeavenClient/MapleStory-Client/includes/NoLifeNx" CACHE PATH "Path to nlnx headers")
set(NX_LIB_PATH "${CMAKE_CURRENT_SOURCE_DIR}/../../HeavenClient/MapleStory-Client/includes/NoLifeNx/nlnx/x64/Release/NoLifeNx.lib" CACHE PATH "Path to NoLifeNx library")
set(LZ4_LIB_PATH "${CMAKE_CURRENT_SOURCE_DIR}/../../HeavenClient/MapleStory-Client/includes/NoLifeNx/nlnx/includes/lz4_v1_8_2_win64/static/liblz4_static.lib" CACHE PATH "Path to lz4 library")

# Include directories
target_include_directories(NXWrapper PRIVATE ${NX_INCLUDE_DIR})

# Link libraries directly
target_link_libraries(NXWrapper PRIVATE ${NX_LIB_PATH} ${LZ4_LIB_PATH})

# Set output directory
set_target_properties(NXWrapper PROPERTIES
    RUNTIME_OUTPUT_DIRECTORY_DEBUG "${CMAKE_CURRENT_SOURCE_DIR}/../Assets/Plugins"
    RUNTIME_OUTPUT_DIRECTORY_RELEASE "${CMAKE_CURRENT_SOURCE_DIR}/../Assets/Plugins"
    LIBRARY_OUTPUT_DIRECTORY_DEBUG "${CMAKE_CURRENT_SOURCE_DIR}/../Assets/Plugins"
    LIBRARY_OUTPUT_DIRECTORY_RELEASE "${CMAKE_CURRENT_SOURCE_DIR}/../Assets/Plugins"
)

# Copy dependent DLLs to output (if any exist)
if(WIN32)
    # Copy lz4 dll if it exists
    set(LZ4_DLL_PATH "${CMAKE_CURRENT_SOURCE_DIR}/../../HeavenClient/MapleStory-Client/includes/NoLifeNx/nlnx/includes/lz4_v1_8_2_win64/dll/liblz4.so.1.8.2.dll")
    if(EXISTS ${LZ4_DLL_PATH})
        add_custom_command(TARGET NXWrapper POST_BUILD
            COMMAND ${CMAKE_COMMAND} -E copy_if_different
            ${LZ4_DLL_PATH}
            $<TARGET_FILE_DIR:NXWrapper>
        )
    endif()
endif()