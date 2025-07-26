using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using MapleClient.GameData;
using GameData;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace MapleClient.SceneGeneration
{
    /// <summary>
    /// Main controller for generating Unity scenes from MapleStory map data
    /// </summary>
    public class MapSceneGenerator : MonoBehaviour
    {
        private MapDataExtractor dataExtractor;
        private FootholdGenerator footholdGen;
        private PortalGenerator portalGen;
        private LifeSpawnGenerator lifeGen;
        private BackgroundGenerator bgGen;
        private ObjectGenerator objGen;
        private TileGenerator tileGen;
        
        [Header("Generation Settings")]
        public bool generateInPlayMode = false;
        public int testMapId = 100000000; // Henesys
        
        private void Awake()
        {
            InitializeGenerators();
        }
        
        public void InitializeGenerators()
        {
            dataExtractor = new MapDataExtractor();
            footholdGen = new FootholdGenerator();
            portalGen = new PortalGenerator();
            lifeGen = new LifeSpawnGenerator();
            bgGen = new BackgroundGenerator();
            objGen = new ObjectGenerator();
            tileGen = new TileGenerator();
        }
        
        /// <summary>
        /// Generate a scene for a specific map ID
        /// </summary>
        public GameObject GenerateMapScene(int mapId)
        {
            Debug.Log($"Generating scene for map ID: {mapId}");
            
            // Extract map data
            MapData mapData = dataExtractor.ExtractMapData(mapId);
            if (mapData == null)
            {
                Debug.LogError($"Failed to extract data for map ID: {mapId}");
                return null;
            }
            
            // Create root GameObject
            GameObject mapRoot = new GameObject($"Map_{mapId}");
            
            // Add FootholdManager component and initialize it
            FootholdManager footholdManager = mapRoot.AddComponent<FootholdManager>();
            footholdManager.Initialize(mapData.Footholds ?? new List<Foothold>());
            
            // Generate backgrounds
            if (mapData.Backgrounds != null && mapData.Backgrounds.Count > 0)
            {
                bgGen.GenerateBackgrounds(mapData.Backgrounds, mapRoot.transform, mapData.VRBounds);
            }
            
            // Generate tiles (ground tiles)
            if (mapData.Tiles != null && mapData.Tiles.Count > 0)
            {
                tileGen.GenerateTiles(mapData.Tiles, mapRoot.transform);
            }
            
            // Generate footholds (platforms)
            if (mapData.Footholds != null && mapData.Footholds.Count > 0)
            {
                footholdGen.GenerateFootholds(mapData.Footholds, mapRoot.transform);
            }
            
            // Generate portals
            if (mapData.Portals != null && mapData.Portals.Count > 0)
            {
                portalGen.GeneratePortals(mapData.Portals, mapRoot.transform);
            }
            
            // Generate NPCs
            if (mapData.NPCs != null && mapData.NPCs.Count > 0)
            {
                lifeGen.GenerateNPCs(mapData.NPCs, mapRoot.transform);
            }
            
            // Generate monster spawns
            if (mapData.Monsters != null && mapData.Monsters.Count > 0)
            {
                lifeGen.GenerateMonsterSpawns(mapData.Monsters, mapRoot.transform);
            }
            
            // Generate objects
            if (mapData.Objects != null && mapData.Objects.Count > 0)
            {
                objGen.GenerateObjects(mapData.Objects, mapRoot.transform);
            }
            
            // Add map info component
            MapInfo info = mapRoot.AddComponent<MapInfo>();
            info.mapId = mapId;
            info.bgm = mapData.BGM;
            info.returnMap = mapData.ReturnMap;
            info.forcedReturn = mapData.ForcedReturn;
            info.fieldLimit = mapData.FieldLimit;
            info.vrBounds = mapData.VRBounds;
            
            // Setup camera bounds
            SetupCameraBounds(mapData.VRBounds);
            
            Debug.Log($"Scene generation complete for map {mapId}");
            return mapRoot;
        }
        
        private void SetupCameraBounds(Bounds bounds)
        {
            // Find or create camera bounds controller
            GameObject camBounds = GameObject.Find("CameraBounds");
            if (camBounds == null)
            {
                camBounds = new GameObject("CameraBounds");
            }
            
            CameraBounds boundsComponent = camBounds.GetComponent<CameraBounds>();
            if (boundsComponent == null)
            {
                boundsComponent = camBounds.AddComponent<CameraBounds>();
            }
            
            boundsComponent.SetBounds(bounds);
        }
        
        #if UNITY_EDITOR
        /// <summary>
        /// Editor menu to generate scenes
        /// </summary>
        [MenuItem("MapleUnity/Generate Map Scene")]
        public static void ShowGenerateDialog()
        {
            MapSceneGeneratorWindow.ShowWindow();
        }
        #endif
        
        // Test generation in play mode
        private void Start()
        {
            if (generateInPlayMode)
            {
                StartCoroutine(TestGeneration());
            }
        }
        
        private IEnumerator TestGeneration()
        {
            yield return new WaitForSeconds(1f);
            
            // Clear existing map
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
            
            // Generate test map
            GameObject map = GenerateMapScene(testMapId);
            if (map != null)
            {
                map.transform.parent = transform;
            }
        }
    }
    
    /// <summary>
    /// Component to store map information
    /// </summary>
    public class MapInfo : MonoBehaviour
    {
        public int mapId;
        public string bgm;
        public int returnMap;
        public int forcedReturn;
        public int fieldLimit;
        public Bounds vrBounds;
    }
    
    /// <summary>
    /// Camera bounds controller
    /// </summary>
    public class CameraBounds : MonoBehaviour
    {
        private Bounds bounds;
        private Camera mainCamera;
        
        private void Start()
        {
            mainCamera = Camera.main;
        }
        
        public void SetBounds(Bounds newBounds)
        {
            bounds = newBounds;
        }
        
        private void LateUpdate()
        {
            if (mainCamera == null) return;
            
            // Constrain camera position to bounds
            Vector3 pos = mainCamera.transform.position;
            
            // Calculate camera extents
            float height = mainCamera.orthographicSize * 2;
            float width = height * mainCamera.aspect;
            
            // Clamp position
            pos.x = Mathf.Clamp(pos.x, bounds.min.x + width / 2, bounds.max.x - width / 2);
            pos.y = Mathf.Clamp(pos.y, bounds.min.y + height / 2, bounds.max.y - height / 2);
            
            mainCamera.transform.position = pos;
        }
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// Editor window for generating map scenes
    /// </summary>
    public class MapSceneGeneratorWindow : EditorWindow
    {
        private int mapId = 100000000;
        private bool createNewScene = true;
        private string sceneName = "";
        
        // Common map IDs for quick access
        private readonly Dictionary<string, int> commonMaps = new Dictionary<string, int>
        {
            { "Henesys", 100000000 },
            { "Ellinia", 101000000 },
            { "Perion", 102000000 },
            { "Kerning City", 103000000 },
            { "Lith Harbor", 104000000 },
            { "Sleepywood", 105000000 },
            { "Mushroom Town", 106000000 },
            { "Amherst", 1000000 },
            { "Southperry", 2000000 },
            { "Free Market", 910000000 }
        };
        
        public static void ShowWindow()
        {
            GetWindow<MapSceneGeneratorWindow>("Generate Map Scene");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Map Scene Generator", EditorStyles.boldLabel);
            
            EditorGUILayout.Space();
            
            // Map ID input
            mapId = EditorGUILayout.IntField("Map ID:", mapId);
            
            // Common maps dropdown
            EditorGUILayout.Space();
            GUILayout.Label("Common Maps:");
            
            foreach (var map in commonMaps)
            {
                if (GUILayout.Button($"{map.Key} ({map.Value})"))
                {
                    mapId = map.Value;
                    sceneName = map.Key.Replace(" ", "");
                }
            }
            
            EditorGUILayout.Space();
            
            // Scene options
            createNewScene = EditorGUILayout.Toggle("Create New Scene", createNewScene);
            if (createNewScene)
            {
                sceneName = EditorGUILayout.TextField("Scene Name:", 
                    string.IsNullOrEmpty(sceneName) ? $"Map_{mapId}" : sceneName);
            }
            
            EditorGUILayout.Space();
            
            // Generate button
            if (GUILayout.Button("Generate Scene", GUILayout.Height(30)))
            {
                GenerateScene();
            }
            
            // Batch generation
            EditorGUILayout.Space();
            if (GUILayout.Button("Batch Generate Common Maps"))
            {
                BatchGenerateCommonMaps();
            }
        }
        
        private void GenerateScene()
        {
            if (createNewScene)
            {
                // Create new scene
                Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                newScene.name = sceneName;
            }
            
            // Create generator
            GameObject generatorObj = new GameObject("MapSceneGenerator");
            MapSceneGenerator generator = generatorObj.AddComponent<MapSceneGenerator>();
            generator.InitializeGenerators();
            
            // Generate map
            GameObject map = generator.GenerateMapScene(mapId);
            
            // Clean up generator
            DestroyImmediate(generatorObj);
            
            // Clean up NXDataManager singleton
            NXDataManagerSingleton.Cleanup();
            
            // Mark scene as dirty
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            
            Debug.Log($"Generated scene for map {mapId}");
        }
        
        private void BatchGenerateCommonMaps()
        {
            if (!EditorUtility.DisplayDialog("Batch Generate", 
                "This will generate scenes for all common maps. Continue?", "Yes", "No"))
            {
                return;
            }
            
            foreach (var map in commonMaps)
            {
                // Create new scene
                Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                
                // Create generator
                GameObject generatorObj = new GameObject("MapSceneGenerator");
                MapSceneGenerator generator = generatorObj.AddComponent<MapSceneGenerator>();
                generator.InitializeGenerators();
                
                // Generate map
                GameObject mapObj = generator.GenerateMapScene(map.Value);
                
                // Clean up generator
                DestroyImmediate(generatorObj);
                
                // Save scene
                string scenePath = $"Assets/Scenes/Maps/{map.Key.Replace(" ", "")}.unity";
                EditorSceneManager.SaveScene(newScene, scenePath);
                
                Debug.Log($"Generated and saved scene: {scenePath}");
            }
            
            // Clean up NXDataManager singleton after batch generation
            NXDataManagerSingleton.Cleanup();
            
            Debug.Log("Batch generation complete!");
        }
    }
    #endif
}