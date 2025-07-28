#pragma once

#ifdef NXWRAPPER_EXPORTS
#define NXWRAPPER_API __declspec(dllexport)
#else
#define NXWRAPPER_API __declspec(dllimport)
#endif

extern "C" {
    // Initialize the NX system with path to NX files
    NXWRAPPER_API bool NX_Initialize(const char* nxPath);
    
    // Get a node by path (e.g., "Map.nx/Obj/guide.img/common/post/0")
    NXWRAPPER_API void* NX_GetNode(const char* path);
    
    // Get node properties
    NXWRAPPER_API const char* NX_GetNodeName(void* node);
    NXWRAPPER_API int NX_GetNodeType(void* node); // 0=none, 1=integer, 2=real, 3=string, 4=vector, 5=bitmap, 6=audio
    NXWRAPPER_API int NX_GetChildCount(void* node);
    
    // Get node value based on type
    NXWRAPPER_API long long NX_GetIntValue(void* node);
    NXWRAPPER_API double NX_GetRealValue(void* node);
    NXWRAPPER_API const char* NX_GetStringValue(void* node);
    NXWRAPPER_API bool NX_GetVectorValue(void* node, int* x, int* y);
    NXWRAPPER_API bool NX_GetBitmapData(void* node, unsigned char** data, int* size);
    
    // Get child by name
    NXWRAPPER_API void* NX_GetChild(void* node, const char* name);
    
    // Get child by index
    NXWRAPPER_API void* NX_GetChildByIndex(void* node, int index);
    
    // Check if node has specific child
    NXWRAPPER_API bool NX_HasChild(void* node, const char* name);
    
    // Special function to get origin if it exists
    NXWRAPPER_API bool NX_GetOrigin(void* node, int* x, int* y);
    
    // Cleanup
    NXWRAPPER_API void NX_Cleanup();
}