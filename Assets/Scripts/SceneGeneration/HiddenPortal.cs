using System.Collections.Generic;
using UnityEngine;

namespace MapleClient.SceneGeneration
{
    public class HiddenPortal : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                // TODO: Activate hidden portal
                Debug.Log("Hidden portal activated!");
            }
        }
    }
}
