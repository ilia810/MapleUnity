#include "NXWrapper.h"
#include <nlnx/nx.hpp>
#include <nlnx/node.hpp>
#include <nlnx/bitmap.hpp>
#include <nlnx/file.hpp>
#include <map>
#include <memory>
#include <string>
#include <cstring>

// Store loaded NX files as pointers since nl::file cannot be copied
static std::map<std::string, std::unique_ptr<nl::file>> nxFiles;
static std::string nxBasePath;

// Helper to split path like "Map.nx/Obj/guide.img/common/post/0"
std::pair<std::string, std::string> splitNxPath(const std::string& path) {
    size_t pos = path.find(".nx/");
    if (pos != std::string::npos) {
        return { path.substr(0, pos + 3), path.substr(pos + 4) };
    }
    pos = path.find('/');
    if (pos != std::string::npos) {
        return { path.substr(0, pos) + ".nx", path.substr(pos + 1) };
    }
    return { "", path };
}

extern "C" {
    
NXWRAPPER_API bool NX_Initialize(const char* nxPath) {
    try {
        nxBasePath = nxPath;
        if (nxBasePath.back() != '/' && nxBasePath.back() != '\\') {
            nxBasePath += '/';
        }
        
        // Pre-load common NX files
        std::string files[] = { "Map.nx", "Character.nx", "Item.nx", "String.nx", "UI.nx" };
        for (const auto& file : files) {
            std::string fullPath = nxBasePath + file;
            try {
                nxFiles[file] = std::make_unique<nl::file>(fullPath);
            } catch (...) {
                // File might not exist, that's okay
            }
        }
        
        return true;
    } catch (...) {
        return false;
    }
}

NXWRAPPER_API void* NX_GetNode(const char* path) {
    try {
        auto [nxFileName, nodePath] = splitNxPath(path);
        
        // Load NX file if not already loaded
        if (nxFiles.find(nxFileName) == nxFiles.end()) {
            std::string fullPath = nxBasePath + nxFileName;
            nxFiles[nxFileName] = std::make_unique<nl::file>(fullPath);
        }
        
        // Navigate to the node - file can be implicitly converted to node
        nl::node current = *nxFiles[nxFileName];
        
        // Split node path by /
        size_t start = 0;
        size_t end = nodePath.find('/');
        while (end != std::string::npos) {
            std::string part = nodePath.substr(start, end - start);
            if (!part.empty()) {
                current = current[part];
                if (!current) return nullptr;
            }
            start = end + 1;
            end = nodePath.find('/', start);
        }
        
        // Last part
        if (start < nodePath.length()) {
            std::string lastPart = nodePath.substr(start);
            if (!lastPart.empty()) {
                current = current[lastPart];
            }
        }
        
        if (!current) return nullptr;
        
        // Return a new allocated node
        return new nl::node(current);
    } catch (...) {
        return nullptr;
    }
}

NXWRAPPER_API const char* NX_GetNodeName(void* node) {
    if (!node) return "";
    try {
        nl::node* n = static_cast<nl::node*>(node);
        static std::string name;
        name = n->name();
        return name.c_str();
    } catch (...) {
        return "";
    }
}

NXWRAPPER_API int NX_GetNodeType(void* node) {
    if (!node) return 0;
    try {
        nl::node* n = static_cast<nl::node*>(node);
        return static_cast<int>(n->data_type());
    } catch (...) {
        return 0;
    }
}

NXWRAPPER_API int NX_GetChildCount(void* node) {
    if (!node) return 0;
    try {
        nl::node* n = static_cast<nl::node*>(node);
        return static_cast<int>(n->size());
    } catch (...) {
        return 0;
    }
}

NXWRAPPER_API long long NX_GetIntValue(void* node) {
    if (!node) return 0;
    try {
        nl::node* n = static_cast<nl::node*>(node);
        return n->get_integer();
    } catch (...) {
        return 0;
    }
}

NXWRAPPER_API double NX_GetRealValue(void* node) {
    if (!node) return 0.0;
    try {
        nl::node* n = static_cast<nl::node*>(node);
        return n->get_real();
    } catch (...) {
        return 0.0;
    }
}

NXWRAPPER_API const char* NX_GetStringValue(void* node) {
    if (!node) return "";
    try {
        nl::node* n = static_cast<nl::node*>(node);
        static std::string str;
        str = n->get_string();
        return str.c_str();
    } catch (...) {
        return "";
    }
}

NXWRAPPER_API bool NX_GetVectorValue(void* node, int* x, int* y) {
    if (!node || !x || !y) return false;
    try {
        nl::node* n = static_cast<nl::node*>(node);
        if (n->data_type() == nl::node::type::vector) {
            nl::vector2i vec = n->get_vector();
            *x = vec.first;
            *y = vec.second;
            return true;
        }
        return false;
    } catch (...) {
        return false;
    }
}

NXWRAPPER_API bool NX_GetBitmapData(void* node, unsigned char** data, int* size) {
    if (!node || !data || !size) return false;
    try {
        nl::node* n = static_cast<nl::node*>(node);
        if (n->data_type() == nl::node::type::bitmap) {
            nl::bitmap bmp = n->get_bitmap();
            *size = bmp.length();
            *data = new unsigned char[*size];
            std::memcpy(*data, bmp.data(), *size);
            return true;
        }
        return false;
    } catch (...) {
        return false;
    }
}

NXWRAPPER_API void* NX_GetChild(void* node, const char* name) {
    if (!node || !name) return nullptr;
    try {
        nl::node* n = static_cast<nl::node*>(node);
        nl::node child = (*n)[name];
        if (!child) return nullptr;
        return new nl::node(child);
    } catch (...) {
        return nullptr;
    }
}

NXWRAPPER_API void* NX_GetChildByIndex(void* node, int index) {
    if (!node || index < 0) return nullptr;
    try {
        nl::node* n = static_cast<nl::node*>(node);
        int i = 0;
        for (const auto& child : *n) {
            if (i == index) {
                return new nl::node(child);
            }
            i++;
        }
        return nullptr;
    } catch (...) {
        return nullptr;
    }
}

NXWRAPPER_API bool NX_HasChild(void* node, const char* name) {
    if (!node || !name) return false;
    try {
        nl::node* n = static_cast<nl::node*>(node);
        nl::node child = (*n)[name];
        return child.data_type() != nl::node::type::none;
    } catch (...) {
        return false;
    }
}

NXWRAPPER_API bool NX_GetOrigin(void* node, int* x, int* y) {
    if (!node || !x || !y) return false;
    try {
        nl::node* n = static_cast<nl::node*>(node);
        
        // Check if this node has an origin child
        nl::node origin = (*n)["origin"];
        if (origin && origin.data_type() == nl::node::type::vector) {
            nl::vector2i vec = origin.get_vector();
            *x = vec.first;
            *y = vec.second;
            return true;
        }
        
        return false;
    } catch (...) {
        return false;
    }
}

NXWRAPPER_API void NX_Cleanup() {
    nxFiles.clear();
}

} // extern "C"