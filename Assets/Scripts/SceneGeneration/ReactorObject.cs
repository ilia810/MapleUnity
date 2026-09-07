using System.Collections.Generic;
using UnityEngine;

namespace MapleClient.SceneGeneration
{
    public class ReactorObject : MonoBehaviour
    {
        public int reactorId;
        public bool isActivated = false;
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player") && !isActivated)
            {
                // TODO: Activate reactor
                isActivated = true;
                Debug.Log("Reactor activated!");
            }
        }
    }
}
