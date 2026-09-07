using MapleClient.GameLogic.Core;
using MapleClient.GameView;
using UnityEngine;

namespace MapleClient.SceneGeneration
{
    [DefaultExecutionOrder(999)]
    public class DynamicBackgroundManager : MonoBehaviour
    {
        public Bounds vrBounds { get; set; }
        public long Ticks { get; private set; }
        public long Revision { get; private set; }
        public float Interpolation { get; private set; }
        private GameManager game;
        private MapInfo map;

        private void Start() { map = GetComponentInParent<MapInfo>(); }
        private void LateUpdate()
        {
            if (game == null) game = FindFirstObjectByType<GameManager>();
            Synchronize(game?.World);
        }

        public void Synchronize(GameWorld world)
        {
            if (world == null || (map != null && map.mapId != world.CurrentMapId)) return;
            Ticks = world.MapSimulationTicks;
            Revision = world.MapRevision;
            Interpolation = world.GetPhysicsInterpolationFactor();
        }
    }
}
