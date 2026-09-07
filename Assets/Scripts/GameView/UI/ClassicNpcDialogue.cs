using System;
using System.Collections.Generic;
using System.Linq;
using GameData;
using MapleClient.GameData;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Vector2 = UnityEngine.Vector2;

namespace MapleClient.GameView.UI
{
    public sealed class ClassicNpcDialogue : MonoBehaviour
    {
        private GameWorld world;
        private Font font;
        private RectTransform panel,contents;
        private NpcSpawn npc,lastClicked;
        private ClassicNpcAnimator speakingActor;
        private long revision;
        private float lastClickTime;
        private QuestDefinition quest;
        private string[] pages;
        private int page;
        private Action finalAction;
        private bool question;
        private readonly List<RaycastResult> uiHits=new List<RaycastResult>();
        public bool Visible => panel!=null && panel.gameObject.activeSelf;
        public int NpcId=>npc?.NpcId ?? 0;
        public bool PortraitOnRight {get;private set;}
        private float TextX=>PortraitOnRight?18:151;
        public void Bind(GameWorld value)
        {
            world=value;
            if(panel!=null)return;
            font=Font.CreateDynamicFontFromOSFont("Arial",12);
            panel=ClassicUI.Window("NpcDialoguePanel",transform,529,211);
            panel.GetComponent<ClassicWindow>().Configure(new Vector2(.5f,.58f),Close);panel.gameObject.SetActive(false);
        }
        private void Update()
        {
            if(world==null)return;
            if(Visible && (world.MapRevision!=revision || !world.CanTalkToNpc(npc)))Close();
            if(ClassicWindowManager.IsTyping)return;
            if(Input.GetMouseButtonDown(0))ClickWorld(Input.mousePosition);
        }
        public void TalkNearest()
        {
            if(world!=null && !TryOpen(world.NearestNpc))Post("Stand near an NPC, then use your Talk shortcut or double-click them.");
        }
        public bool ClickWorld(Vector2 screen)
        {
            var anchor=NpcAtScreen(screen);if(anchor==null)return false;
            var candidate=SpawnFor(anchor);
            if(candidate==null || !world.CanTalkToNpc(candidate)){Post("Move closer to speak to "+anchor.Label+".");return false;}
            bool doubleClick=ReferenceEquals(candidate,lastClicked) && Time.unscaledTime-lastClickTime<.4f;
            lastClicked=candidate;lastClickTime=Time.unscaledTime;
            if(doubleClick){lastClicked=null;return TryOpen(candidate);}return false;
        }
        public NpcSpawn SpawnFor(WorldLabelAnchor anchor)=>world?.CurrentMap?.NpcSpawns.Where(n=>n.NpcId==anchor.NpcId)
            .OrderBy(n=>Mathf.Abs(n.X/100f-anchor.Feet.x)).FirstOrDefault();
        public WorldLabelAnchor NpcAtScreen(Vector2 screen)
        {
            if(EventSystem.current!=null)
            {
                uiHits.Clear();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=screen},uiHits);
                if(uiHits.Count>0)return null;
            }
            var camera=Camera.main;if(camera==null)return null;
            foreach(var anchor in WorldLabelAnchor.Active.Where(a=>a.Visible && a.Kind==WorldLabelAnchor.ActorKind.Npc).OrderByDescending(a=>a.transform.position.y))
            {
                var renderer=anchor.GetComponentInChildren<SpriteRenderer>();if(renderer==null || renderer.sprite==null)continue;
                var bounds=renderer.bounds;var min=camera.WorldToScreenPoint(bounds.min);var max=camera.WorldToScreenPoint(bounds.max);
                if(!new Rect(min.x,min.y,max.x-min.x,max.y-min.y).Contains(screen))continue;
                if(min.z>0)return anchor;
            }
            return null;
        }
        public bool TryOpen(NpcSpawn value)
        {
            if(!world.CanTalkToNpc(value))return false;
            if(speakingActor!=null)speakingActor.DialogueSpeaking=false;
            npc=value;revision=world.MapRevision;quest=null;
            speakingActor=WorldLabelAnchor.Active.Where(a=>a.Visible && a.Kind==WorldLabelAnchor.ActorKind.Npc && a.NpcId==npc.NpcId)
                .OrderBy(a=>Mathf.Abs(a.Feet.x-npc.X/100f)).FirstOrDefault()?.GetComponent<ClassicNpcAnimator>();
            if(speakingActor!=null)speakingActor.DialogueSpeaking=true;
            // Offline Say data has no speaker-side flag. Keep the NPC on its side of the player.
            PortraitOnRight=npc.X/100f>world.Player.Position.X+.05f;
            panel.gameObject.SetActive(true);Menu();panel.GetComponent<ClassicWindow>().Focus();return true;
        }
        public void Close(){if(speakingActor!=null)speakingActor.DialogueSpeaking=false;speakingActor=null;if(panel!=null)panel.gameObject.SetActive(false);npc=null;quest=null;finalAction=null;}
        private void Frame()
        {
            if(contents!=null){contents.gameObject.SetActive(false);Destroy(contents.gameObject);}
            contents=ClassicUI.Rect("NpcDialogueContents",panel,529,211);ClassicUI.Place(contents,0,0,529,211);
            ClassicUI.Art("DialogueTop",contents,"UIWindow.img/UtilDlgEx/t",0,0).raycastTarget=true;
            // Repeat the authored twenty-pixel strip; stretching it produces visible vertical bands in its dither.
            for(int i=0;i<6;i++)ClassicUI.Art("DialogueMiddle"+i,contents,"UIWindow.img/UtilDlgEx/c",0,28+i*20).raycastTarget=true;
            ClassicUI.Region("DialogueMiddleTail",contents,"UIWindow.img/UtilDlgEx/c",new Rect(0,0,529,5),0,148,529,5).raycastTarget=true;
            ClassicUI.Art("DialogueBottom",contents,"UIWindow.img/UtilDlgEx/s",0,153).raycastTarget=true;
            if(PortraitOnRight)
                foreach(Transform child in contents)
                {
                    // Mirror only the empty frame, then lay out portrait, text and buttons normally.
                    var r=(RectTransform)child;r.pivot=new Vector2(.5f,1);r.anchoredPosition+=new Vector2(529/2f,0);r.localScale=new Vector3(-1,1,1);
                }
            ClassicUI.DragTitle(contents,panel,529);
            ClassicUI.ArtButton("EndNpcChat",contents,"UIWindow.img/UtilDlgEx/BtClose",8,187,Close);
            float baseline=PortraitOnRight?133:95,portraitHeight=PortraitOnRight?111:78;
            var portrait=ClassicWindowSkin.Fill("DialogueNpcPortrait",contents,21,17,100,portraitHeight,Color.white);
            var sprite=NXDataManagerSingleton.Instance.GetNPCSpriteWithOrigin(npc.NpcId.ToString()).sprite;
            portrait.sprite=sprite;portrait.preserveAspect=true;portrait.enabled=sprite!=null;
            if(sprite!=null)
            {
                float scale=Mathf.Min(1,100/sprite.rect.width,portraitHeight/sprite.rect.height);
                ClassicUI.Place(portrait.rectTransform,(PortraitOnRight?458:71)-sprite.rect.width*scale/2,baseline-sprite.rect.height*scale,sprite.rect.width*scale,sprite.rect.height*scale);
            }
            if(speakingActor!=null && speakingActor.Frame!=null)
                portrait.gameObject.AddComponent<ClassicNpcPortrait>().Bind(speakingActor,PortraitOnRight?458:71,baseline,100,portraitHeight);
            ClassicUI.Art("NpcNameBar",contents,"UIWindow.img/UtilDlgEx/bar",PortraitOnRight?390:14,baseline+9);
            var name=ClassicWindowSkin.Label("DialogueNpcName",contents,font,ClassicQuestText.Npc(npc.NpcId),PortraitOnRight?394:18,baseline+11,114,16,11,Color.white);name.alignment=TextAnchor.MiddleCenter;name.supportRichText=false;
        }
        private void Menu()
        {
            Frame();quest=null;
            var viewport=ClassicWindowSkin.Fill("NpcChoicesViewport",contents,TextX,23,341,129,Color.clear,true).rectTransform;
            viewport.gameObject.AddComponent<RectMask2D>();
            var body=ClassicUI.Rect("NpcChoicesContent",viewport,341,129);ClassicUI.Place(body,0,0,341,129);
            string ambient=NXAssetLoader.Instance.GetNxFile("string")?.GetNode("Npc.img/"+npc.NpcId+"/n0")?.GetValue<string>();
            var greeting=ClassicWindowSkin.Label("NpcGreeting",body,font,string.IsNullOrEmpty(ambient)?"Choose a quest or an available service.":ClassicQuestText.Read(world,null,ambient),2,0,337,48,12);
            float choiceY=Mathf.Max(48,greeting.preferredHeight)+6;greeting.rectTransform.sizeDelta=new Vector2(337,choiceY-6);
            var choices=world.Quests.Definitions.Where(q=>(q.Start.Npc==npc.NpcId && world.Quests.CanStart(q.Id)) ||
                (world.Quests.State(q.Id)==QuestState.InProgress && (q.Start.Npc==npc.NpcId || q.Finish.Npc==npc.NpcId))).ToArray();
            int i=0;
            foreach(var q in choices)
            {
                var button=ClassicWindowSkin.Fill("NpcQuest_"+q.Id,body,2,choiceY+i*32,337,29,ClassicWindowSkin.List,true);
                string prefix=world.Quests.State(q.Id)==QuestState.Available?"[Available] ":world.Quests.CanFinish(q.Id)?"[Complete] ":"[In progress] ";
                var title=ClassicWindowSkin.Label("QuestChoice",button.transform,font,prefix+q.Name,4,1,329,27,11);title.supportRichText=false;
                var b=button.gameObject.AddComponent<Button>();b.targetGraphic=button;b.onClick.AddListener(()=>SelectQuest(q.Id));i++;
            }
            if(world.CanUseShop(npc))ClassicWindowSkin.Action("NpcOpenShop",body,font,"Buy / sell items",2,choiceY+i++*32,173,()=>{Close();GetComponent<LocalPlayMenu>()?.OpenShop();});
            var delivery=choices.FirstOrDefault(q=>world.Quests.State(q.Id)==QuestState.InProgress && q.Start.Npc==npc.NpcId &&
                q.OnStart.Items.Any(item=>item.Value>0 && world.Player.GetItemInfo(item.Key)?.IsQuest==true && world.Player.Inventory.GetItemCount(item.Key)<item.Value));
            if(delivery!=null)ClassicWindowSkin.Action("NpcRecoverDelivery",body,font,"Replace missing delivery item",2,choiceY+i++*32,228,()=>{
                quest=delivery;if(!world.RecoverQuestDelivery(npc,delivery.Id,out var message)){SayError(message);return;}Post(message);Say("1/lost",message,Menu,false);
            });
            if(i==0)
            {
                string message="There are no available offline quests or services here.";
                var locked=world.Quests.Definitions.FirstOrDefault(q=>q.Start.Npc==npc.NpcId && world.Quests.State(q.Id)==QuestState.Available);
                if(locked!=null)message=locked.Name+"\nRequires level "+locked.Start.MinLevel+(locked.Start.MaxLevel<200?"–"+locked.Start.MaxLevel:"+")+" and its quest prerequisites.";
                var empty=ClassicWindowSkin.Label("NpcNoQuest",body,font,message,2,choiceY,337,70,12);
                float emptyHeight=Mathf.Max(70,empty.preferredHeight);choiceY+=emptyHeight;empty.rectTransform.sizeDelta=new Vector2(337,emptyHeight);
            }
            body.sizeDelta=new Vector2(341,Mathf.Max(129,choiceY+i*32));
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=body;
            scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=20;
            scroll.verticalScrollbar=ClassicWindowSkin.Scrollbar("NpcChoicesScroll",contents,TextX+346,23,129,null,true);
            scroll.verticalScrollbar.GetComponent<ClassicScrollbarSkin>().ContentScroll=scroll;
            scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
            ClassicWindowSkin.Action("NpcQuestJournal",contents,font,"Quest journal",393,187,126,()=>GetComponent<ClassicQuestView>()?.Show(true));
        }
        public void SelectQuest(int id)
        {
            var definition=world.Quests.Get(id);if(definition==null || !world.CanTalkToNpc(npc))return;
            quest=definition;
            if(world.Quests.CanStart(id) && quest.Start.Npc==npc.NpcId)
                Say("0",quest.Descriptions[0],()=>{
                    if(!world.StartQuest(npc,id,out var message)){SayError(message);return;}Post(message);Say("0/yes",quest.Descriptions[1],Close,false);
                },true);
            else if(world.Quests.State(id)==QuestState.InProgress)
            {
                if(quest.Finish.Npc!=npc.NpcId)Say("1/stop/npc","Return to "+ClassicQuestText.Npc(quest.Finish.Npc)+".\n"+ClassicQuestText.Objectives(world,quest),Menu,false);
                else if(world.Quests.CanFinish(id))Say("1","Your objectives are complete. Claim your reward?",()=>{
                    if(!world.FinishQuest(npc,id,out var message)){SayError(message);return;}Post(message);Say("1/yes",quest.Descriptions[2],Close,false);
                },true);
                else Say(quest.Finish.Items.Count>0?"1/stop/item":"1/stop/mob",ClassicQuestText.Objectives(world,quest),Menu,false);
            }
        }
        private void SayError(string message){Post(message);pages=new[]{ClassicTooltipView.Escape(message)};page=0;finalAction=Menu;question=false;DrawPage();}
        private void Say(string branch,string fallback,Action onEnd,bool ask)
        {
            string[] raw=quest.Dialogue.TryGetValue(branch,out var source) && source.Length>0?source:new[]{fallback};
            pages=raw.Select(s=>ClassicQuestText.Read(world,quest,s)).ToArray();page=0;finalAction=onEnd;question=ask;DrawPage();
        }
        private void DrawPage()
        {
            Frame();ClassicQuestView.ScrollText("NpcSpeech",contents,font,pages[page],TextX,24,361,128,13);
            ClassicWindowSkin.Label("NpcPage",contents,font,(page+1)+" / "+pages.Length,147,189,88,16,10);
            if(page>0)ClassicUI.ArtButton("NpcPrevious",contents,"UIWindow.img/UtilDlgEx/BtPrev",363,187,()=>{page--;DrawPage();});
            if(page+1<pages.Length)ClassicUI.ArtButton("NpcNext",contents,"UIWindow.img/UtilDlgEx/BtNext",474,187,()=>{page++;DrawPage();});
            else
            {
                if(question)ClassicUI.ArtButton("NpcNo",contents,"UIWindow.img/UtilDlgEx/BtNo",474,187,()=>{if(world.Quests.State(quest.Id)==QuestState.Available)Say("0/no","Maybe another time.",Menu,false);else Menu();});
                ClassicUI.ArtButton(question?"NpcYes":"NpcOkay",contents,"UIWindow.img/UtilDlgEx/"+(question?"BtYes":"BtOK"),question?419:474,187,()=>finalAction?.Invoke());
            }
        }
        private void Post(string text)=>GetComponent<StatusBar>()?.PostMessage(text);
        private void OnDisable()=>Close();
        private void OnDestroy(){Close();if(font!=null)Destroy(font);}
    }
}
