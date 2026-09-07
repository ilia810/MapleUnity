using System.Collections.Generic;
using System.Linq;
using MapleClient.GameData;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Data;
using UnityEngine;

namespace MapleClient.GameView
{
    /// <summary>Render the world-owned bullet clock; rendering never advances damage or travel.</summary>
    public sealed class SkillProjectileView : MonoBehaviour
    {
        private GameWorld world;
        private readonly Dictionary<SkillProjectile, SpriteRenderer> sprites = new Dictionary<SkillProjectile, SpriteRenderer>();
        public void Bind(GameWorld value) { world = value; }
        private void LateUpdate()
        {
            if (world == null) return;
            var live = new HashSet<SkillProjectile>(world.Projectiles);
            foreach (var pair in sprites.ToArray())
                if (!live.Contains(pair.Key)) { Destroy(pair.Value.gameObject); sprites.Remove(pair.Key); }
            foreach (var bullet in live)
            {
                if (!sprites.TryGetValue(bullet, out var sprite))
                {
                    sprite = new GameObject("SkillProjectile").AddComponent<SpriteRenderer>();
                    sprite.transform.SetParent(transform, false);
                    // Stage draws combat after every actor layer and before foreground scenery.
                    sprite.sortingLayerName = StageRenderOrder.SortingLayerName; sprite.sortingOrder = 8500;
                    sprites.Add(bullet, sprite);
                }
                var frame = bullet.Sample(out float fraction);
                sprite.sprite = frame == null ? null : MapleClient.GameData.SkillSprites.Frame(bullet.AssetFile, frame.Path);
                var position = bullet.InterpolatedPosition(world.GetPhysicsInterpolationFactor());
                sprite.transform.position = new Vector3(position.X, position.Y, 0);
                sprite.flipX = bullet.FacingRight;
                if (frame == null) continue;
                sprite.color = new Color(1, 1, 1, Mathf.Lerp(frame.StartAlpha, frame.EndAlpha, fraction));
                sprite.transform.localScale = Vector3.one * Mathf.Max(0, Mathf.Lerp(frame.StartScale, frame.EndScale, fraction));
            }
        }
    }
}
