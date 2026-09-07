using System.Collections.Generic;
using System.Linq;

namespace MapleClient.GameData
{
    /// <summary>Authored ambient speech keys only; dialogue greetings are not ambient lines.</summary>
    public static class NxNpcSpeech
    {
        public static string[] Read(INxFile npcs,INxFile strings,int id)
        {
            string path=NxNpcAnimations.ResolvePath(npcs,id);
            var source=path==null?null:npcs.GetNode(path);
            var text=strings?.GetNode("Npc.img/"+id);if(source==null || text==null)return new string[0];
            return source.Children.SelectMany(state=>state["speak"]?.Children ?? Enumerable.Empty<INxNode>())
                .Select(n=>n.GetValue<string>()).Where(key=>!string.IsNullOrEmpty(key))
                .Select(key=>text[key]?.GetValue<string>()).Where(line=>!string.IsNullOrWhiteSpace(line)).Distinct().ToArray();
        }
    }
}
