using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using reNX;
using reNX.NXProperties;
using UnityEngine;

namespace MapleClient.GameData
{
    /// <summary>
    /// Implementation of INxFile that uses the actual reNX library to read real NX files
    /// </summary>
    public class RealNxFile : INxFile, IDisposable
    {
        private readonly string filePath;
        private NXFile nxFile;
        private RealNxNode rootNode;

        public bool IsLoaded { get; private set; }
        public INxNode Root => rootNode;

        public RealNxFile(string filePath)
        {
            this.filePath = filePath;
            
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"NX file not found: {filePath}");
            }

            LoadNxFile();
        }

        private void LoadNxFile()
        {
            try
            {
                // Load the NX file using reNX
                nxFile = new NXFile(filePath);
                
                // Create wrapper for root node
                rootNode = new RealNxNode(nxFile.BaseNode);
                
                IsLoaded = true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load NX file: {filePath}", ex);
            }
        }

        public INxNode GetNode(string path)
        {
            if (string.IsNullOrEmpty(path))
                return Root;

            try
            {
                // Don't use ResolvePath as it uses indexer which throws exceptions
                // Always navigate step by step
                var parts = path.Split('/');
                NXNode current = nxFile.BaseNode;
                
                foreach (var part in parts)
                {
                    if (string.IsNullOrEmpty(part))
                        continue;
                    
                    // Check if the child exists before accessing it
                    bool found = false;
                    foreach (var child in current)
                    {
                        if (child.Name == part)
                        {
                            current = child;
                            found = true;
                            break;
                        }
                    }
                    
                    if (!found)
                    {
                        return null;
                    }
                }
                
                if (current != null)
                {
                    return new RealNxNode(current);
                }
                
                return null;
            }
            catch (Exception ex)
            {
                // Only log errors for important paths to reduce spam
                if (!path.Contains("/") || path.StartsWith("00002") || path.StartsWith("00012"))
                {
                    Debug.LogError($"RealNxFile.GetNode: Exception for path '{path}': {ex.Message}");
                }
                return null;
            }
        }

        public void Dispose()
        {
            nxFile?.Dispose();
        }
        
        internal static byte[] ExtractPngFromBitmap(object bitmap, string nodeName)
        {
            try
            {
                if (bitmap == null)
                {
                    UnityEngine.Debug.LogError($"ExtractPngFromBitmap: bitmap is null for {nodeName}");
                    return null;
                }
                
                var bitmapType = bitmap.GetType();
                UnityEngine.Debug.Log($"ExtractPngFromBitmap: Processing {bitmapType.FullName} for {nodeName}");
                
                // Method 1: Check if this bitmap already has PNG data
                var pngDataProperty = bitmapType.GetProperty("PngData");
                if (pngDataProperty != null)
                {
                    var pngData = pngDataProperty.GetValue(bitmap) as byte[];
                    if (pngData != null && pngData.Length > 0)
                    {
                        UnityEngine.Debug.Log($"ExtractPngFromBitmap: Found PngData property with {pngData.Length} bytes");
                        return pngData;
                    }
                }
                
                // Method 2: Try to find and use Save method
                var saveMethods = bitmapType.GetMethods().Where(m => m.Name == "Save").ToArray();
                UnityEngine.Debug.Log($"ExtractPngFromBitmap: Found {saveMethods.Length} Save methods");
                
                // Look for Save(Stream, ImageFormat)
                var saveMethod = saveMethods.FirstOrDefault(m => 
                {
                    var parameters = m.GetParameters();
                    return parameters.Length == 2 && 
                           parameters[0].ParameterType.Name.Contains("Stream") &&
                           parameters[1].ParameterType.Name.Contains("ImageFormat");
                });
                
                if (saveMethod != null)
                {
                    UnityEngine.Debug.Log($"ExtractPngFromBitmap: Found Save method with correct signature");
                    
                    using (var ms = new System.IO.MemoryStream())
                    {
                        // Get ImageFormat type and find Png property
                        var imageFormatParam = saveMethod.GetParameters()[1];
                        var imageFormatType = imageFormatParam.ParameterType;
                        
                        // Get the Png property
                        var pngProperty = imageFormatType.GetProperty("Png", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        if (pngProperty != null)
                        {
                            var pngFormat = pngProperty.GetValue(null);
                            
                            if (pngFormat != null)
                            {
                                try
                                {
                                    saveMethod.Invoke(bitmap, new object[] { ms, pngFormat });
                                    ms.Position = 0; // Reset position before reading
                                    var pngData = ms.ToArray();
                                    
                                    // Verify PNG header
                                    if (pngData.Length > 8 && pngData[0] == 0x89 && pngData[1] == 0x50 && 
                                        pngData[2] == 0x4E && pngData[3] == 0x47)
                                    {
                                        UnityEngine.Debug.Log($"ExtractPngFromBitmap: Successfully extracted valid PNG: {pngData.Length} bytes from {nodeName}");
                                        return pngData;
                                    }
                                    else
                                    {
                                        UnityEngine.Debug.LogError($"ExtractPngFromBitmap: Invalid PNG header for {nodeName}. First 8 bytes: {string.Join(" ", pngData.Take(8).Select(b => b.ToString("X2")))}");
                                    }
                                }
                                catch (Exception saveEx)
                                {
                                    UnityEngine.Debug.LogError($"ExtractPngFromBitmap: Error during save for {nodeName}: {saveEx.Message}");
                                    if (saveEx.InnerException != null)
                                    {
                                        UnityEngine.Debug.LogError($"ExtractPngFromBitmap: Inner save error: {saveEx.InnerException.Message}");
                                    }
                                }
                            }
                            else
                            {
                                UnityEngine.Debug.LogError($"ExtractPngFromBitmap: Png format property value is null for {nodeName}");
                            }
                        }
                        else
                        {
                            UnityEngine.Debug.LogError($"ExtractPngFromBitmap: Could not find Png property on ImageFormat type for {nodeName}");
                        }
                    }
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"ExtractPngFromBitmap: No suitable Save method found for {nodeName}, trying alternative approach");
                }
                
                // Method 3: Alternative approach - pixel-by-pixel extraction
                try
                {
                    UnityEngine.Debug.Log($"ExtractPngFromBitmap: Trying pixel-by-pixel extraction for {nodeName}");
                    
                    // Get Width and Height
                    var widthProp = bitmapType.GetProperty("Width");
                    var heightProp = bitmapType.GetProperty("Height");
                    
                    if (widthProp != null && heightProp != null)
                    {
                        int width = (int)widthProp.GetValue(bitmap);
                        int height = (int)heightProp.GetValue(bitmap);
                        
                        UnityEngine.Debug.Log($"ExtractPngFromBitmap: Bitmap dimensions: {width}x{height} for {nodeName}");
                        
                        if (width > 0 && height > 0 && width < 4096 && height < 4096) // Sanity check
                        {
                            // Try to get pixel data through GetPixel
                            var getPixelMethod = bitmapType.GetMethod("GetPixel");
                            if (getPixelMethod != null)
                            {
                                UnityEngine.Debug.Log($"ExtractPngFromBitmap: Found GetPixel method, extracting pixels for {nodeName}");
                                
                                // Create a Unity Texture2D and extract pixels
                                var texture = new UnityEngine.Texture2D(width, height, UnityEngine.TextureFormat.ARGB32, false);
                                
                                // Extract pixels - note: this might be slow for large images
                                for (int y = 0; y < height; y++)
                                {
                                    for (int x = 0; x < width; x++)
                                    {
                                        try
                                        {
                                            var pixel = getPixelMethod.Invoke(bitmap, new object[] { x, y });
                                            if (pixel != null)
                                            {
                                                // Extract color components
                                                var pixelType = pixel.GetType();
                                                var rProp = pixelType.GetProperty("R");
                                                var gProp = pixelType.GetProperty("G");
                                                var bProp = pixelType.GetProperty("B");
                                                var aProp = pixelType.GetProperty("A");
                                                
                                                if (rProp != null && gProp != null && bProp != null && aProp != null)
                                                {
                                                    byte r = (byte)rProp.GetValue(pixel);
                                                    byte g = (byte)gProp.GetValue(pixel);
                                                    byte b = (byte)bProp.GetValue(pixel);
                                                    byte a = (byte)aProp.GetValue(pixel);
                                                    
                                                    var color = new UnityEngine.Color32(r, g, b, a);
                                                texture.SetPixel(x, height - 1 - y, color);
                                                
                                                // Log first pixel for debugging
                                                if (x == 0 && y == 0)
                                                {
                                                    UnityEngine.Debug.Log($"ExtractPngFromBitmap: First pixel color: R={r}, G={g}, B={b}, A={a}");
                                                }
                                                }
                                            }
                                        }
                                        catch (Exception pixelEx)
                                        {
                                            // Log only first pixel error to avoid spam
                                            if (x == 0 && y == 0)
                                            {
                                                UnityEngine.Debug.LogError($"ExtractPngFromBitmap: GetPixel failed for {nodeName}: {pixelEx.Message}");
                                            }
                                            // Set default pixel
                                            texture.SetPixel(x, height - 1 - y, UnityEngine.Color.clear);
                                        }
                                    }
                                }
                                
                                texture.Apply();
                                var pngData = texture.EncodeToPNG();
                                UnityEngine.Object.Destroy(texture);
                                
                                if (pngData != null && pngData.Length > 0)
                                {
                                    UnityEngine.Debug.Log($"ExtractPngFromBitmap: Pixel-by-pixel extraction succeeded for {nodeName}: {pngData.Length} bytes");
                                    return pngData;
                                }
                                else
                                {
                                    UnityEngine.Debug.LogError($"ExtractPngFromBitmap: Pixel-by-pixel extraction produced null/empty data for {nodeName}");
                                }
                            }
                            else
                            {
                                UnityEngine.Debug.LogError($"ExtractPngFromBitmap: No GetPixel method found for {nodeName}");
                            }
                        }
                        else
                        {
                            UnityEngine.Debug.LogError($"ExtractPngFromBitmap: Invalid dimensions {width}x{height} for {nodeName}");
                        }
                    }
                    else
                    {
                        UnityEngine.Debug.LogError($"ExtractPngFromBitmap: Could not find Width/Height properties for {nodeName}");
                    }
                }
                catch (Exception altEx)
                {
                    UnityEngine.Debug.LogError($"ExtractPngFromBitmap: Alternative extraction failed for {nodeName}: {altEx.Message}");
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"ExtractPngFromBitmap: Failed to extract PNG from bitmap {nodeName}: {ex.Message}");
                if (ex.InnerException != null)
                {
                    UnityEngine.Debug.LogError($"ExtractPngFromBitmap: Inner exception: {ex.InnerException.Message}");
                }
            }
            
            UnityEngine.Debug.LogError($"ExtractPngFromBitmap: All extraction methods failed for {nodeName}");
            return null;
        }
    }

    /// <summary>
    /// Wrapper for reNX NXNode to implement our INxNode interface
    /// </summary>
    public class RealNxNode : INxNode
    {
        private readonly NXNode nxNode;
        private Dictionary<string, INxNode> childrenCache;
        private INxNode parent;

        public string Name => nxNode.Name;
        public INxNode Parent { get; set; }
        
        public object Value 
        { 
            get 
            {
                
                // First, try to handle common types directly
                if (nxNode is NXValuedNode<string> stringNode)
                    return stringNode.Value;
                else if (nxNode is NXValuedNode<long> longNode)
                    return longNode.Value;
                else if (nxNode is NXValuedNode<double> doubleNode)
                    return doubleNode.Value;
                else if (nxNode is NXValuedNode<byte[]> audioNode)
                    return audioNode.Value;
                    
                // Special case: NXBitmapNode directly - check both direct type and interface
                var nodeTypeName = nxNode.GetType().Name;
                if (nodeTypeName == "NXBitmapNode" || nodeTypeName.Contains("Bitmap"))
                {
                    UnityEngine.Debug.Log($"RealNxNode.Value: Detected bitmap node type: {nodeTypeName} for {nxNode.Name}");
                    var valueProperty = nxNode.GetType().GetProperty("Value");
                    if (valueProperty != null)
                    {
                        var bitmap = valueProperty.GetValue(nxNode);
                        if (bitmap != null)
                        {
                            UnityEngine.Debug.Log($"RealNxNode.Value: Found bitmap value of type {bitmap.GetType().FullName}");
                            var pngData = RealNxFile.ExtractPngFromBitmap(bitmap, nxNode.Name);
                            if (pngData != null && pngData.Length > 0)
                            {
                                return pngData;
                            }
                        }
                        else
                        {
                            UnityEngine.Debug.LogWarning($"RealNxNode.Value: Bitmap node but Value property returned null for {nxNode.Name}");
                        }
                    }
                }
                
                // For other types, use reflection to check the actual type
                var nodeType = nxNode.GetType();
                
                // Check if it's any kind of NXValuedNode
                if (nodeType.BaseType != null && nodeType.BaseType.IsGenericType)
                {
                    var baseType = nodeType.BaseType;
                    var genericTypeDef = baseType.GetGenericTypeDefinition();
                    
                    // Check if it's NXValuedNode<T>
                    if (genericTypeDef.Name == "NXValuedNode`1")
                    {
                        var valueProperty = nodeType.GetProperty("Value");
                        if (valueProperty != null)
                        {
                            try
                            {
                                var value = valueProperty.GetValue(nxNode);
                                if (value != null)
                                {
                                    var valueType = value.GetType();
                                    
                                    // Handle Point type
                                    if (valueType.Name == "Point")
                                    {
                                        var xProp = valueType.GetProperty("X");
                                        var yProp = valueType.GetProperty("Y");
                                        if (xProp != null && yProp != null)
                                        {
                                            var x = (int)xProp.GetValue(value);
                                            var y = (int)yProp.GetValue(value);
                                            return new Vector2(x, y);
                                        }
                                    }
                                    // Handle Bitmap type
                                    else if (valueType.Name == "Bitmap")
                                    {
                                        return RealNxFile.ExtractPngFromBitmap(value, nxNode.Name);
                                    }
                                    
                                    // Return the value as-is for other types
                                    return value;
                                }
                            }
                            catch (Exception ex)
                            {
                                UnityEngine.Debug.LogWarning($"Failed to get value from node {nxNode.Name}: {ex.Message}");
                                if (ex.InnerException != null)
                                {
                                    UnityEngine.Debug.LogError($"Inner exception: {ex.InnerException.Message}\nStack: {ex.InnerException.StackTrace}");
                                }
                            }
                        }
                    }
                }
                
                // C++ client pattern: Check if this is a container node with bitmap children
                // If no value found and node has children, look for bitmap children
                try
                {
                    // Check if node has children by trying to enumerate
                    var hasChildren = false;
                    try 
                    {
                        hasChildren = nxNode.Any();
                    }
                    catch { }
                    
                    
                    if (hasChildren)
                    {
                        // First check if this node has a 'map' child with bitmap references
                        NXNode mapNode = null;
                        foreach (var child in nxNode)
                        {
                            if (child.Name == "map")
                            {
                                mapNode = child;
                                break;
                            }
                        }
                        
                        if (mapNode != null)
                        {
                            foreach (var mapChild in mapNode)
                            {
                                
                                // Check if the map child is a bitmap
                                var childType = mapChild.GetType();
                                if (childType.BaseType != null && childType.BaseType.IsGenericType)
                                {
                                    var baseType = childType.BaseType;
                                    if (baseType.GetGenericTypeDefinition().Name == "NXValuedNode`1")
                                    {
                                        var genArgs = baseType.GetGenericArguments();
                                        if (genArgs.Length > 0 && (genArgs[0].Name == "Bitmap" || genArgs[0].FullName.Contains("Drawing.Bitmap")))
                                        {
                                            var valueProperty = childType.GetProperty("Value");
                                            if (valueProperty != null)
                                            {
                                                var bitmap = valueProperty.GetValue(mapChild);
                                                if (bitmap != null)
                                                {
                                                    var pngData = RealNxFile.ExtractPngFromBitmap(bitmap, mapChild.Name);
                                                    if (pngData != null)
                                                    {
                                                        return pngData;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        
                        // Simple approach: iterate all children and check if any is a bitmap
                        foreach (var child in nxNode)
                        {
                            try
                            {
                                // Check if this child is a valued node with bitmap
                                var childType = child.GetType();
                                
                                // Look for NXValuedNode base type
                                var currentType = childType;
                                while (currentType != null && currentType.BaseType != null)
                                {
                                    if (currentType.BaseType.IsGenericType && 
                                        currentType.BaseType.GetGenericTypeDefinition().Name == "NXValuedNode`1")
                                    {
                                        var genArgs = currentType.BaseType.GetGenericArguments();
                                        if (genArgs.Length > 0)
                                        {
                                            
                                            // Check if it's a bitmap type
                                            if (genArgs[0].Name == "Bitmap" || genArgs[0].FullName.Contains("Drawing.Bitmap") || genArgs[0].Name.Contains("Bitmap"))
                                            {
                                                UnityEngine.Debug.Log($"RealNxNode.Value: Found bitmap child '{child.Name}' with generic type {genArgs[0].FullName}");
                                                
                                                // Get the Value property
                                                var valueProperty = childType.GetProperty("Value");
                                                if (valueProperty != null)
                                                {
                                                    var bitmap = valueProperty.GetValue(child);
                                                    if (bitmap != null)
                                                    {
                                                        UnityEngine.Debug.Log($"RealNxNode.Value: Got bitmap value from child '{child.Name}'");
                                                        var pngData = RealNxFile.ExtractPngFromBitmap(bitmap, child.Name);
                                                        if (pngData != null && pngData.Length > 0)
                                                        {
                                                            UnityEngine.Debug.Log($"RealNxNode.Value: Container bitmap extracted: {pngData.Length} bytes from child '{child.Name}'");
                                                            return pngData;
                                                        }
                                                        else
                                                        {
                                                            UnityEngine.Debug.LogWarning($"RealNxNode.Value: ExtractPngFromBitmap returned null or empty for child '{child.Name}'");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        UnityEngine.Debug.LogWarning($"RealNxNode.Value: Bitmap value is null for child '{child.Name}'");
                                                    }
                                                }
                                                else
                                                {
                                                    UnityEngine.Debug.LogWarning($"RealNxNode.Value: No Value property found on bitmap child '{child.Name}'");
                                                }
                                            }
                                        }
                                        break;
                                    }
                                    currentType = currentType.BaseType;
                                }
                            }
                            catch (Exception childEx)
                            {
                                // Log child processing error with details
                                UnityEngine.Debug.LogError($"Container child processing error for {child.Name}: {childEx.Message}");
                                if (childEx.InnerException != null)
                                {
                                    UnityEngine.Debug.LogError($"Inner: {childEx.InnerException.Message}");
                                }
                            }
                        }
                    }
                }
                catch (Exception containerEx)
                {
                    // Container handling failed
                    UnityEngine.Debug.LogError($"Container handling error: {containerEx.Message}\nInner: {containerEx.InnerException?.Message}\nStack: {containerEx.StackTrace}");
                }
                
                return null;
            }
            set { /* Read-only from NX files */ }
        }

        public IEnumerable<INxNode> Children
        {
            get
            {
                if (childrenCache == null)
                {
                    childrenCache = new Dictionary<string, INxNode>();
                    foreach (var child in nxNode)
                    {
                        var childNode = new RealNxNode(child);
                        childNode.Parent = this;
                        childrenCache[child.Name] = childNode;
                    }
                }
                return childrenCache.Values;
            }
        }

        public RealNxNode(NXNode nxNode)
        {
            this.nxNode = nxNode ?? throw new ArgumentNullException(nameof(nxNode));
        }

        public INxNode this[string childName]
        {
            get
            {
                if (childrenCache == null)
                {
                    // Force population of children cache
                    var _ = Children;
                }
                
                childrenCache.TryGetValue(childName, out var child);
                return child;
            }
        }
        
        // Helper method to safely get child node without using indexer
        private static NXNode GetChildSafe(NXNode parent, string childName)
        {
            if (parent == null || string.IsNullOrEmpty(childName))
                return null;
                
            foreach (var child in parent)
            {
                if (child.Name == childName)
                    return child;
            }
            
            return null;
        }

        public T GetValue<T>()
        {
            var val = Value;
            if (val == null)
                return default(T);
                
            if (val is T typedValue)
                return typedValue;
                
            // Handle numeric conversions
            if (typeof(T) == typeof(int))
            {
                if (val is long l)
                    return (T)(object)(int)l;
                if (val is double d)
                    return (T)(object)(int)d;
            }
            else if (typeof(T) == typeof(float))
            {
                if (val is double d)
                    return (T)(object)(float)d;
                if (val is long l)
                    return (T)(object)(float)l;
            }
            else if (typeof(T) == typeof(string))
            {
                return (T)(object)val.ToString();
            }
                
            try
            {
                return (T)Convert.ChangeType(val, typeof(T));
            }
            catch
            {
                return default(T);
            }
        }

        public bool HasChild(string name)
        {
            return nxNode.ContainsChild(name);
        }
        
        public INxNode GetNode(string path)
        {
            if (string.IsNullOrEmpty(path))
                return this;

            var parts = path.Split('/');
            INxNode current = this;

            foreach (var part in parts)
            {
                if (current == null || !current.HasChild(part))
                    return null;
                    
                current = current[part];
            }

            return current;
        }

        // Removed - bitmap conversion is handled by SpriteLoader
    }
}