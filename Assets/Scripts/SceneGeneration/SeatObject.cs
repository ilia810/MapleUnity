using System.Collections.Generic;
using UnityEngine;

namespace MapleClient.SceneGeneration
{
    public class SeatObject : MonoBehaviour
    {
        private bool isOccupied = false;
        
        private void OnTriggerStay2D(Collider2D other)
        {
            if (other.CompareTag("Player") && !isOccupied && Input.GetKeyDown(KeyCode.DownArrow))
            {
                // TODO: Make player sit
                isOccupied = true;
                Debug.Log("Player sits down");
            }
        }
    }
}
