using UnityEngine;

namespace MapleClient.GameData
{
    /// <summary>
    /// Container for sprite and its origin data
    /// </summary>
    public class SpriteWithOrigin
    {
        public Sprite Sprite { get; set; }
        public Vector2 Origin { get; set; }
        
        public SpriteWithOrigin()
        {
            Sprite = null;
            Origin = Vector2.zero;
        }
        
        public SpriteWithOrigin(Sprite sprite, Vector2 origin)
        {
            Sprite = sprite;
            Origin = origin;
        }
    }
}