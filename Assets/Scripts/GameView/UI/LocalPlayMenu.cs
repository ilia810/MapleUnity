using System.Collections.Generic;
using System.Linq;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Skills;
using MapleClient.GameData;
using UnityEngine;
using UnityEngine.UI;
using Vector2 = UnityEngine.Vector2;

namespace MapleClient.GameView.UI
{
    public sealed class LocalPlayMenu : MonoBehaviour
    {
        private GameManager manager;
        private GameWorld world;
        private Font font;
        private RectTransform panel, practice, shop;
        private Text wallet, notice, mesoHud, shopNotice;
        private Button shopButton, buyButton, sellButton, supplies;
        private ClassicTooltipView tooltip;
        private NpcSpawn npc;
        private bool dirty=true;
        private int buyOffset, sellOffset, category=1, quantity=1;
        private ShopOffer selectedBuy, selectedSell;
        private readonly List<GameObject> rows=new List<GameObject>();
        private readonly List<Button> categories=new List<Button>();
        private Scrollbar buyScroll,sellScroll;
        private Button buyUp,buyDown,sellUp,sellDown;
        private ClassicNpcPortrait shopPortrait;
        public bool ShopVisible=>shop!=null&&shop.gameObject.activeSelf;
        public void Bind(GameManager value)
        {
            manager=value;world=value.World;if(panel==null)CreateUI();
            world.Player.Inventory.Changed+=MarkDirty;world.Player.MesosChanged+=MarkDirty;world.ItemPickedUp+=PickedUp;
        }
        private void MarkDirty()=>dirty=true;
        private void PickedUp(int id,int count)=>Post(id==0?$"Picked up {count} mesos.":$"Picked up {world.Player.GetItemInfo(id)?.Name??"item"} x{count}.");
        private void Post(string text){notice.text=text;shopNotice.text=text;GetComponent<StatusBar>()?.PostMessage(text);}
        public void Show(bool visible)
        { if(panel==null)return;panel.gameObject.SetActive(visible);if(visible)panel.GetComponent<ClassicWindow>().Focus();dirty=true; }
        public void ShowPractice()
        { practice.gameObject.SetActive(true);practice.GetComponent<ClassicWindow>().Focus();Show(false); }
        public void OpenShop()
        {
            npc=world.NearestShop;
            if(npc==null){Show(true);Post("Visit Luna in the Henesys General Store to trade.");return;}
            var actor=WorldLabelAnchor.Active.FirstOrDefault(a=>a.Visible&&a.Kind==WorldLabelAnchor.ActorKind.Npc&&a.NpcId==npc.NpcId)?.GetComponent<ClassicNpcAnimator>();
            if(actor!=null)shopPortrait.Bind(actor,54,80,88,74);
            shop.gameObject.SetActive(true);shop.GetComponent<ClassicWindow>().Focus();buyOffset=sellOffset=0;selectedBuy=selectedSell=null;dirty=true;
        }
        private RectTransform Window(string name,float w,float h,string title,Vector2 center,System.Action close)
        {
            var root=ClassicUI.Window(name,transform,w,h);root.GetComponent<ClassicWindow>().Configure(center,close);
            var frame=ClassicWindowSkin.Surface(name+"Frame",root,w,h,font,title);ClassicUI.DragTitle(frame,root,w);
            ClassicUI.ArtButton("Close"+name.Replace("Panel",""),frame,"Basic.img/BtClose",w-16,5,()=>close());return root;
        }
        private Button Action(string name,Transform parent,string label,float y,UnityEngine.Events.UnityAction action)
            =>ClassicWindowSkin.Action(name,parent,font,label,10,y,230,action,new Color32(92,150,187,255));
        private void CreateUI()
        {
            font=Font.CreateDynamicFontFromOSFont("Arial",12);tooltip=ClassicTooltipView.For(transform);
            var hud=ClassicUI.Hud(transform);
            ClassicUI.HudButton("LocalPlayToggle",hud,"StatusBar.img/BtMenu",648,()=>Show(!panel.gameObject.activeSelf));
            ClassicUI.ArtButton("TradeToggle",hud,"StatusBar.img/KeySet",736,5,OpenShop);
            panel=Window("LocalPlayPanel",250,306,"ADVENTURE MENU",new Vector2(.75f,.48f),()=>Show(false));
            wallet=ClassicWindowSkin.Label("MesoWallet",panel,font,"",10,31,230,35,12);
            shopButton=Action("OpenLocalShop",panel,"Trade with Luna",74,()=>{Show(false);OpenShop();});
            Action("OpenCharacterProgression",panel,"First job advancement",100,()=>{GetComponent<CharacterProgressionView>()?.ShowAdvancement();Show(false);});
            Action("OpenPractice",panel,"Practice supplies and skills",126,ShowPractice);
            Action("SaveLocalProgress",panel,"Save progress",158,Save);Action("LoadLocalProgress",panel,"Load saved progress",184,Load);
            Action("OpenQuestJournal",panel,"Quest journal",210,()=>{GetComponent<ClassicQuestView>()?.Show(true);Show(false);});
            ClassicWindowSkin.Label("LocalGuideText",panel,font,"I Bag · E Equip · K Skills · C Stats\nV Talk · Double-click NPCs to talk",10,237,230,31,11);
            notice=ClassicWindowSkin.Label("LocalNotice",panel,font,"Saving is manual. Load replaces this session.",10,271,230,30,10);
            practice=Window("PracticePanel",250,347,"LOCAL PRACTICE",new Vector2(.43f,.53f),()=>practice.gameObject.SetActive(false));
            ClassicWindowSkin.Label("PracticeHint",practice,font,"Choose a kit to try equipment and skills.\nEach kit supplies items once per character.",10,31,230,36,11);
            supplies=Action("PracticeSupplies",practice,"Equipment and potion supplies",74,()=>{world.RequestPracticeSupplies(out var result);Post(result);practice.gameObject.SetActive(false);GetComponent<InventoryView>()?.ShowBag(true);});
            Action("PracticeSkills",practice,"Warrior skills",100,()=>PracticeSkills(false));Action("PracticeMagicSkills",practice,"Magician skills",126,()=>PracticeSkills(true));
            int[] types={145,146,147,149,133};string[] names={"Bow","Crossbow","Claw","Gun","Thief dagger"};
            for(int i=0;i<types.Length;i++){int type=types[i];Action("RangedPreset_"+type,practice,names[i]+" practice",158+25*i,()=>Ranged(type));}
            RectTransform support=null;
            support=Window("SupportPracticePanel",250,357,"SUPPORT PRACTICE",new Vector2(.43f,.53f),()=>support.gameObject.SetActive(false));
            ClassicWindowSkin.Label("SupportPracticeHint",support,font,"Changes job and teaches level-1 buffs.\nKeeps your items, HP/MP and earned points.",10,31,230,36,11);
            int[] supportJobs={100,200,110,130,210,220,300,400,410,420};
            string[] supportNames={"Iron Body","Magician defenses","Fighter: Rage","Spearman: Hyper Body","Fire / Poison: Meditation","Ice / Lightning: Meditation","Archer: Focus","Thief: Dark Sight","Assassin: Haste","Bandit: Haste"};
            for(int i=0;i<supportJobs.Length;i++)
            { int job=supportJobs[i];Action("SupportPreset_"+job,support,supportNames[i],74+25*i,()=>{GetComponent<SkillMenu>()?.PracticeSupport(job);support.gameObject.SetActive(false);}); }
            support.gameObject.SetActive(false);
            Action("OpenSupportPractice",practice,"Defensive and support skills",289,()=>{support.gameObject.SetActive(true);support.GetComponent<ClassicWindow>().Focus();practice.gameObject.SetActive(false);});
            ClassicWindowSkin.Label("PracticeFooter",practice,font,"Equip your new weapon from the bag.",10,323,230,16,10);
            CreateShop();panel.gameObject.SetActive(false);practice.gameObject.SetActive(false);
        }
        private void PracticeSkills(bool magic){GetComponent<SkillMenu>()?.Practice(magic);practice.gameObject.SetActive(false);}
        private void Ranged(int type)
        {
            if(world.RequestWeaponPractice(type,out var result)){int i=0;foreach(int id in SourceSkillRules.WeaponPracticeSkills(type))GetComponent<SkillBar>()?.AssignSkillToSlot(id,i++);}
            Post(result);practice.gameObject.SetActive(false);GetComponent<InventoryView>()?.ShowBag(true);
        }
        private void CreateShop()
        {
            shop=ClassicUI.Window("LocalShopPanel",transform,463,378);shop.GetComponent<ClassicWindow>().Configure(new Vector2(.5f,.5f),CloseShop);
            // Preserve both native rounded frames and portrait wells; repeat one authored
            // list row for the sixth entry instead of stretching the five-row bitmap.
            ClassicUI.Region("ShopTop",shop,"UIWindow.img/Shop/backgrnd",new Rect(0,0,463,126),0,0,463,126).raycastTarget=true;
            for(int row=0;row<6;row++)
                ClassicUI.Region("ShopRowArt"+row,shop,"UIWindow.img/Shop/backgrnd",new Rect(0,126,463,39),0,126+39*row,463,39).raycastTarget=true;
            ClassicUI.Region("ShopBottom",shop,"UIWindow.img/Shop/backgrnd",new Rect(0,321,463,18),0,360,463,18).raycastTarget=true;
            for(int side=0;side<2;side++)
            {
                var pane=ClassicWindowSkin.Fill(side==0?"BuyPane":"SellPane",shop,side*231,120,231,240,Color.clear,true);
                pane.gameObject.AddComponent<ClassicScrollWheel>().Scroll=d=>{if(pane.name=="BuyPane")buyOffset+=d;else sellOffset+=d;dirty=true;};
            }
            ClassicUI.DragTitle(shop,shop,463);
            var portrait=ClassicWindowSkin.Fill("Shopkeeper",shop,23,9,64,70,Color.white);
            portrait.sprite=SpriteLoader.LoadSprite(NXAssetLoader.Instance.GetNxFile("npc")?.GetNode("1011100.img/stand/0"),"npc/1011100.img/stand/0");portrait.preserveAspect=true;portrait.enabled=portrait.sprite!=null;
            shopPortrait=portrait.gameObject.AddComponent<ClassicNpcPortrait>();
            var playerPortrait=ClassicUI.Rect("ShopPlayer",shop,90,78);ClassicUI.Place(playerPortrait,243,7,90,78);playerPortrait.gameObject.AddComponent<ClassicPlayerPortrait>();
            ClassicUI.ArtButton("LeaveShop",shop,"UIWindow.img/Shop/BtExit",151,14,CloseShop);
            buyButton=ClassicUI.ArtButton("ShopBuy",shop,"UIWindow.img/Shop/BtBuy",151,37,()=>Trade(false));
            sellButton=ClassicUI.ArtButton("ShopSell",shop,"UIWindow.img/Shop/BtSell",375,37,()=>Trade(true));
            ClassicWindowSkin.Fill("MesoField",shop,360,66,92,15,Color.white);
            ClassicUI.Region("ShopCoin",shop,"UIWindow.img/Shop/backgrnd",new Rect(343,65,13,15),343,66,13,15);
            mesoHud=ClassicWindowSkin.Label("MesoHud",shop,font,"0",361,66,88,15,11);mesoHud.alignment=TextAnchor.MiddleRight;
            var all=ClassicUI.Button("ShopBuyTab",shop,font,"All",()=>{buyOffset=0;dirty=true;});ClassicUI.Place(all.GetComponent<RectTransform>(),6,94,55,22);ClassicUI.TabState(all.transform,true);
            string[] labels={"Equip","Use","Set-up","Etc.","Cash"};
            for(int i=0;i<5;i++){int c=i+1;var tab=ClassicUI.Button(i==0?"ShopSellTab":"ShopCategory_"+c,shop,font,labels[i],()=>{category=c;sellOffset=0;selectedSell=null;dirty=true;});ClassicUI.Place(tab.GetComponent<RectTransform>(),237+42*i,94,42,22);tab.GetComponentInChildren<Text>().fontSize=10;categories.Add(tab);}
            buyScroll=ClassicWindowSkin.Scrollbar("ShopBuyScroll",shop,213,140,203,v=>{buyOffset=Mathf.RoundToInt((1-v)*Mathf.Max(0,OfflineEconomy.Stock.Count()-6));dirty=true;});
            sellScroll=ClassicWindowSkin.Scrollbar("ShopSellScroll",shop,445,140,203,v=>{sellOffset=Mathf.RoundToInt((1-v)*Mathf.Max(0,SellEntries().Count-6));dirty=true;});
            buyUp=Arrow("ShopBuyUp",213,126,true,()=>{buyOffset--;dirty=true;});buyDown=Arrow("ShopBuyDown",213,345,false,()=>{buyOffset++;dirty=true;});
            sellUp=Arrow("ShopSellUp",445,126,true,()=>{sellOffset--;dirty=true;});sellDown=Arrow("ShopSellDown",445,345,false,()=>{sellOffset++;dirty=true;});
            shopNotice=ClassicWindowSkin.Label("ShopNotice",shop,font,"Select an item, then Buy or Sell.",8,362,309,12,9);
            ClassicWindowSkin.Action("ShopQuantity1",shop,font,"×1",362,359,43,()=>{quantity=1;dirty=true;});ClassicWindowSkin.Action("ShopQuantity10",shop,font,"×10",407,359,43,()=>{quantity=10;dirty=true;});
            shop.gameObject.SetActive(false);
        }
        private Button Arrow(string name,float x,float y,bool up,UnityEngine.Events.UnityAction action)
        {
            return ClassicWindowSkin.ScrollArrow(name,shop,x,y,up,action);
        }
        private void CloseShop(){shop.gameObject.SetActive(false);tooltip.Hide();npc=null;}
        private void Save(){manager.GetComponent<LocalProgressController>().TrySave(out var result);Post(result);}
        private void Load(){if(manager.GetComponent<LocalProgressController>().TryLoad(out var result))CloseShop();Post(result);dirty=true;}
        private List<ShopOffer> SellEntries()=>world.Player.Inventory.GetStacks().Where(s=>s.ItemId/1000000==category&&OfflineEconomy.SellPrice(world.Player.GetItemInfo(s.ItemId))>0)
            .Select(s=>new ShopOffer(s.ItemId,s.Quantity,OfflineEconomy.SellPrice(world.Player.GetItemInfo(s.ItemId)),s.Slot)).ToList();
        private void Refresh()
        {
            dirty=false;foreach(var row in rows){row.SetActive(false);Destroy(row);}rows.Clear();tooltip.Hide();
            var buy=OfflineEconomy.Stock.ToList();var sell=SellEntries();buyOffset=Mathf.Clamp(buyOffset,0,Mathf.Max(0,buy.Count-6));sellOffset=Mathf.Clamp(sellOffset,0,Mathf.Max(0,sell.Count-6));
            selectedSell=sell.FirstOrDefault(s=>selectedSell!=null&&s.ItemId==selectedSell.ItemId&&s.BagSlot==selectedSell.BagSlot);
            buyScroll.size=Mathf.Min(1,6f/Mathf.Max(1,buy.Count));buyScroll.SetValueWithoutNotify(1-buyOffset/(float)Mathf.Max(1,buy.Count-6));
            sellScroll.size=Mathf.Min(1,6f/Mathf.Max(1,sell.Count));sellScroll.SetValueWithoutNotify(1-sellOffset/(float)Mathf.Max(1,sell.Count-6));
            buyScroll.interactable=buy.Count>6;buyScroll.handleRect.gameObject.SetActive(buy.Count>6);
            sellScroll.interactable=sell.Count>6;sellScroll.handleRect.gameObject.SetActive(sell.Count>6);
            buyUp.interactable=buyOffset>0;buyDown.interactable=buyOffset<buy.Count-6;
            sellUp.interactable=sellOffset>0;sellDown.interactable=sellOffset<sell.Count-6;
            for(int i=0;i<categories.Count;i++)ClassicUI.TabState(categories[i].transform,category==i+1);
            if(ShopVisible){BuildRows(buy,false,buyOffset);BuildRows(sell,true,sellOffset);}
        }
        private void BuildRows(List<ShopOffer> entries,bool sell,int offset)
        {
            var named=new HashSet<int>();int i=0;
            foreach(var offer in entries.Skip(offset).Take(6))
            {
                int id=offer.ItemId;string name=(sell?"SellItem_":"BuyItem_")+id+(named.Add(id)?"":"_Slot_"+offer.BagSlot);
                var selected=sell?selectedSell:selectedBuy;bool active=selected!=null&&selected.ItemId==id&&selected.BagSlot==offer.BagSlot;
                var image=ClassicWindowSkin.Fill(name,shop,sell?239:8,126+39*i++,196,35,Color.clear,true);
                if(active)ClassicUI.Art("Selection",image.transform,"UIWindow.img/Shop/select",35,0);
                var button=image.gameObject.AddComponent<Button>();button.onClick.AddListener(()=>{if(sell)selectedSell=offer;else selectedBuy=offer;dirty=true;});
                var icon=ClassicWindowSkin.Fill("Icon",image.transform,0,0,32,32,Color.white);icon.sprite=NXAssetLoader.Instance.LoadItemIcon(id);icon.preserveAspect=true;icon.enabled=icon.sprite!=null;
                var title=ClassicWindowSkin.Label("Label",image.transform,font,world.Player.GetItemInfo(id)?.Name??"Item",38,0,158,18,12,active?Color.white:ClassicWindowSkin.Ink);title.horizontalOverflow=HorizontalWrapMode.Overflow;title.text=FitItemName(title,title.text,158);image.gameObject.AddComponent<RectMask2D>();
                ClassicUI.Art("PriceCoin",image.transform,"UIWindow.img/Shop/meso",38,20);
                ClassicWindowSkin.Label("Price",image.transform,font,$"{offer.Price:N0} Mesos"+(sell?$"  ×{offer.Quantity}":offer.Quantity>1?$" / {offer.Quantity}":""),53,18,143,17,11,active?Color.white:ClassicWindowSkin.Ink);
                var hover=image.gameObject.AddComponent<ClassicHover>();hover.Enter=e=>tooltip.ShowItem(image,e.position,world.Player,id);hover.Exit=()=>tooltip.Hide(image);
                image.gameObject.AddComponent<ClassicScrollWheel>().Scroll=d=>{if(sell)sellOffset+=d;else buyOffset+=d;dirty=true;};
                rows.Add(image.gameObject);
            }
        }
        private static string FitItemName(Text label,string text,float width)
        {
            label.text=text;if(label.preferredWidth<=width)return text;
            while(text.Length>0){text=text.Substring(0,text.Length-1);label.text=text+"…";if(label.preferredWidth<=width)return label.text;}
            return "…";
        }
        private void Trade(bool sell)
        {
            var offer=sell?selectedSell:selectedBuy;if(offer==null)return;string result;
            if(sell)world.SellToShop(npc,offer.ItemId,quantity,out result,offer.BagSlot);else world.BuyFromShop(npc,offer.ItemId,quantity,out result);
            Post(result);dirty=true;
        }
        private void Update()
        {
            if(world==null||panel==null)return;
            wallet.text=$"{world.Player.Name} · Level {world.Player.Level}\n{world.Player.Mesos:N0} mesos";mesoHud.text=world.Player.Mesos.ToString("N0");
            shopButton.interactable=world.NearestShop!=null;supplies.interactable=world.CanRequestPracticeSupplies;
            if(ShopVisible&&!world.CanUseShop(npc)){CloseShop();Post("Trading closed. Stand near Luna while idle to trade.");}
            buyButton.interactable=selectedBuy!=null&&world.CanUseShop(npc)&&(long)selectedBuy.Price*quantity<=world.Player.Mesos;
            sellButton.interactable=selectedSell!=null&&world.CanUseShop(npc)&&selectedSell.Quantity>=quantity;
            if(dirty)Refresh();
        }
        private void OnDestroy(){if(world!=null){world.Player.Inventory.Changed-=MarkDirty;world.Player.MesosChanged-=MarkDirty;world.ItemPickedUp-=PickedUp;}if(font!=null)Destroy(font);}
    }
}
