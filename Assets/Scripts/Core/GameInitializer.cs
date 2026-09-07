using UnityEngine;
using MapleClient.GameView;
using MapleClient.SceneGeneration;

namespace MapleClient.Core
{
    /// <summary>Connects the view layer to map generation at application startup.</summary>
    public class GameInitializer : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void InitializeOnLoad()
        {
            GameManager.MapSceneFactory = mapId =>
            {
                var host = new GameObject("MapSceneGenerator");
                var generator = host.AddComponent<MapSceneGenerator>();
                try
                {
                    return generator.GenerateMapScene(mapId);
                }
                finally
                {
                    Object.Destroy(host);
                }
            };
        }
    }
}