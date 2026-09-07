using System;
using System.Collections.Generic;
using System.Linq;
using GameData;
using MapleClient.GameLogic.Data;
using UnityEngine;

namespace MapleClient.GameData
{
    /// <summary>Load bounds/timing with item metadata; decode each effect bitmap only when drawn.</summary>
    internal static class NxWeaponAfterimages
    {
        private static int Int(INxNode node, string key, int fallback = 0) => node?[key]?.GetValue<int>() ?? fallback;
        public static WeaponAfterimage Read(NXDataManager manager, string name, int requiredLevel, string stance)
        {
            if (string.IsNullOrEmpty(name)) return null;
            string path = $"Afterimage/{name}.img/{requiredLevel / 10}/{stance}";
            var source = manager.GetNode("character", path);
            if (source == null) return null;
            var lt = source["lt"]?.GetValue<Vector2>(); var rb = source["rb"]?.GetValue<Vector2>();
            var result = new WeaponAfterimage { Path = path, HasBounds = lt.HasValue && rb.HasValue,
                Bounds = new AttackBounds((int)(lt?.x ?? 0), (int)(lt?.y ?? 0), (int)(rb?.x ?? 0), (int)(rb?.y ?? 0)) };
            // Afterimage.cpp retains the last numeric child in source enumeration order.
            INxNode animation = null;
            foreach (var child in source.Children)
                if (int.TryParse(child.Name, out int frame) && frame >= 0 && frame < 255)
                { result.FirstFrame = frame; animation = child; }
            if (animation == null) return result;
            string animationPath = path + "/" + animation.Name;
            result.Zigzag = Int(animation, "zigzag") != 0;
            var frames = new List<AfterimageFrame>();
            var nodes = animation.Children.Where(n => int.TryParse(n.Name, out int i) && i >= 0).OrderBy(n => int.Parse(n.Name)).ToArray();
            // RealNxNode.Value can resolve a container's first bitmap. Check numeric
            // children before the direct-frame form, without decoding Value here.
            if (nodes.Length == 0 && animation["origin"] != null) nodes = new[] { animation };
            foreach (var frame in nodes)
            {
                int? a0 = frame["a0"]?.GetValue<int>(), a1 = frame["a1"]?.GetValue<int>();
                int? z0 = frame["z0"]?.GetValue<int>(), z1 = frame["z1"]?.GetValue<int>();
                frames.Add(new AfterimageFrame {
                    Path = ReferenceEquals(frame, animation) ? animationPath : animationPath + "/" + frame.Name,
                    DelayMilliseconds = Math.Max(1, Int(frame, "delay", 100) == 0 ? 100 : Int(frame, "delay", 100)),
                    StartAlpha = (a0 ?? (a1.HasValue ? 255 - a1.Value : 255)) / 255f,
                    EndAlpha = (a1 ?? (a0.HasValue ? 255 - a0.Value : 255)) / 255f,
                    StartScale = (z0 ?? 100) / 100f, EndScale = (z1 ?? (z0.HasValue ? 0 : 100)) / 100f
                });
            }
            result.Frames = frames.ToArray();
            return result;
        }
    }

    public static class NxAfterimageSprites
    {
        public static Sprite Load(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            var loader = NXAssetLoader.Instance;
            var file = loader.GetNxFile("character");
            var source = file?.GetNode(path);
            if (source == null) return null;
            var sprite = SpriteLoader.LoadSpriteWithShift(loader.ResolveEquipmentNode(source, file), Vector2.zero, "afterimage/" + path);
            if (sprite != null) sprite.name = path;
            return sprite;
        }
    }
}
