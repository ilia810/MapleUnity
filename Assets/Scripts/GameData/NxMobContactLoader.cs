using System.Collections.Generic;
using System.Linq;
using MapleClient.GameLogic;
using GameData;
using Vector2 = UnityEngine.Vector2;

namespace MapleClient.GameData
{
    /// <summary>Read only collision metadata; no bitmap decoding or dependency on scene views.</summary>
    internal static class NxMobContactLoader
    {
        public static Dictionary<string, MobContactAnimation> Load(NXDataManager manager, int monsterId)
        {
            var result = new Dictionary<string, MobContactAnimation>();
            var visited = new HashSet<int>();
            INxNode source = null;
            while (visited.Add(monsterId))
            {
                source = manager.GetNode("mob", $"{monsterId:D7}.img");
                if (source == null) return result;
                string link = source["info"]?["link"]?.GetValue<string>();
                if (!int.TryParse(link, out int target) || manager.GetNode("mob", $"{target:D7}.img") == null) break;
                if (visited.Contains(target)) return result;
                monsterId = target;
            }
            foreach (string action in new[] { "stand", "move", "fly", "hit1" })
            {
                var node = source?[action];
                if (node == null) continue;
                var frames = new List<MobContactFrame>();
                foreach (var frame in node.Children.Where(n => int.TryParse(n.Name, out int i) && i >= 0).OrderBy(n => int.Parse(n.Name)))
                {
                    var lt = frame["lt"]?.GetValue<Vector2>() ?? Vector2.zero;
                    var rb = frame["rb"]?.GetValue<Vector2>() ?? Vector2.zero;
                    var head = frame["head"]?.GetValue<Vector2>() ?? Vector2.zero;
                    int delay = frame["delay"]?.GetValue<int>() ?? 100;
                    frames.Add(new MobContactFrame { Left = (int)lt.x, Top = (int)lt.y,
                        Right = (int)rb.x, Bottom = (int)rb.y, HeadX = (int)head.x, HeadY = (int)head.y, DelayMilliseconds = delay > 0 ? delay : 100 });
                }
                if (frames.Count > 0) result[action] = new MobContactAnimation(frames.ToArray(),
                    (node["zigzag"]?.GetValue<int>() ?? 0) != 0);
            }
            return result;
        }
    }
}
