using UnityEngine;
using MapleClient.GameLogic.Core;
using MapleClient.GameData;

namespace MapleClient.GameView
{
    public class DroppedItemView : MonoBehaviour
    {
        private DroppedItem droppedItem;
        private SpriteRenderer spriteRenderer;
        public DroppedItem Model => droppedItem;
        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        public void SetDroppedItem(DroppedItem item)
        {
            droppedItem = item;
            spriteRenderer.sortingLayerName = StageRenderOrder.SortingLayerName;
            spriteRenderer.sortingOrder = StageRenderOrder.DropOrder(item.FootholdLayer);
            spriteRenderer.sprite = item.IsMeso ? NXAssetLoader.Instance.LoadMesoIcon(item.Quantity, 0) : NXAssetLoader.Instance.LoadDroppedItemIcon(item.ItemId);
            Update();
        }
        private void Update()
        {
            if (droppedItem == null) return;
            // Source Drop floating motion: 0.025 radians per 8 ms, 5 pixel bob. The model clock pauses with simulation.
            float phase = droppedItem.Age / .008f * .025f;
            float bob = (1 - Mathf.Cos(phase)) * .025f;
            transform.position = new Vector3(droppedItem.Position.X, droppedItem.Position.Y - .01f + bob, 0);
            if (droppedItem.IsMeso) spriteRenderer.sprite = NXAssetLoader.Instance.LoadMesoIcon(droppedItem.Quantity, NXAssetLoader.Instance.MesoFrameAt(droppedItem.Quantity, droppedItem.Age));
            spriteRenderer.color = new Color(1, 1, 1, Mathf.Clamp01(droppedItem.LifeTime));
        }
    }
}
