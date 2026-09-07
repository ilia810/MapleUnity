using System;
using System.Linq;
using MapleClient.GameData;
using UnityEngine;

namespace MapleClient.GameView.UI
{
    /// <summary>Native NX UI/effect frames, including their authored origins and frame delays.</summary>
    public sealed class ClassicSpriteFrames
    {
        public sealed class Frame { public Sprite Sprite; public Vector2 Origin; public int Milliseconds; }
        public Frame[] Frames { get; }
        public int Duration { get; }
        public ClassicSpriteFrames(string file,string path)
        {
            var source=NXAssetLoader.Instance.GetNxFile(file)?.GetNode(path);
            Frames=(source?.Children ?? Enumerable.Empty<INxNode>()).Where(n=>int.TryParse(n.Name,out _)).OrderBy(n=>int.Parse(n.Name))
                .Select(n=>new Frame{Sprite=MapleClient.GameData.SpriteLoader.LoadSprite(n,file+"/"+path+"/"+n.Name),
                    Origin=MapleClient.GameData.SpriteLoader.GetOrigin(n),Milliseconds=Math.Max(1,n["delay"]?.GetValue<int>() ?? 100)})
                .Where(f=>f.Sprite!=null).ToArray();
            Duration=Frames.Sum(f=>f.Milliseconds);
        }
        public Frame Sample(double seconds)
        {
            if(Frames.Length==0)return null;
            double time=Math.Max(0,seconds*1000)%Duration;
            foreach(var frame in Frames){if(time<frame.Milliseconds)return frame;time-=frame.Milliseconds;}
            return Frames[0];
        }
        public static Vector2 CanvasPoint(RectTransform root,Canvas canvas,Vector2 screen)
        {
            var pixels=canvas.pixelRect;
            return new Vector2(root.rect.xMin+(screen.x-pixels.xMin)/Mathf.Max(1,pixels.width)*root.rect.width,
                root.rect.yMin+(screen.y-pixels.yMin)/Mathf.Max(1,pixels.height)*root.rect.height);
        }
    }
}
