using UnityEngine;
using MapleClient.Utilities;

namespace MapleClient.Core
{
    /// <summary>
    /// Initializes core game systems at startup
    /// </summary>
    public class GameInitializer : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void InitializeOnLoad()
        {
            // Create debug log capture system
            GameObject debugLogObj = new GameObject("DebugLogCapture");
            debugLogObj.AddComponent<DebugLogCapture>();
            
            Debug.Log("Game systems initialized - Debug log capture enabled");
        }
    }
}