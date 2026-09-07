using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MapleClient.GameData;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Data;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using AssetItemType = MapleClient.GameLogic.Interfaces.ItemType;

namespace MapleClient.GameView.UI
{
    public class InventoryView : MonoBehaviour
    {
        private Player player;
        private GameWorld world;
        private Font font;
        private RectTransform panel, gearPanel, bagSurface, gearSurface, context;
        private Text mesos;
        private ClassicTooltipView tooltip;
        private bool dirty=true, expanded;
        private int category=1, selectedId, selectedBagSlot;
        private EquipSlot? selectedSlot;
        private readonly List<GameObject> rows=new List<GameObject>();
        private readonly List<Image> tabLabels=new List<Image>();
        private InventorySlotInteraction dragSource;
        private long dragRevision;
        private RectTransform dragIcon;
        private int Columns=>expanded?10:5;
        public bool BagVisible=>panel!=null&&panel.gameObject.activeSelf;
        public bool EquipmentVisible=>gearPanel!=null&&gearPanel.gameObject.activeSelf;
        public GameObject SelectedRow { get; private set; }

        private IEnumerator Start()
        {
            CreateUI();GameManager manager;
            while((manager=FindFirstObjectByType<GameManager>())==null||manager.World==null)yield return null;
            world=manager.World;world.MapLoaded+=OnMapLoaded;SetPlayer(world.Player);
        }
        public void SetPlayer(Player value)
        { CancelItemDrag();Unbind();player=value;if(player!=null){player.Inventory.Changed+=Changed;player.EquipmentChanged+=Changed;}dirty=true; }
        public void Show(bool visible)
        { ShowBag(visible);if(!visible)ShowEquipment(false); }
        public void ShowBag(bool visible)
        { if(panel==null)return;CancelItemDrag();HideActions();panel.gameObject.SetActive(visible);if(visible)panel.GetComponent<ClassicWindow>().Focus();dirty=true; }
        public void ShowEquipment(bool visible)
        { if(gearPanel==null)return;HideActions();gearPanel.gameObject.SetActive(visible);if(visible)gearPanel.GetComponent<ClassicWindow>().Focus();dirty=true; }
        private void Changed(){CancelItemDrag();HideActions();dirty=true;}
        private void OnMapLoaded(MapleClient.GameLogic.MapData map){CancelItemDrag();HideActions();tooltip.Hide();dirty=true;}
        private void CreateUI()
        {
            font=Font.CreateDynamicFontFromOSFont("Arial",12);tooltip=ClassicTooltipView.For(transform);
            var hud=ClassicUI.Hud(transform);
            ClassicUI.ArtButton("InventoryToggle",hud,"StatusBar.img/InvenKey",646,5,()=>ShowBag(!BagVisible));
            ClassicUI.ArtButton("EquipmentToggle",hud,"StatusBar.img/EquipKey",616,5,()=>ShowEquipment(!EquipmentVisible));
            panel=ClassicUI.Window("InventoryPanel",transform,211,289);
            panel.GetComponent<ClassicWindow>().Configure(new Vector2(.56f,.53f),()=>ShowBag(false));
            CreateBag();
            gearPanel=ClassicUI.Window("EquipmentPanel",transform,175,291);
            gearPanel.GetComponent<ClassicWindow>().Configure(new Vector2(.73f,.51f),()=>ShowEquipment(false));
            gearSurface=ClassicUI.Rect("EquipmentWindow",gearPanel,175,291);ClassicUI.Place(gearSurface,0,0,175,291);
            ClassicUI.Art("EquipmentBackground",gearSurface,"UIWindow.img/Equip/backgrnd",0,0).raycastTarget=true;
            ClassicUI.DragTitle(gearSurface,gearPanel,175);
            ClassicUI.ArtButton("CloseEquipment",gearSurface,"Basic.img/BtClose",158,5,()=>ShowEquipment(false));
            context=ClassicUI.Window("ItemActions",transform,116,25);context.gameObject.SetActive(false);
            context.GetComponent<ClassicWindow>().CloseAction=HideActions;
            context.GetComponent<ClassicWindow>().DismissOnOutsideClick=true;
            panel.gameObject.SetActive(false);gearPanel.gameObject.SetActive(false);
        }
        private void CreateBag()
        {
            if(bagSurface!=null){bagSurface.gameObject.SetActive(false);Destroy(bagSurface.gameObject);}tabLabels.Clear();
            float width=expanded?386:211,height=expanded?184:289;panel.sizeDelta=new Vector2(width,height);
            bagSurface=ClassicWindowSkin.Surface("BagWindow",panel,width,height,font,"ITEM INVENTORY","UIWindow.img/Item/backgrnd");
            ClassicUI.DragTitle(bagSurface,panel,width);
            ClassicUI.ArtButton("ExpandInventory",bagSurface,"UIWindow.img/Item/"+(expanded?"BtSmall":"BtFull"),width-58,5,()=>{expanded=!expanded;HideActions();CreateBag();dirty=true;});
            ClassicUI.ArtButton("GatherInventory",bagSurface,"UIWindow.img/Item/BtGather",width-44,5,()=>Organize(false));
            ClassicUI.ArtButton("SortInventory",bagSurface,"UIWindow.img/Item/BtSort",width-30,5,()=>Organize(true));
            ClassicUI.ArtButton("CloseInventory",bagSurface,"Basic.img/BtClose",width-16,5,()=>ShowBag(false));
            for(int i=0;i<6;i++)
            {
                int c=i+1;var tab=ClassicUI.Button("InventoryCategory_"+c,bagSurface,font,i==5?"Deco":"",()=>{category=c;selectedId=0;HideActions();tooltip.Hide();dirty=true;});
                ClassicUI.Place(tab.GetComponent<RectTransform>(),4+i*33,24,33,19);
                if(i==5){tab.interactable=false;tab.GetComponentInChildren<Text>().fontSize=10;continue;}
                tabLabels.Add(ClassicUI.Art("CategoryLabel",tab.transform,"UIWindow.img/Item/Tab/disabled/"+i,2,3));
            }
            for(int i=0;i<Inventory.SlotsPerCategory;i++)
                ClassicWindowSkin.Fill("Cell",bagSurface,6+(i%Columns)*35,48+(i/Columns)*35,34,34,ClassicWindowSkin.Paper);
            var scroll=ClassicWindowSkin.Scrollbar("InventoryScroll",bagSurface,width-18,49,height-81,null,true);scroll.interactable=false;scroll.handleRect.gameObject.SetActive(false);
            ClassicUI.Region("MesoCoin",bagSurface,"UIWindow.img/Item/backgrnd",new Rect(8,268,11,12),8,height-21,11,12);
            ClassicWindowSkin.Fill("MesoField",bagSurface,24,height-23,width-62,16,Color.white);
            mesos=ClassicWindowSkin.Label("InventoryMesos",bagSurface,font,"0",25,height-23,width-65,17,12);mesos.alignment=TextAnchor.MiddleRight;
            ClassicWindowSkin.Label("MesosLabel",bagSurface,font,"MESOS",width-35,height-21,30,12,8,Color.white,true);
        }
        private void Organize(bool sort)
        {
            if(world==null||!world.IsLocal||player.IsDead||player.IsBasicAttacking)return;
            CancelItemDrag();HideActions();tooltip.Hide();player.Inventory.Organize(category,sort);selectedId=0;dirty=true;
        }
        private void Refresh()
        {
            if(player==null)return;dirty=false;
            foreach(var row in rows){if(row!=null){row.SetActive(false);Destroy(row);}}rows.Clear();SelectedRow=null;
            for(int i=0;i<5;i++)
            { bool active=category==i+1;ClassicUI.NativeCaption(tabLabels[i],ClassicUI.Sprite("UIWindow.img/Item/Tab/"+(active?"enabled/":"disabled/")+i),33,19);ClassicUI.TabState(tabLabels[i].transform.parent,active); }
            if(BagVisible)for(int slot=1;slot<=Inventory.SlotsPerCategory;slot++)
            { var stack=player.Inventory.GetStack(category,slot);CreateRow(stack?.ItemId??0,stack?.Quantity??0,slot,null,bagSurface,new Vector2(7+35*((slot-1)%Columns),49+35*((slot-1)/Columns))); }
            if(EquipmentVisible)foreach(var entry in player.GetEquippedItems())CreateRow(entry.Value,1,0,entry.Key,gearSurface,EquipmentPosition(entry.Key));
        }
        private void CreateRow(int id,int count,int bagSlot,EquipSlot? equip,Transform parent,Vector2 pos)
        {
            string rowName=equip.HasValue?"EquipmentRow_"+id:id==0?$"InventorySlot_{category}_{bagSlot}":"ItemRow_"+id+(rows.Any(r=>r!=null&&r.name=="ItemRow_"+id)?"_Slot_"+bagSlot:"");
            var image=ClassicWindowSkin.Fill(rowName,parent,pos.x,pos.y,32,32,Color.clear,true);var row=image.rectTransform;rows.Add(row.gameObject);
            var button=row.gameObject.AddComponent<Button>();button.onClick.AddListener(()=>Select(row,id,bagSlot,equip));
            if(!equip.HasValue)row.gameObject.AddComponent<InventorySlotInteraction>().Initialize(this,category,bagSlot,id);
            if(id==0)return;
            var icon=ClassicWindowSkin.Fill("Icon",row,0,0,32,32,Color.white);icon.sprite=NXAssetLoader.Instance.LoadItemIcon(id);icon.preserveAspect=true;icon.enabled=icon.sprite!=null;
            if(count>1){var label=ClassicWindowSkin.Label("Count",row,font,count.ToString(),0,18,32,14,10,Color.white);label.alignment=TextAnchor.LowerRight;label.gameObject.AddComponent<ClassicNumberLabel>().Bind(label,"Basic.img/ItemNo");}
            var hover=row.gameObject.AddComponent<ClassicHover>();hover.Enter=e=>{if(dragSource==null&&!context.gameObject.activeSelf)tooltip.ShowItem(row,e.position,player,id,equip.HasValue);};hover.Exit=()=>tooltip.Hide(row);
            var click=row.gameObject.AddComponent<ClassicItemClick>();click.Double=e=>{Select(row,id,bagSlot,equip);Act();};click.Right=e=>{Select(row,id,bagSlot,equip);OpenActions(e.position);};
            if(id==selectedId&&equip==selectedSlot&&(equip.HasValue||bagSlot==selectedBagSlot)){image.color=new Color(.5f,.7f,1,.4f);SelectedRow=row.gameObject;}
        }
        private void Select(RectTransform row,int id,int bagSlot,EquipSlot? equip)
        {
            selectedId=id;selectedBagSlot=bagSlot;selectedSlot=equip;SelectedRow=row.gameObject;HideActions();
            foreach(var r in rows)if(r!=null)r.GetComponent<Image>().color=r==row.gameObject&&id!=0?new Color(.5f,.7f,1,.4f):Color.clear;
            if(id!=0)tooltip.ShowItem(row,RectTransformUtility.WorldToScreenPoint(null,row.TransformPoint(row.rect.center)),player,id,equip.HasValue);
        }
        public void OpenSelectedActions()
        { if(SelectedRow!=null){var r=SelectedRow.GetComponent<RectTransform>();OpenActions(RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center)));} }
        private void OpenActions(Vector2 screen)
        {
            if(selectedId==0)return;tooltip.Hide();
            foreach(Transform child in context){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            var item=player.GetItemInfo(selectedId);string label=selectedSlot.HasValue?"Remove":item?.Type==AssetItemType.Equip?"Equip":"Use";
            ClassicWindowSkin.Fill("ContextPaper",context,0,0,116,25,new Color32(238,238,231,255),true);
            var action=ClassicWindowSkin.Action("ItemAction",context,font,label,4,4,108,Act);action.interactable=!player.IsDead&&item?.Ammunition==null;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,screen,null,out var point);
            // Place after activation so the generic default position is initialized first.
            context.gameObject.SetActive(true);context.GetComponent<ClassicWindow>().Configure(new Vector2(.5f,.5f),HideActions);
            context.anchoredPosition=point+new Vector2(60,-14);context.GetComponent<ClassicWindow>().BeginDrag();
            context.GetComponent<ClassicWindow>().SetPosition(context.anchoredPosition);
        }
        private void HideActions(){if(context!=null)context.gameObject.SetActive(false);}
        private void Act()
        {
            if(world==null||selectedId==0)return;string result;
            if(selectedSlot.HasValue)world.RemoveEquipment(selectedSlot.Value,out result);else world.UseInventorySlot(category,selectedBagSlot,selectedId,out result);
            GetComponent<StatusBar>()?.PostMessage(result);HideActions();tooltip.Hide();dirty=true;
        }
        private void Update()
        {
            if(panel==null)return;
            if(dragSource!=null&&(player==null||player.IsDead||player.IsBasicAttacking||player.Inventory.Revision!=dragRevision))CancelItemDrag();
            if(player!=null&&mesos!=null)mesos.text=player.Mesos.ToString("N0");
            if(dirty&&(BagVisible||EquipmentVisible))Refresh();
        }
        internal bool BeginItemDrag(InventorySlotInteraction source,PointerEventData pointer)
        {
            if(world==null||!world.IsLocal||player==null||player.IsDead||player.IsBasicAttacking||source.ItemId==0||source.Category!=category||player.Inventory.GetStack(source.Category,source.Slot)?.ItemId!=source.ItemId)return false;
            CancelItemDrag();HideActions();tooltip.Hide();dragSource=source;dragRevision=player.Inventory.Revision;
            dragIcon=ClassicUI.Rect("DraggedInventoryItem",transform,32,32);var icon=dragIcon.gameObject.AddComponent<Image>();icon.sprite=source.transform.Find("Icon").GetComponent<Image>().sprite;icon.preserveAspect=true;icon.raycastTarget=false;DragItem(pointer);return true;
        }
        internal void DragItem(PointerEventData pointer)
        { if(dragIcon==null)return;var canvas=GetComponent<Canvas>();if(RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,pointer.position,canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,out var point))dragIcon.localPosition=point; }
        internal bool TryGetDraggedItem(InventorySlotInteraction source, out int id)
        {
            id = 0;
            if (source == null || source != dragSource || player == null || player.IsDead ||
                player.Inventory.Revision != dragRevision || player.Inventory.GetStack(source.Category, source.Slot)?.ItemId != source.ItemId) return false;
            id = source.ItemId; return true;
        }
        internal void DropItem(InventorySlotInteraction destination,InventorySlotInteraction source)
        {
            if(source==null||source!=dragSource||destination.Category!=source.Category)return;
            int id=source.ItemId;bool moved=world.MoveInventoryStack(source.Category,source.Slot,destination.Slot,id,dragRevision,out string result);
            if(moved){selectedId=id;selectedBagSlot=destination.Slot;selectedSlot=null;}
            GetComponent<StatusBar>()?.PostMessage(result);CancelItemDrag();dirty=true;
        }
        internal void CancelItemDrag(InventorySlotInteraction source=null)
        { if(source!=null&&source!=dragSource)return;dragSource=null;if(dragIcon!=null){dragIcon.gameObject.SetActive(false);Destroy(dragIcon.gameObject);dragIcon=null;} }
        private static Vector2 EquipmentPosition(EquipSlot slot)
        {
            Vector2 c;switch(slot){case EquipSlot.Hat:c=new Vector2(1,0);break;case EquipSlot.FaceAccessory:c=new Vector2(1,1);break;case EquipSlot.EyeAccessory:c=new Vector2(2,2);break;case EquipSlot.Earring:c=new Vector2(3,2);break;
                case EquipSlot.Top:case EquipSlot.Overall:c=new Vector2(1,3);break;case EquipSlot.Bottom:c=new Vector2(1,4);break;case EquipSlot.Shoes:c=new Vector2(2,5);break;case EquipSlot.Glove:c=new Vector2(0,4);break;case EquipSlot.Cape:c=new Vector2(0,3);break;
                case EquipSlot.Shield:c=new Vector2(4,3);break;case EquipSlot.Weapon:c=new Vector2(3,3);break;case EquipSlot.Ring1:c=new Vector2(3,1);break;case EquipSlot.Ring2:c=new Vector2(4,1);break;case EquipSlot.Ring3:c=new Vector2(3,4);break;case EquipSlot.Ring4:c=new Vector2(4,4);break;case EquipSlot.Pendant:c=new Vector2(2,3);break;case EquipSlot.Belt:c=new Vector2(2,4);break;default:c=new Vector2(0,1);break;}
            return new Vector2(5+33*c.x,35+33*c.y);
        }
        private void Unbind(){if(player!=null){player.Inventory.Changed-=Changed;player.EquipmentChanged-=Changed;}}
        private void OnDestroy(){CancelItemDrag();Unbind();if(world!=null)world.MapLoaded-=OnMapLoaded;if(font!=null)Destroy(font);}
    }
}
