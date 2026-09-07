using System.Collections.Generic;
using UnityEngine;
using MapleClient.GameView;

namespace MapleClient.SceneGeneration
{
    public class NPCBehavior : MonoBehaviour
    {
        public string npcId;
        public int footholdId;
        public int facingDirection = 1;
        
        
        private void Start()
        {
            // Load NPC data and sprite
            LoadNPCData();
            if (int.TryParse(npcId, out int id))
            {
                (GetComponent<WorldLabelAnchor>() ?? gameObject.AddComponent<WorldLabelAnchor>()).BindNpc(id);
                (GetComponent<ClassicNpcAnimator>() ?? gameObject.AddComponent<ClassicNpcAnimator>()).Bind(id,GetComponentInChildren<SpriteRenderer>());
            }
            ApplyStageOrder(GetComponentInParent<FootholdManager>());
        }

        public void ApplyStageOrder(FootholdManager footholds)
        {
            int layer = footholds?.GetFootholdById(footholdId)?.Layer ?? 0;
            foreach (var renderer in GetComponentsInChildren<SpriteRenderer>(true))
            {
                renderer.sortingLayerName = StageRenderOrder.SortingLayerName;
                renderer.sortingOrder = StageRenderOrder.NpcOrder(layer);
            }
        }
        
        private void LoadNPCData()
        {
            // Saved scenes can contain an empty sprite from an older importer.
            var renderer = GetComponentInChildren<SpriteRenderer>();
            if (renderer != null && renderer.sprite == null)
                LifeSpawnGenerator.LoadNPCSprite(npcId, renderer);
        }
        
        // Interaction is handled by ClassicNpcDialogue using source NPC identity and world proximity.
    }
}
