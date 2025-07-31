using UnityEngine;

using Debug = UnityEngine.Debug;

namespace MapleClient.GameView
{
    /// <summary>
    /// Configures Unity's physics settings to match MapleStory v83's 60 FPS physics.
    /// This component should be added to the GameManager or a persistent object.
    /// </summary>
    [DefaultExecutionOrder(-1000)] // Execute before other scripts
    public class PhysicsConfiguration : MonoBehaviour
    {
        private const float TARGET_FIXED_TIMESTEP = 1f / 60f; // 60 FPS
        private const int MAX_FIXED_TIMESTEPS = 4; // Prevent spiral of death
        
        private void Awake()
        {
            ConfigurePhysicsSettings();
        }
        
        private void ConfigurePhysicsSettings()
        {
            // Set fixed timestep to 60 FPS (0.01667 seconds)
            Time.fixedDeltaTime = TARGET_FIXED_TIMESTEP;
            
            // Set maximum allowed timestep to prevent spiral of death
            // This limits how many fixed updates can run per frame
            Time.maximumDeltaTime = TARGET_FIXED_TIMESTEP * MAX_FIXED_TIMESTEPS;
            
            // Disable Unity's physics system - we use our own
            Physics.autoSimulation = false;
            Physics2D.simulationMode = SimulationMode2D.Script;
            
            // Log configuration
            Debug.Log($"Physics configured for 60 FPS:");
            Debug.Log($"  Fixed Timestep: {Time.fixedDeltaTime:F5} seconds ({1f / Time.fixedDeltaTime:F1} FPS)");
            Debug.Log($"  Maximum Delta Time: {Time.maximumDeltaTime:F3} seconds");
            Debug.Log($"  Unity Physics: Disabled (using custom physics)");
        }
        
        // Verify settings haven't been changed
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                ConfigurePhysicsSettings();
            }
        }
        
#if UNITY_EDITOR
        // Show current physics configuration in inspector
        [Header("Current Physics Settings (Read-Only)")]
        [SerializeField] private float currentFixedTimestep;
        [SerializeField] private float currentMaximumDeltaTime;
        [SerializeField] private float currentTimeScale;
        
        private void Update()
        {
            currentFixedTimestep = Time.fixedDeltaTime;
            currentMaximumDeltaTime = Time.maximumDeltaTime;
            currentTimeScale = Time.timeScale;
        }
#endif
    }
}