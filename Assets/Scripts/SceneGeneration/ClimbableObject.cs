using System.Collections.Generic;
using UnityEngine;

namespace MapleClient.SceneGeneration
{
    public class ClimbableObject : MonoBehaviour
    {
        public bool isLadder = true;
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                // TODO: Enable climbing
                Debug.Log($"Player can climb {(isLadder ? "ladder" : "rope")}");
            }
        }
        
        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                // TODO: Disable climbing
            }
        }
    }
}
