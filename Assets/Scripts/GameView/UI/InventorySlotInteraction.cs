using UnityEngine;
using UnityEngine.EventSystems;

namespace MapleClient.GameView.UI
{
    /// <summary>Drag/drop routing for one original bag cell, including empty destinations.</summary>
    public sealed class InventorySlotInteraction : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        private InventoryView owner;
        public int Category { get; private set; }
        public int Slot { get; private set; }
        public int ItemId { get; private set; }
        public void Initialize(InventoryView view, int category, int slot, int itemId)
        { owner=view; Category=category; Slot=slot; ItemId=itemId; }
        public bool TryGetDraggedItem(out int id)
        { id = 0; return owner != null && owner.TryGetDraggedItem(this, out id); }
        public void OnBeginDrag(PointerEventData e)
        {
            if(e.button==PointerEventData.InputButton.Left && owner.BeginItemDrag(this,e)) e.eligibleForClick=false;
        }
        public void OnDrag(PointerEventData e) { if(e.button==PointerEventData.InputButton.Left)owner.DragItem(e); }
        public void OnDrop(PointerEventData e)
        { if(e.button==PointerEventData.InputButton.Left)owner.DropItem(this,e.pointerDrag!=null?e.pointerDrag.GetComponent<InventorySlotInteraction>():null); }
        public void OnEndDrag(PointerEventData e) => owner.CancelItemDrag(this);
        private void OnDisable() { if(owner!=null)owner.CancelItemDrag(this); }
    }
}
