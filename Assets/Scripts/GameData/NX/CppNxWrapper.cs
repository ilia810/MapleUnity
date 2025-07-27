using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace MapleClient.GameData
{
    /// <summary>
    /// Wrapper for the C++ nx library via P/Invoke
    /// </summary>
    public static class CppNxWrapper
    {
        private const string DLL_NAME = "NXWrapper";
        
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool NX_Initialize([MarshalAs(UnmanagedType.LPStr)] string nxPath);
        
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr NX_GetNode([MarshalAs(UnmanagedType.LPStr)] string path);
        
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr NX_GetNodeName(IntPtr node);
        
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int NX_GetNodeType(IntPtr node);
        
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int NX_GetChildCount(IntPtr node);
        
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern long NX_GetIntValue(IntPtr node);
        
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern double NX_GetRealValue(IntPtr node);
        
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr NX_GetStringValue(IntPtr node);
        
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool NX_GetVectorValue(IntPtr node, out int x, out int y);
        
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool NX_GetBitmapData(IntPtr node, out IntPtr data, out int size);
        
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr NX_GetChild(IntPtr node, [MarshalAs(UnmanagedType.LPStr)] string name);
        
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr NX_GetChildByIndex(IntPtr node, int index);
        
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool NX_HasChild(IntPtr node, [MarshalAs(UnmanagedType.LPStr)] string name);
        
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool NX_GetOrigin(IntPtr node, out int x, out int y);
        
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void NX_Cleanup();
        
        // Node types
        public enum NodeType
        {
            None = 0,
            Integer = 1,
            Real = 2,
            String = 3,
            Vector = 4,
            Bitmap = 5,
            Audio = 6
        }
        
        private static bool initialized = false;
        
        public static bool Initialize(string nxPath)
        {
            if (initialized) return true;
            
            try
            {
                initialized = NX_Initialize(nxPath);
                if (initialized)
                {
                    Debug.Log($"C++ NX library initialized with path: {nxPath}");
                }
                else
                {
                    Debug.LogError($"Failed to initialize C++ NX library with path: {nxPath}");
                }
                return initialized;
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception initializing C++ NX library: {e.Message}");
                return false;
            }
        }
        
        public static void Cleanup()
        {
            if (initialized)
            {
                NX_Cleanup();
                initialized = false;
            }
        }
        
        public static IntPtr GetNode(string path)
        {
            if (!initialized) return IntPtr.Zero;
            return NX_GetNode(path);
        }
        
        public static string GetNodeName(IntPtr node)
        {
            if (node == IntPtr.Zero) return "";
            var ptr = NX_GetNodeName(node);
            return Marshal.PtrToStringAnsi(ptr) ?? "";
        }
        
        public static NodeType GetNodeType(IntPtr node)
        {
            if (node == IntPtr.Zero) return NodeType.None;
            return (NodeType)NX_GetNodeType(node);
        }
        
        public static bool HasChild(IntPtr node, string name)
        {
            if (node == IntPtr.Zero) return false;
            return NX_HasChild(node, name);
        }
        
        public static IntPtr GetChild(IntPtr node, string name)
        {
            if (node == IntPtr.Zero) return IntPtr.Zero;
            return NX_GetChild(node, name);
        }
        
        public static bool GetOrigin(IntPtr node, out Vector2 origin)
        {
            origin = Vector2.zero;
            if (node == IntPtr.Zero) return false;
            
            if (NX_GetOrigin(node, out int x, out int y))
            {
                origin = new Vector2(x, y);
                return true;
            }
            return false;
        }
        
        public static int GetChildCount(IntPtr node)
        {
            if (node == IntPtr.Zero) return 0;
            return NX_GetChildCount(node);
        }
        
        public static byte[] GetBitmapData(IntPtr node)
        {
            if (node == IntPtr.Zero) return null;
            
            if (NX_GetBitmapData(node, out IntPtr dataPtr, out int size))
            {
                byte[] data = new byte[size];
                Marshal.Copy(dataPtr, data, 0, size);
                // Note: We should free the allocated memory in C++
                // Add a FreeData function to the DLL
                return data;
            }
            return null;
        }
        
        public static Vector2? GetVectorValue(IntPtr node)
        {
            if (node == IntPtr.Zero) return null;
            
            if (NX_GetVectorValue(node, out int x, out int y))
            {
                return new Vector2(x, y);
            }
            return null;
        }
    }
}