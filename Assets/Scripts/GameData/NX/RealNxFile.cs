using System;
using System.Collections.Generic;
using System.Drawing;
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
                // Silently handle exceptions
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
                    return null;
                }
                
                var bitmapType = bitmap.GetType();
                
                // Method 1: Check if this bitmap already has PNG data
                var pngDataProperty = bitmapType.GetProperty("PngData");
                if (pngDataProperty != null)
                {
                    var pngData = pngDataProperty.GetValue(bitmap) as byte[];
                    if (pngData != null && pngData.Length > 0)
                    {
                        return pngData;
                    }
                }
                
                // Method 2: Try to find and use Save method
                var saveMethods = bitmapType.GetMethods().Where(m => m.Name == "Save").ToArray();
                
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
                                        return pngData;
                                    }
                                    else
                                    {
                                    }
                                }
                                catch (Exception saveEx)
                                {
                                }
                            }
                            else
                            {
                            }
                        }
                        else
                        {
                        }
                    }
                }
                else
                {
                }
                
                // Method 3: Alternative approach - pixel-by-pixel extraction
                try
                {
                    
                    // Get Width and Height
                    var widthProp = bitmapType.GetProperty("Width");
                    var heightProp = bitmapType.GetProperty("Height");
                    
                    if (widthProp != null && heightProp != null)
                    {
                        int width = (int)widthProp.GetValue(bitmap);
                        int height = (int)heightProp.GetValue(bitmap);
                        
                        
                        if (width > 0 && height > 0 && width < 4096 && height < 4096) // Sanity check
                        {
                            // Try to get pixel data through GetPixel
                            var getPixelMethod = bitmapType.GetMethod("GetPixel");
                            if (getPixelMethod != null)
                            {
                                
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
                                                }
                                                }
                                            }
                                        }
                                        catch (Exception pixelEx)
                                        {
                                            // Log only first pixel error to avoid spam
                                            if (x == 0 && y == 0)
                                            {
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
                                    return pngData;
                                }
                                else
                                {
                                }
                            }
                            else
                            {
                            }
                        }
                        else
                        {
                        }
                    }
                    else
                    {
                    }
                }
                catch (Exception altEx)
                {
                }
            }
            catch (Exception ex)
            {
            }
            
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
                    var valueProperty = nxNode.GetType().GetProperty("Value");
                    if (valueProperty != null)
                    {
                        var bitmap = valueProperty.GetValue(nxNode);
                        if (bitmap != null)
                        {
                            var pngData = RealNxFile.ExtractPngFromBitmap(bitmap, nxNode.Name);
                            if (pngData != null && pngData.Length > 0)
                            {
                                return pngData;
                            }
                        }
                        else
                        {
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
                                                
                                                // Get the Value property
                                                var valueProperty = childType.GetProperty("Value");
                                                if (valueProperty != null)
                                                {
                                                    var bitmap = valueProperty.GetValue(child);
                                                    if (bitmap != null)
                                                    {
                                                        var pngData = RealNxFile.ExtractPngFromBitmap(bitmap, child.Name);
                                                        if (pngData != null && pngData.Length > 0)
                                                        {
                                                            return pngData;
                                                        }
                                                        else
                                                        {
                                                        }
                                                    }
                                                    else
                                                    {
                                                    }
                                                }
                                                else
                                                {
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
                            }
                        }
                    }
                }
                catch (Exception containerEx)
                {
                    // Container handling failed silently
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
                    
                    // First, add all real children
                    foreach (var child in nxNode)
                    {
                        var childNode = new RealNxNode(child);
                        childNode.Parent = this;
                        childrenCache[child.Name] = childNode;
                    }
                    
                    // ALWAYS check for origin on bitmap nodes, regardless of whether they have children
                    TryAddVirtualOriginNode();
                }
                return childrenCache.Values;
            }
        }
        
        private void TryAddVirtualOriginNode()
        {
            // Skip if we already have an origin child
            if (childrenCache.ContainsKey("origin"))
                return;
                
            var nodeType = nxNode.GetType();
            var nodeTypeName = nodeType.Name;
            
            // DEBUG: Log what type we're checking
            if (Name == "0" || Name == "1" || Name == "post" || Name == "sign")
            {
                UnityEngine.Debug.Log($"[TryAddVirtualOriginNode] Checking node '{Name}' of type: {nodeTypeName}");
                
                // Also log if this node has any bitmap children
                try
                {
                    int bitmapChildCount = 0;
                    foreach (var child in nxNode)
                    {
                        if (child.GetType().Name.Contains("Bitmap"))
                            bitmapChildCount++;
                    }
                    if (bitmapChildCount > 0)
                    {
                        UnityEngine.Debug.Log($"  Node has {bitmapChildCount} bitmap children");
                    }
                }
                catch { }
            }
            
            // Check if this node has image data by checking if Value returns byte[]
            bool isImageNode = false;
            try
            {
                var value = this.Value; // Use our own Value property which handles the conversion
                if (value is byte[] imageData && imageData.Length > 0)
                {
                    isImageNode = true;
                    if (Name == "0" || Name == "1")
                    {
                        UnityEngine.Debug.Log($"  Node has image data (byte[{imageData.Length}]), treating as image node");
                    }
                }
            }
            catch { }
            
            // Also check by type name
            if (!isImageNode && (nodeTypeName.Contains("Bitmap") || nodeTypeName.Contains("Canvas")))
            {
                isImageNode = true;
                if (Name == "0" || Name == "1")
                {
                    UnityEngine.Debug.Log($"  Node type contains Bitmap/Canvas, treating as image node");
                }
            }
            
            // NEW: Also check if this is a container node with bitmap children
            bool isContainerWithBitmap = false;
            if (!isImageNode)
            {
                try
                {
                    foreach (var child in nxNode)
                    {
                        if (child.GetType().Name.Contains("Bitmap"))
                        {
                            isContainerWithBitmap = true;
                            if (Name == "0" || Name == "1")
                            {
                                UnityEngine.Debug.Log($"  Node is container with bitmap child, checking for origin");
                            }
                            break;
                        }
                    }
                }
                catch { }
            }
            
            if (isImageNode || isContainerWithBitmap)
            {
                
                // Try multiple approaches to find the origin
                
                // DEBUG: Log that we're searching for origin
                if (Name == "0" || Name == "1")
                {
                    UnityEngine.Debug.Log($"  Searching for origin on image node '{Name}'...");
                }
                
                // Approach 1: Look for Origin property (public or non-public)
                var originProperty = nodeType.GetProperty("Origin", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (originProperty == null)
                {
                    originProperty = nodeType.GetProperty("origin", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                }
                
                if (originProperty != null)
                {
                    try
                    {
                        var originValue = originProperty.GetValue(nxNode);
                        if (originValue != null)
                        {
                            childrenCache["origin"] = new VirtualOriginNode(originValue);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Silently continue to next approach
                    }
                }
                
                // Approach 2: Look for origin in fields (sometimes properties are backed by fields)
                var allFields = nodeType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                
                foreach (var field in allFields)
                {
                    if (field.Name.ToLower().Contains("origin"))
                    {
                        try
                        {
                            var originValue = field.GetValue(nxNode);
                            if (originValue != null)
                            {
                                childrenCache["origin"] = new VirtualOriginNode(originValue);
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            // Silently continue to next field
                        }
                    }
                }
                
                // Approach 3: Check if node has a Canvas property that contains origin
                var canvasProperty = nodeType.GetProperty("Canvas");
                if (canvasProperty != null)
                {
                    try
                    {
                        var canvas = canvasProperty.GetValue(nxNode);
                        if (canvas != null)
                        {
                            var canvasType = canvas.GetType();
                            // Look for origin in canvas
                            var canvasOriginProp = canvasType.GetProperty("Origin") ?? canvasType.GetProperty("origin");
                            if (canvasOriginProp != null)
                            {
                                var originValue = canvasOriginProp.GetValue(canvas);
                                if (originValue != null)
                                {
                                    childrenCache["origin"] = new VirtualOriginNode(originValue);
                                    return;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Silently continue to next approach
                    }
                }
                
                // Approach 4: Check base classes
                var baseType = nodeType.BaseType;
                while (baseType != null && baseType != typeof(object))
                {
                    
                    var baseOriginProp = baseType.GetProperty("Origin", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (baseOriginProp != null)
                    {
                        try
                        {
                            var originValue = baseOriginProp.GetValue(nxNode);
                            if (originValue != null)
                            {
                                childrenCache["origin"] = new VirtualOriginNode(originValue);
                                return;
                            }
                        }
                        catch { }
                    }
                    
                    baseType = baseType.BaseType;
                }
                
                // Approach 5: Try to access NX-specific properties
                // The NX format stores canvas info which includes origin
                if (nodeTypeName == "NXCanvasNode" || nodeTypeName.Contains("Canvas"))
                {
                    
                    // Try accessing Width/Height which are often alongside origin
                    var widthProp = nodeType.GetProperty("Width");
                    var heightProp = nodeType.GetProperty("Height");
                    if (widthProp != null && heightProp != null)
                    {
                        
                        // Look for X/Y properties (sometimes origin is stored as X,Y)
                        var xProp = nodeType.GetProperty("X") ?? nodeType.GetProperty("OriginX");
                        var yProp = nodeType.GetProperty("Y") ?? nodeType.GetProperty("OriginY");
                        
                        if (xProp != null && yProp != null)
                        {
                            try
                            {
                                var x = xProp.GetValue(nxNode);
                                var y = yProp.GetValue(nxNode);
                                if (x != null && y != null)
                                {
                                    var originPoint = new System.Drawing.Point(Convert.ToInt32(x), Convert.ToInt32(y));
                                    childrenCache["origin"] = new VirtualOriginNode(originPoint);
                                    return;
                                }
                            }
                            catch { }
                        }
                    }
                }
                
                // Approach 6: For container nodes with bitmap children, check if origin is in the data
                if (isContainerWithBitmap)
                {
                    // The container node might have origin data that's not exposed as a child
                    // Try to access it through the internal structure
                    
                    // DEBUG: Log all children to understand structure
                    if (Name == "0" || Name == "1")
                    {
                        UnityEngine.Debug.Log($"  Container node children:");
                        foreach (var child in nxNode)
                        {
                            UnityEngine.Debug.Log($"    - {child.Name} ({child.GetType().Name})");
                        }
                    }
                    
                    // Try accessing through indexer with different keys
                    string[] possibleKeys = { "origin", "Origin", "_origin", "offset" };
                    foreach (var key in possibleKeys)
                    {
                        try
                        {
                            // Use the NXNode's indexer directly
                            var indexerProp = nodeType.GetProperty("Item", new Type[] { typeof(string) });
                            if (indexerProp != null)
                            {
                                var result = indexerProp.GetValue(nxNode, new object[] { key });
                                if (result != null)
                                {
                                    UnityEngine.Debug.Log($"  Found property via indexer['{key}']: {result.GetType().Name}");
                                    // This might be another NXNode with the origin value
                                    var resultType = result.GetType();
                                    if (resultType.Name.Contains("NXNode"))
                                    {
                                        // Try to get its value
                                        var getMethod = resultType.GetMethod("Get", Type.EmptyTypes);
                                        if (getMethod != null)
                                        {
                                            var value = getMethod.Invoke(result, null);
                                            if (value != null)
                                            {
                                                UnityEngine.Debug.Log($"    Value: {value} (type: {value.GetType().Name})");
                                                childrenCache["origin"] = new VirtualOriginNode(value);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }
                
                // Approach 7: Check if this node has NX property children that weren't enumerated
                // Sometimes NX nodes have properties stored as pseudo-children
                try
                {
                    // Use reflection to find a method that might give us properties
                    var getPropertyMethod = nodeType.GetMethod("GetProperty", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (getPropertyMethod != null)
                    {
                        var originProp = getPropertyMethod.Invoke(nxNode, new object[] { "origin" });
                        if (originProp != null)
                        {
                            // This might be an NXProperty node, try to get its value
                            var propType = originProp.GetType();
                            var valueProp = propType.GetProperty("Value");
                            if (valueProp != null)
                            {
                                var originValue = valueProp.GetValue(originProp);
                                if (originValue != null)
                                {
                                    childrenCache["origin"] = new VirtualOriginNode(originValue);
                                    return;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Silently continue
                }
                
                // DEBUG: Log if no origin found
                if (Name == "0" || Name == "1")
                {
                    UnityEngine.Debug.Log($"  No origin found for image node '{Name}'");
                }
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

    /// <summary>
    /// Virtual node to represent origin data that wasn't exposed as a child node by reNX
    /// </summary>
    public class VirtualOriginNode : INxNode
    {
        private readonly object originValue;
        private Vector2? convertedValue;

        public string Name => "origin";
        public INxNode Parent { get; set; }
        
        public object Value 
        { 
            get 
            {
                if (convertedValue.HasValue)
                    return convertedValue.Value;
                    
                // Convert the origin value to Vector2
                if (originValue != null)
                {
                    var valueType = originValue.GetType();
                    if (valueType.Name == "Point")
                    {
                        var xProp = valueType.GetProperty("X");
                        var yProp = valueType.GetProperty("Y");
                        if (xProp != null && yProp != null)
                        {
                            var x = Convert.ToInt32(xProp.GetValue(originValue));
                            var y = Convert.ToInt32(yProp.GetValue(originValue));
                            convertedValue = new Vector2(x, y);
                            return convertedValue.Value;
                        }
                    }
                    else if (originValue is Vector2 vec)
                    {
                        convertedValue = vec;
                        return vec;
                    }
                }
                
                return Vector2.zero;
            }
            set { /* Read-only */ }
        }

        public IEnumerable<INxNode> Children => Enumerable.Empty<INxNode>();

        public VirtualOriginNode(object originValue)
        {
            this.originValue = originValue ?? throw new ArgumentNullException(nameof(originValue));
        }

        public INxNode this[string childName] => null;
        
        public T GetValue<T>()
        {
            var val = Value;
            if (val == null)
                return default(T);
                
            if (val is T typedValue)
                return typedValue;
                
            try
            {
                return (T)Convert.ChangeType(val, typeof(T));
            }
            catch
            {
                return default(T);
            }
        }

        public bool HasChild(string name) => false;
        
        public INxNode GetNode(string path) => string.IsNullOrEmpty(path) ? this : null;
    }
}