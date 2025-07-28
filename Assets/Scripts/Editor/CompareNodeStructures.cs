using UnityEngine;
using UnityEditor;
using MapleClient.GameData;
using GameData;
using System.Reflection;

namespace MapleClient.Editor
{
    public class CompareNodeStructures : EditorWindow
    {
        [MenuItem("MapleUnity/Debug/Compare Tile vs Object Structure")]
        public static void ShowWindow()
        {
            GetWindow<CompareNodeStructures>("Compare Structures");
        }
        
        private void OnGUI()
        {
            if (GUILayout.Button("Compare Structures"))
            {
                CompareStructures();
            }
        }
        
        private void CompareStructures()
        {
            Debug.Log("=== COMPARING TILE VS OBJECT STRUCTURES ===");
            
            var nxManager = NXDataManagerSingleton.Instance;
            var dataManager = nxManager.DataManager;
            
            // Working tile
            string tilePath = "Tile/woodMarble.img/edD/1";
            var tileNode = dataManager.GetNode("map", tilePath);
            
            // Broken object
            string objPath = "Obj/guide.img/common/post/0";
            var objNode = dataManager.GetNode("map", objPath);
            
            Debug.Log("\n--- TILE NODE (working) ---");
            if (tileNode != null)
            {
                AnalyzeNode(tileNode, "Tile");
            }
            
            Debug.Log("\n--- OBJECT NODE (broken) ---");
            if (objNode != null)
            {
                AnalyzeNode(objNode, "Object");
            }
        }
        
        private void AnalyzeNode(INxNode node, string label)
        {
            Debug.Log($"{label} node: {node.Name}");
            Debug.Log($"  Has value: {node.Value != null}");
            if (node.Value != null)
            {
                Debug.Log($"  Value type: {node.Value.GetType().Name}");
            }
            
            // Check for origin
            var originChild = node["origin"];
            Debug.Log($"  Has origin child: {originChild != null}");
            if (originChild != null)
            {
                Debug.Log($"    Origin value: {originChild.Value}");
            }
            
            // Get underlying NX node
            if (node is RealNxNode realNode)
            {
                var nxNodeField = realNode.GetType().GetField("nxNode", BindingFlags.NonPublic | BindingFlags.Instance);
                if (nxNodeField != null)
                {
                    var nxNode = nxNodeField.GetValue(realNode);
                    if (nxNode != null)
                    {
                        var nxType = nxNode.GetType();
                        Debug.Log($"  Underlying NX type: {nxType.Name}");
                        
                        // Check if it's enumerable and count children
                        if (nxNode is System.Collections.IEnumerable enumerable)
                        {
                            int count = 0;
                            System.Collections.Generic.List<string> childInfo = new System.Collections.Generic.List<string>();
                            foreach (var child in enumerable)
                            {
                                if (child != null && count < 5)
                                {
                                    var childType = child.GetType();
                                    var nameProp = childType.GetProperty("Name");
                                    if (nameProp != null)
                                    {
                                        var name = nameProp.GetValue(child) as string;
                                        childInfo.Add($"{name} ({childType.Name})");
                                    }
                                }
                                count++;
                            }
                            Debug.Log($"  NX Children ({count} total): {string.Join(", ", childInfo)}");
                        }
                        
                        // Try to access through indexer
                        Debug.Log($"  Attempting direct indexer access:");
                        var indexer = nxType.GetProperty("Item", new System.Type[] { typeof(string) });
                        if (indexer != null)
                        {
                            try
                            {
                                var originViaIndexer = indexer.GetValue(nxNode, new object[] { "origin" });
                                Debug.Log($"    Indexer['origin']: {(originViaIndexer != null ? originViaIndexer.GetType().Name : "null")}");
                            }
                            catch (System.Exception e)
                            {
                                Debug.Log($"    Indexer['origin']: ERROR - {e.GetType().Name}");
                            }
                        }
                    }
                }
            }
        }
    }
}