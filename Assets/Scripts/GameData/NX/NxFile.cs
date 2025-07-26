using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Drawing;
using reNX;
using reNX.NXProperties;

namespace MapleClient.GameData
{
    public class NxFile : INxFile
    {
        private readonly string filePath;
        private NXFile nxFile;
        private NxNode rootNode;

        public bool IsLoaded { get; private set; }
        public INxNode Root => rootNode;

        public NxFile(string filePath)
        {
            this.filePath = filePath;
            
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"NX file not found: {filePath}");
            }

            // Load the actual NX file using reNX
            LoadNxFile();
        }
        
        private void LoadNxFile()
        {
            try
            {
                nxFile = new NXFile(filePath);
                rootNode = new NxNode("root");
                
                // Convert reNX nodes to our interface
                if (nxFile.BaseNode != null)
                {
                    ConvertNode(nxFile.BaseNode, rootNode);
                }
                
                IsLoaded = true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to load NX file {filePath}: {ex.Message}");
                LoadMockData();
            }
        }
        
        private void ConvertNode(NXNode sourceNode, NxNode targetNode)
        {
            foreach (var child in sourceNode)
            {
                // Get the value from the NXNode
                object value = null;
                if (child is NXValuedNode<string> stringNode)
                    value = stringNode.Value;
                else if (child is NXValuedNode<long> longNode)
                    value = longNode.Value;
                else if (child is NXValuedNode<double> doubleNode)
                    value = doubleNode.Value;
                else if (child is NXValuedNode<Point> pointNode)
                    value = new UnityEngine.Vector2(pointNode.Value.X, pointNode.Value.Y);
                else if (child is NXValuedNode<byte[]> audioNode)
                    value = audioNode.Value;
                // Note: Bitmap handling would go here if needed
                
                var childNode = new NxNode(child.Name, value);
                targetNode.AddChild(childNode);
                
                // Recursively convert children
                if (child.ChildCount > 0)
                {
                    ConvertNode(child, childNode);
                }
            }
        }

        public INxNode GetNode(string path)
        {
            if (string.IsNullOrEmpty(path))
                return Root;

            var parts = path.Split('/');
            INxNode current = Root;

            foreach (var part in parts)
            {
                if (current == null || !current.HasChild(part))
                    return null;
                    
                current = current[part];
            }

            return current;
        }

        private void LoadMockData()
        {
            // Create mock data structure for testing
            rootNode = new NxNode("root");
            IsLoaded = true;
        }
    }

    public class NxNode : INxNode
    {
        private readonly Dictionary<string, INxNode> children;
        
        public string Name { get; }
        public object Value { get; set; }
        public INxNode Parent { get; set; }
        public IEnumerable<INxNode> Children => children.Values;

        public NxNode(string name, object value = null)
        {
            Name = name;
            Value = value;
            children = new Dictionary<string, INxNode>();
        }

        public INxNode this[string childName]
        {
            get
            {
                children.TryGetValue(childName, out var child);
                return child;
            }
        }

        public T GetValue<T>()
        {
            if (Value == null)
                return default(T);
                
            if (Value is T typedValue)
                return typedValue;
                
            try
            {
                return (T)Convert.ChangeType(Value, typeof(T));
            }
            catch
            {
                return default(T);
            }
        }

        public bool HasChild(string name)
        {
            return children.ContainsKey(name);
        }

        public void AddChild(NxNode child)
        {
            children[child.Name] = child;
            child.Parent = this;
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
    }
}