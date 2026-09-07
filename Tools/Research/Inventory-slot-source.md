# Inventory slot actions

Reference: `C:/HeavenClient/MapleStory-Client`, inspected working tree at HEAD `e8746783`.

- `IO/UITypes/UIItemInventory.cpp`, `ItemIcon::drop_on_items`: same-category destinations are addressed by source and destination slot. Cross-category and same-slot drops do not dispatch an action. A bag move sends `MoveItemPacket(tab, source, slot, 1)`; the final quantity and merge decision come back from the server.
- `Net/Packets/InventoryPackets.h`: use requests carry both bag slot and item ID. Equipment requests carry the source bag slot and destination equipment slot. Moving or using an item is not an aggregate operation on every copy of its item ID.
- `Character/Inventory/Inventory.cpp`, `swap`, `change_count`, `modify`: the response swaps slot records or applies quantities/removals to individual slots. Internal moves preserve each category's slot address space. The source also has unique equipment records; Unity still stores unrolled equipment copies by slot and item ID.
- `Character/Inventory/Inventory.cpp`, `recalc_stats`: ordered USE slots choose the first nonempty compatible ammunition stack. Reordering the bag therefore changes ammunition priority.

## Unity offline resolution

The offline client resolves requests locally: move into an empty slot, swap different items/equipment copies, or fill a matching non-equipment stack to its NX `slotMax`, leaving overflow in the source. A full target rejects a merge. These merge rules supply the server's role; they are not an inferred client-side C++ algorithm. Storage's existing stack limits and ammunition model remain in effect.

Bag moves are accepted only in local play while alive and between attacks. An inventory revision guards a drag against pickups, consumption, restores, or other changes while held. Movement emits one inventory-change notification without generating item-added/removed events. Clicking use/equip or a shop sale targets its actual category/slot and expected item ID. Failed slot exchanges preflight additions before committing, including full equipment bags and multiple displaced pieces.

All 24 authored bag cells receive drag/drop targets, including empty cells. The existing artwork and layout are retained. Dropping outside the bag, closing/switching windows, changing the bag, or travelling cancels the drag. Ground disposal, stack splitting, bag-to-equipment dragging, equipment instance rolls, and rechargeable-ammunition lifecycle remain separate work.

The existing version-1 save already contains category item IDs, slots and quantities, so reordered bags require no schema change. Focused integration checks exercise duplicate stacks/copies, full bags, merge overflow, stale addresses, ammunition ordering and save restoration. Scene checks route real UI raycasts at 640x480 and exercise dragging, exact-stack use/equip/sell, interrupted drags, and a disk save loaded into a fresh scene.
