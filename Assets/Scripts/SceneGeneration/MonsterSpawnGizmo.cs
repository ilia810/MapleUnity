using System.Collections.Generic;
using UnityEngine;

namespace MapleClient.SceneGeneration
{
    public class MonsterSpawnGizmo : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            var spawn = GetComponent<MonsterSpawnPoint>();
            if (spawn == null) return;
            
            // Draw spawn point
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
            
            // Draw spawn area if exists
            if (spawn.hasSpawnArea)
            {
                Gizmos.color = new Color(1f, 0, 0, 0.3f);
                Vector3 min = new Vector3(spawn.spawnAreaMin, transform.position.y - 0.5f, transform.position.z);
                Vector3 max = new Vector3(spawn.spawnAreaMax, transform.position.y + 0.5f, transform.position.z);
                Vector3 size = max - min;
                Vector3 center = (min + max) * 0.5f;
                Gizmos.DrawCube(center, size);
            }
            
            // Draw facing direction
            Gizmos.color = Color.yellow;
            Vector3 dir = spawn.facingDirection > 0 ? Vector3.right : Vector3.left;
            Gizmos.DrawLine(transform.position, transform.position + dir * 0.5f);
        }
    }
}
