using System.Collections.Generic;

namespace MapleClient.GameData
{
    public interface INxFile
    {
        bool IsLoaded { get; }
        INxNode Root { get; }
        INxNode GetNode(string path);
    }

    public interface INxNode
    {
        string Name { get; }
        object Value { get; }
        INxNode Parent { get; }
        IEnumerable<INxNode> Children { get; }
        INxNode this[string childName] { get; }
        T GetValue<T>();
        bool HasChild(string name);
        INxNode GetNode(string path);
    }
}