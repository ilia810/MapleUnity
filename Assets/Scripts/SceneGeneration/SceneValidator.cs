using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MapleClient.SceneGeneration
{
    /// <summary>
    /// Validates generated scenes for completeness and correctness
    /// </summary>
    public class SceneValidator : MonoBehaviour
    {
        [Header("Validation Results")]
        public ValidationReport lastReport;
        
        [Header("Debug Visualization")]
        public bool showFootholds = true;
        public bool showPortals = true;
        public bool showSpawns = true;
        public bool showBounds = true;
        
        public ValidationReport ValidateScene(GameObject mapRoot)
        {
            ValidationReport report = new ValidationReport();
            report.mapName = mapRoot.name;
            
            // Get map info
            MapInfo info = mapRoot.GetComponent<MapInfo>();
            if (info == null)
            {
                report.errors.Add("Missing MapInfo component on root");
            }
            else
            {
                report.mapId = info.mapId;
            }
            
            // Validate footholds
            ValidateFootholds(mapRoot, report);
            
            // Validate portals
            ValidatePortals(mapRoot, report);
            
            // Validate NPCs
            ValidateNPCs(mapRoot, report);
            
            // Validate spawns
            ValidateSpawns(mapRoot, report);
            
            // Validate backgrounds
            ValidateBackgrounds(mapRoot, report);
            
            // Validate objects
            ValidateObjects(mapRoot, report);
            
            lastReport = report;
            return report;
        }
        
        private void ValidateFootholds(GameObject mapRoot, ValidationReport report)
        {
            var footholds = mapRoot.GetComponentsInChildren<FootholdData>();
            report.footholdCount = footholds.Length;
            
            if (footholds.Length == 0)
            {
                report.warnings.Add("No footholds found - map may not have walkable platforms");
            }
            
            // Check for disconnected footholds
            HashSet<int> connectedIds = new HashSet<int>();
            foreach (var fh in footholds)
            {
                connectedIds.Add(fh.footholdId);
            }
            
            foreach (var fh in footholds)
            {
                if (fh.nextId != 0 && !connectedIds.Contains(fh.nextId))
                {
                    report.warnings.Add($"Foothold {fh.footholdId} references missing next foothold {fh.nextId}");
                }
                if (fh.prevId != 0 && !connectedIds.Contains(fh.prevId))
                {
                    report.warnings.Add($"Foothold {fh.footholdId} references missing prev foothold {fh.prevId}");
                }
            }
        }
        
        private void ValidatePortals(GameObject mapRoot, ValidationReport report)
        {
            var portals = mapRoot.GetComponentsInChildren<PortalBehavior>();
            report.portalCount = portals.Length;
            
            bool hasSpawnPoint = false;
            foreach (var portal in portals)
            {
                if (portal.portalType == PortalGenerator.PortalType.StartPoint)
                {
                    hasSpawnPoint = true;
                    break;
                }
            }
            
            if (!hasSpawnPoint)
            {
                report.errors.Add("No spawn point portal found - players cannot spawn in this map");
            }
        }
        
        private void ValidateNPCs(GameObject mapRoot, ValidationReport report)
        {
            var npcs = mapRoot.GetComponentsInChildren<NPCBehavior>();
            report.npcCount = npcs.Length;
            
            foreach (var npc in npcs)
            {
                if (string.IsNullOrEmpty(npc.npcId))
                {
                    report.errors.Add($"NPC at {npc.transform.position} has no ID");
                }
            }
        }
        
        private void ValidateSpawns(GameObject mapRoot, ValidationReport report)
        {
            var spawns = mapRoot.GetComponentsInChildren<MonsterSpawnPoint>();
            report.spawnCount = spawns.Length;
            
            foreach (var spawn in spawns)
            {
                if (string.IsNullOrEmpty(spawn.monsterId))
                {
                    report.errors.Add($"Monster spawn at {spawn.transform.position} has no monster ID");
                }
                
                if (spawn.spawnTime <= 0)
                {
                    report.warnings.Add($"Monster spawn {spawn.monsterId} has invalid spawn time: {spawn.spawnTime}");
                }
            }
        }
        
        private void ValidateBackgrounds(GameObject mapRoot, ValidationReport report)
        {
            // Check for the new viewport-based background system
            var bgManager = mapRoot.GetComponentInChildren<DynamicBackgroundManager>();
            if (bgManager == null)
            {
                report.warnings.Add("No DynamicBackgroundManager found - backgrounds may not render");
                report.backgroundCount = 0;
                return;
            }
            
            var backgrounds = mapRoot.GetComponentsInChildren<ViewportBackgroundLayer>();
            report.backgroundCount = backgrounds.Length;
            
            if (backgrounds.Length == 0)
            {
                report.warnings.Add("No background layers found - map may appear empty");
            }
            else
            {
                // Check for Type 3 backgrounds (should have at least one for sky/base color)
                bool hasFullScreenBg = false;
                foreach (var bg in backgrounds)
                {
                    if (bg.backgroundData != null && bg.backgroundData.Type == 3)
                    {
                        hasFullScreenBg = true;
                        break;
                    }
                }
                
                if (!hasFullScreenBg)
                {
                    report.warnings.Add("No Type 3 (full screen) background found - map may lack base color/sky");
                }
            }
        }
        
        private void ValidateObjects(GameObject mapRoot, ValidationReport report)
        {
            var objects = mapRoot.GetComponentsInChildren<MapObject>();
            report.objectCount = objects.Length;
        }
        
        #if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (lastReport == null) return;
            
            // Draw validation visualization
            if (showBounds && lastReport.mapId != 0)
            {
                GameObject mapRoot = GameObject.Find($"Map_{lastReport.mapId}");
                if (mapRoot != null)
                {
                    MapInfo info = mapRoot.GetComponent<MapInfo>();
                    if (info != null)
                    {
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawWireCube(info.vrBounds.center, info.vrBounds.size);
                    }
                }
            }
        }
        #endif
        
        [System.Serializable]
        public class ValidationReport
        {
            public string mapName;
            public int mapId;
            public int footholdCount;
            public int portalCount;
            public int npcCount;
            public int spawnCount;
            public int backgroundCount;
            public int objectCount;
            public List<string> errors = new List<string>();
            public List<string> warnings = new List<string>();
            
            public bool IsValid => errors.Count == 0;
            
            public void PrintReport()
            {
                Debug.Log($"=== Validation Report for {mapName} ===");
                Debug.Log($"Map ID: {mapId}");
                Debug.Log($"Footholds: {footholdCount}");
                Debug.Log($"Portals: {portalCount}");
                Debug.Log($"NPCs: {npcCount}");
                Debug.Log($"Monster Spawns: {spawnCount}");
                Debug.Log($"Backgrounds: {backgroundCount}");
                Debug.Log($"Objects: {objectCount}");
                
                if (errors.Count > 0)
                {
                    Debug.LogError($"Errors ({errors.Count}):");
                    foreach (var error in errors)
                    {
                        Debug.LogError($"  - {error}");
                    }
                }
                
                if (warnings.Count > 0)
                {
                    Debug.LogWarning($"Warnings ({warnings.Count}):");
                    foreach (var warning in warnings)
                    {
                        Debug.LogWarning($"  - {warning}");
                    }
                }
                
                if (IsValid)
                {
                    Debug.Log("Validation PASSED!");
                }
                else
                {
                    Debug.LogError("Validation FAILED!");
                }
            }
        }
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// Custom inspector for SceneValidator
    /// </summary>
    [CustomEditor(typeof(SceneValidator))]
    public class SceneValidatorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            SceneValidator validator = (SceneValidator)target;
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Validate Current Scene"))
            {
                // Find map root
                GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
                GameObject mapRoot = null;
                
                foreach (var root in roots)
                {
                    if (root.name.StartsWith("Map_"))
                    {
                        mapRoot = root;
                        break;
                    }
                }
                
                if (mapRoot != null)
                {
                    var report = validator.ValidateScene(mapRoot);
                    report.PrintReport();
                }
                else
                {
                    Debug.LogError("No map root found in scene (should start with 'Map_')");
                }
            }
            
            // Show last report
            if (validator.lastReport != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Last Validation Report", EditorStyles.boldLabel);
                
                var report = validator.lastReport;
                EditorGUILayout.LabelField($"Map: {report.mapName} (ID: {report.mapId})");
                EditorGUILayout.LabelField($"Elements: {report.footholdCount} footholds, {report.portalCount} portals");
                EditorGUILayout.LabelField($"Life: {report.npcCount} NPCs, {report.spawnCount} spawns");
                EditorGUILayout.LabelField($"Visuals: {report.backgroundCount} backgrounds, {report.objectCount} objects");
                
                if (report.errors.Count > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField($"Errors ({report.errors.Count}):", EditorStyles.boldLabel);
                    foreach (var error in report.errors)
                    {
                        EditorGUILayout.HelpBox(error, MessageType.Error);
                    }
                }
                
                if (report.warnings.Count > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField($"Warnings ({report.warnings.Count}):", EditorStyles.boldLabel);
                    foreach (var warning in report.warnings)
                    {
                        EditorGUILayout.HelpBox(warning, MessageType.Warning);
                    }
                }
            }
        }
    }
    #endif
}