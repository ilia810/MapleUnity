using System.Collections.Generic;
using UnityEngine;

namespace MapleClient.SceneGeneration
{
    public class PortalBehavior : MonoBehaviour
    {
        public int portalId;
        public string portalName;
        public PortalGenerator.PortalType portalType;
        public int targetMapId;
        public string targetPortalName;
        public bool isSpawnPoint = false;
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                HandlePortalEnter(other.gameObject);
            }
        }
        
        private void HandlePortalEnter(GameObject player)
        {
            // Handle different portal types
            switch (portalType)
            {
                case PortalGenerator.PortalType.Visible:
                case PortalGenerator.PortalType.Invisible:
                case PortalGenerator.PortalType.Changable:
                case PortalGenerator.PortalType.ChangableInvisible:
                    // Teleport to target map
                    Debug.Log($"Portal {portalName} activated - Target: Map {targetMapId}, Portal {targetPortalName}");
                    // TODO: Implement map transition
                    break;
                    
                case PortalGenerator.PortalType.Script:
                case PortalGenerator.PortalType.ScriptInvisible:
                case PortalGenerator.PortalType.ScriptHidden:
                    // Execute portal script
                    var scriptHandler = GetComponent<ScriptPortalHandler>();
                    if (scriptHandler != null)
                    {
                        scriptHandler.ExecuteScript(player);
                    }
                    break;
            }
        }
    }
}
