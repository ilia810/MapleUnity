using System.Collections.Generic;
using System.Linq;

namespace MapleClient.GameData
{
    /// <summary>Resolve shared NPC art without changing the original NPC's identity.</summary>
    public static class NxNpcAnimations
    {
        public static string ResolvePath(INxFile npcs,int id)
        {
            var seen=new HashSet<int>();
            while(seen.Add(id))
            {
                string path=id.ToString("D7")+".img";
                var node=npcs?.GetNode(path);if(node==null)return null;
                string link=node["info"]?["link"]?.Value?.ToString();
                if(string.IsNullOrEmpty(link))return path;
                if(!int.TryParse(link.Replace(".img",""),out id))return null;
            }
            return null;
        }
        public static string[] Stances(INxFile npcs,string path) =>
            (path==null?Enumerable.Empty<INxNode>():npcs?.GetNode(path)?.Children ?? Enumerable.Empty<INxNode>())
            .Where(n=>n.Name!="info" && n["0"]!=null)
            .OrderBy(n=>n.Name=="stand"?0:1).ThenBy(n=>n.Name,System.StringComparer.Ordinal).Select(n=>n.Name).ToArray();
    }
}
