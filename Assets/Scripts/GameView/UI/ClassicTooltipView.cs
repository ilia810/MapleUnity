using System.Linq;
using System.Text;
using MapleClient.GameData;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Interfaces;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace MapleClient.GameView.UI
{
    public sealed class ClassicTooltipView : MonoBehaviour
    {
        private RectTransform root, main, compare;
        private CanvasGroup visibility;
        private Font font;
        private Object owner;
        private Vector2 pointer;
        private string contentKey;
        private Player hoveredPlayer;private int hoveredItem;private bool hoveredEquipped;
        public Object Owner => owner;
        public static ClassicTooltipView For(Transform canvas)
        { return canvas.GetComponent<ClassicTooltipView>()??canvas.gameObject.AddComponent<ClassicTooltipView>(); }
        private void Ensure()
        {
            if(root!=null)return;
            font=Font.CreateDynamicFontFromOSFont("Arial",12);
            root=ClassicUI.Rect("ClassicTooltip",transform,0,0);
            visibility=root.gameObject.AddComponent<CanvasGroup>();visibility.blocksRaycasts=false;visibility.interactable=false;visibility.alpha=0;
            main=ClassicUI.Rect("ItemTooltip",root,286,120);compare=ClassicUI.Rect("EquippedTooltip",root,236,120);compare.gameObject.SetActive(false);
        }
        public void Hide(Object source=null) { if(source!=null&&owner!=source)return;owner=null;hoveredPlayer=null;if(visibility!=null)visibility.alpha=0; }
        public void ShowText(Object source,Vector2 screen,string title,string body,Sprite icon=null,string detailsName="TooltipDetails")
        {
            Ensure();hoveredPlayer=null;pointer=screen;string key=title+"\n"+body+"\n"+detailsName+":"+(icon!=null?icon.GetInstanceID():0);
            if(owner==source&&contentKey==key){Layout();return;}
            owner=source;contentKey=key;compare.gameObject.SetActive(false);
            Draw(main,title,body,icon,286,detailsName);Layout();
        }
        public void ShowItem(Object source,Vector2 screen,Player player,int itemId,bool equipped=false)
        {
            Ensure();var item=player?.GetItemInfo(itemId);if(item==null){Hide();return;}
            hoveredPlayer=player;hoveredItem=itemId;hoveredEquipped=equipped;
            pointer=screen;string key=itemId+":"+equipped+":"+player.JobId+":"+player.Gender+":"+player.Inventory.Revision+":"+player.Level+":"+player.STR+":"+player.DEX+":"+player.INT+":"+player.LUK+":"+string.Join(",",player.GetEquippedItems().Values);
            if(owner==source&&contentKey==key){Layout();return;}
            owner=source;contentKey=key;bool equipment=item.Type==ItemType.Equip;
            if(equipment)DrawEquipment(main,item,player,false,"ItemDetails");
            else Draw(main,item.Name,Describe(item,player),NXAssetLoader.Instance.LoadItemIcon(itemId),286,"ItemDetails");
            int other=0;
            if(equipment&&!equipped&&item.EquipmentSlot.HasValue)player.GetEquippedItems().TryGetValue(item.EquipmentSlot.Value,out other);
            compare.gameObject.SetActive(other!=0);
            if(other!=0)
            {
                var info=player.GetItemInfo(other);
                if(info!=null)DrawEquipment(compare,info,player,true,"EquippedItemDetails");else compare.gameObject.SetActive(false);
            }
            Layout();
        }
        private float Title(RectTransform card,string title,float width)
        {
            foreach(Transform child in card){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            var dot=ClassicWindowSkin.Label("TitleDot",card,font,"•",7,7,10,20,14,new Color32(117,223,255,255));
            var text=ClassicWindowSkin.Label("TooltipTitle",card,font,Escape(title),18,7,width-26,36,13,Color.white,true);
            float height=Mathf.Max(19,text.preferredHeight);ClassicUI.Place(text.rectTransform,18,7,width-26,height);
            return height+14;
        }
        private static void Icon(RectTransform card,Sprite icon,float y)
        {
            if(icon==null)return;
            ClassicWindowSkin.Fill("IconBacking",card,9,y,66,66,new Color(1,1,1,.58f));
            var art=ClassicWindowSkin.Fill("ItemIcon",card,10,y+1,64,64,Color.white);art.sprite=icon;art.preserveAspect=true;
        }
        private void Frame(RectTransform card,float width,float height)
        {
            card.sizeDelta=new Vector2(width,height);
            var background=ClassicWindowSkin.Fill("Navy",card,0,0,width,height,new Color32(39,39,91,230));background.transform.SetAsFirstSibling();
            foreach(var edge in new[]{new Rect(0,0,width,1),new Rect(0,height-1,width,1),new Rect(0,0,1,height),new Rect(width-1,0,1,height)})
                ClassicWindowSkin.Fill("Edge",card,edge.x,edge.y,edge.width,edge.height,new Color(.78f,.8f,.91f,.9f));
        }
        private void Draw(RectTransform card,string title,string body,Sprite icon,float width,string detailsName)
        {
            float y=Title(card,title,width);Icon(card,icon,y);
            float x=icon==null?9:84;
            var text=ClassicWindowSkin.Label(detailsName,card,font,body,x,y,width-x-10,300,12,new Color32(228,230,243,255));
            float h=Mathf.Max(20,text.preferredHeight);ClassicUI.Place(text.rectTransform,x,y,width-x-10,h);
            Frame(card,width,y+Mathf.Max(icon!=null?66:0,h)+10);
        }
        private void DrawEquipment(RectTransform card,ItemInfo item,Player player,bool equipped,string detailsName)
        {
            const float width=236;float y=Title(card,item.Name+(equipped?" (Equipped)":""),width);
            Icon(card,NXAssetLoader.Instance.LoadItemIcon(item.ItemId),y);
            string description=Describe(item,player);int split=description.IndexOf("\n\n",System.StringComparison.Ordinal);
            string requirements=description.Substring(0,split);
            var req=ClassicWindowSkin.Label("Requirements",card,font,requirements,84,y,142,66,10,new Color32(190,200,222,255));
            req.lineSpacing=1.15f;
            string[] jobs={"BEGINNER","WARRIOR","MAGICIAN","BOWMAN","THIEF","PIRATE"};
            var strip=ClassicWindowSkin.Fill("JobEligibility",card,8,y+73,220,16,new Color(0,0,0,.24f));
            var labels=new Text[jobs.Length];
            for(int i=0;i<jobs.Length;i++)
            {
                bool allowed=AllowsJob(item,i);
                var text=ClassicWindowSkin.Label("AllowedJob_"+i,strip.transform,font,jobs[i],0,0,220,16,8,allowed?new Color32(248,160,72,255):new Color32(137,145,165,255));
                text.alignment=TextAnchor.MiddleCenter;text.horizontalOverflow=HorizontalWrapMode.Overflow;labels[i]=text;
            }
            // Measure each name instead of forcing longer names into equal columns.
            if(labels.Sum(t=>Mathf.Ceil(t.preferredWidth))>210)foreach(var label in labels)label.fontSize=7;
            float spacing=(220-labels.Sum(t=>Mathf.Ceil(t.preferredWidth)))/(labels.Length-1),left=0;
            foreach(var label in labels){float w=Mathf.Ceil(label.preferredWidth);ClassicUI.Place(label.rectTransform,left,0,w,16);left+=w+spacing;}
            ClassicWindowSkin.Fill("DetailRule",card,8,y+93,220,1,new Color(1,1,1,.45f));
            var body=new StringBuilder();body.Append("Type: ").Append(item.EquipmentSlot?.ToString()??"Equipment").Append('\n');
            if(item.Gender!=2)body.Append("<color=").Append(item.Gender==player.Gender?"#b7c3dc":"#ff5454").Append(">For ").Append(item.Gender==0?"male":"female").Append(" characters</color>\n");
            if(item.Stats!=null)foreach(var stat in item.Stats.OrderBy(e=>e.Key))body.Append("• ").Append(StatName(stat.Key)).Append(": ").Append(stat.Value.ToString("+0;-0;0")).Append('\n');
            if(item.Weapon!=null)body.Append(item.IsTwoHanded?"Two-handed":"One-handed").Append(" · Attack speed ").Append(item.Weapon.AttackSpeed).Append('\n');
            body.Append("Remaining enhancements: ").Append(item.Slots);
            if(item.IsOneOfAKind)body.Append("\n<color=#f8a048>One-of-a-kind item</color>");
            if(!item.IsTradeable)body.Append("\n<color=#f8a048>Untradeable</color>");
            var details=ClassicWindowSkin.Label(detailsName,card,font,body.ToString(),9,y+100,218,300,12,new Color32(228,230,243,255));
            float h=Mathf.Max(20,details.preferredHeight);ClassicUI.Place(details.rectTransform,9,y+100,218,h);Frame(card,width,y+110+h);
        }
        private static bool AllowsJob(ItemInfo item,int family)=>item.RequiredJobMask==0||
            (family==0?item.RequiredJobMask==-1:item.RequiredJobMask>0&&(item.RequiredJobMask&(1<<(family-1)))!=0);
        private void Layout()
        {
            float width=main.rect.width+(compare.gameObject.activeSelf?compare.rect.width+3:0),height=Mathf.Max(main.rect.height,compare.gameObject.activeSelf?compare.rect.height:0);
            root.sizeDelta=new Vector2(width,height);ClassicUI.Place(main,0,0,main.rect.width,main.rect.height);
            ClassicUI.Place(compare,main.rect.width+3,0,compare.rect.width,compare.rect.height);
            var canvas=(RectTransform)transform;var c=GetComponent<Canvas>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,pointer,c!=null&&c.renderMode!=RenderMode.ScreenSpaceOverlay?c.worldCamera:null,out var point);
            float scale=Mathf.Min(1,(canvas.rect.width-16)/width,(canvas.rect.height-90)/height);root.localScale=Vector3.one*scale;
            root.anchorMin=root.anchorMax=new Vector2(.5f,.5f);root.pivot=new Vector2(0,1);
            float x=point.x+12,y=point.y-18;
            if(x+width*scale>canvas.rect.xMax-8)x=point.x-width*scale-12;
            root.anchoredPosition=new Vector2(Mathf.Clamp(x,canvas.rect.xMin+8,canvas.rect.xMax-width*scale-8),Mathf.Clamp(y,canvas.rect.yMin+height*scale+8,canvas.rect.yMax-8));
            visibility.alpha=1;root.SetAsLastSibling();
        }
        private void LateUpdate()
        {
            var ownerObject=owner as GameObject;
            if(owner is Component component)ownerObject=component.gameObject;
            if(owner==null||(ownerObject!=null&&!ownerObject.activeInHierarchy)){Hide();return;}
            if(visibility.alpha<=0)return;
            if(hoveredPlayer!=null)ShowItem(owner,pointer,hoveredPlayer,hoveredItem,hoveredEquipped);else Layout();
        }
        public static string Escape(string s) => (s??"").Replace("<","‹").Replace(">","›");
        public static string StatName(StatType stat)
        {
            switch(stat){case StatType.WeaponAttack:return "Weapon attack";case StatType.MagicAttack:return "Magic attack";case StatType.WeaponDefense:return "Weapon defense";
                case StatType.MagicDefense:return "Magic defense";case StatType.Avoidability:return "Evasion";default:return stat.ToString();}
        }
        public static string Describe(ItemInfo item,Player player)
        {
            if(item.Type==ItemType.Equip)
            {
                var s=new StringBuilder();
                s.Append("<color=").Append(player.Level<item.RequiredLevel?"#ff5454":"#b7c3dc").Append(">REQ LEVEL : ").Append(item.RequiredLevel).Append("</color>\n");
                int[] required={item.RequiredStr,item.RequiredDex,item.RequiredInt,item.RequiredLuk},actual={player.STR,player.DEX,player.INT,player.LUK};string[] names={"STR","DEX","INT","LUK"};
                for(int i=0;i<4;i++)s.Append("<color=").Append(actual[i]<required[i]?"#ff5454":"#b7c3dc").Append(">REQ ").Append(names[i]).Append(" : ").Append(required[i]).Append("</color>\n");
                s.Append("\n");
                string[] jobNames={"BEGINNER","WARRIOR","MAGICIAN","BOWMAN","THIEF","PIRATE"};
                for(int i=0;i<jobNames.Length;i++)
                {
                    bool allowed=item.RequiredJobMask==0||(i==0?item.RequiredJobMask==-1:(item.RequiredJobMask>0&&(item.RequiredJobMask&(1<<(i-1)))!=0));
                    s.Append("<color=").Append(allowed?"#f8a048":"#8991a5").Append(">").Append(jobNames[i]).Append("</color>").Append(i%3==2?'\n':' ');
                }
                s.Append("Type: ").Append(item.EquipmentSlot?.ToString()??"Equipment").Append('\n');
                if(item.Stats!=null)foreach(var stat in item.Stats)s.Append("• ").Append(StatName(stat.Key)).Append(": ").Append(stat.Value.ToString("+0;-0;0")).Append('\n');
                if(item.Weapon!=null)s.Append(item.IsTwoHanded?"Two-handed":"One-handed").Append(" · Attack speed ").Append(item.Weapon.AttackSpeed).Append('\n');
                s.Append("Upgrade slots: ").Append(item.Slots);
                return s.ToString().TrimEnd();
            }
            var parts=new System.Collections.Generic.List<string>();
            if(!string.IsNullOrWhiteSpace(item.Description))parts.Add(Escape(item.Description.Replace("\\r\\n","\n").Replace("\\n","\n").Replace("\\r","\n").Replace("\r","")));
            if(item.Hp>0)parts.Add("Restores "+item.Hp+" HP.");if(item.Mp>0)parts.Add("Restores "+item.Mp+" MP.");
            if(item.HpRate>0)parts.Add("Restores "+item.HpRate+"% HP.");if(item.MpRate>0)parts.Add("Restores "+item.MpRate+"% MP.");
            if(item.IsStatBuffConsumable&&item.Buffs!=null){foreach(var b in item.Buffs)parts.Add(b.Key+" "+b.Value.ToString("+0;-0;0"));parts.Add((item.Time/1000)+" seconds.");}
            if(item.Ammunition!=null)parts.Add("Used automatically by compatible weapons.");
            if(parts.Count==0)parts.Add("This item's action is not available in local play.");
            return string.Join("\n",parts);
        }
        private void OnDestroy(){if(font!=null)Destroy(font);}
    }
    public sealed class ClassicHover : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerMoveHandler
    {
        public System.Action<PointerEventData> Enter;
        public System.Action Exit;
        public void OnPointerEnter(PointerEventData e)=>Enter?.Invoke(e);
        public void OnPointerMove(PointerEventData e)=>Enter?.Invoke(e);
        public void OnPointerExit(PointerEventData e)=>Exit?.Invoke();
        private void OnDisable()=>Exit?.Invoke();
    }
    public sealed class ClassicItemClick : MonoBehaviour,IPointerClickHandler
    {
        public System.Action<PointerEventData> Right,Double;
        public void OnPointerClick(PointerEventData e){if(e.button==PointerEventData.InputButton.Right)Right?.Invoke(e);else if(e.button==PointerEventData.InputButton.Left&&e.clickCount==2)Double?.Invoke(e);}
    }
}
