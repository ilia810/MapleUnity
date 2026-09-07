using System.Collections.Generic;
using System.Linq;
using GameData;
using MapleClient.GameLogic.Data;
using UnityEngine;

namespace MapleClient.GameData
{
    public sealed class NxBackgroundAnimation
    {
        public Sprite[] Sprites { get; private set; }
        public SourceAnimationFrame[] Frames { get; private set; }
        public bool Zigzag { get; private set; }

        public static NxBackgroundAnimation Load(NXDataManager manager, string name, int number, bool animated)
        {
            string path = $"Back/{name}.img/{(animated ? "ani" : "back")}/{number}";
            var node = manager.GetNode("map", path);
            if (node == null) return null;
            var sprites = new List<Sprite>();
            var frames = new List<SourceAnimationFrame>();
            // Inspect numbered children first: RealNxNode.Value also exposes a container's first bitmap.
            var numbered = node.Children.Where(n => int.TryParse(n.Name, out int i) && i >= 0).OrderBy(n => int.Parse(n.Name)).ToArray();
            var sources = numbered.Length > 0 ? numbered : new[] { node };
            foreach (var source in sources)
            {
                var bitmap = source.ResolveOutlink(manager);
                if (!(bitmap?.Value is byte[])) continue;
                string key = "background/" + path + (ReferenceEquals(source, node) ? "" : "/" + source.Name);
                var sprite = SpriteLoader.ConvertCharacterNodeToSprite(bitmap, key, SpriteLoader.GetOrigin(bitmap));
                if (sprite == null) continue;
                sprite.name = key;
                int? a0 = source["a0"]?.GetValue<int>(), a1 = source["a1"]?.GetValue<int>();
                int? z0 = source["z0"]?.GetValue<int>(), z1 = source["z1"]?.GetValue<int>();
                frames.Add(new SourceAnimationFrame {
                    DelayMilliseconds = source["delay"]?.GetValue<int>() ?? 100,
                    StartOpacity = a0 ?? (a1.HasValue ? 255 - a1.Value : 255),
                    EndOpacity = a1 ?? (a0.HasValue ? 255 - a0.Value : 255),
                    StartScale = z0 ?? 100, EndScale = z1 ?? (z0.HasValue ? 0 : 100)
                });
                sprites.Add(sprite);
            }
            return sprites.Count == 0 ? null : new NxBackgroundAnimation {
                Sprites = sprites.ToArray(), Frames = frames.ToArray(), Zigzag = (node["zigzag"]?.GetValue<int>() ?? 0) != 0
            };
        }
    }
}
