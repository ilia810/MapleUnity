using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MapleClient.GameData
{
    public class NxFile : INxFile
    {
        private readonly string filePath;
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

            // For now, we'll create mock data
            // In a real implementation, this would parse the NX file format
            LoadMockData();
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
        }
    }
}