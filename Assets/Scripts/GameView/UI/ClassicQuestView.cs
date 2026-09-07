using System.Collections.Generic;
using System.Linq;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Data;
using UnityEngine;
using UnityEngine.UI;

namespace MapleClient.GameView.UI
{
    /// <summary>The original Quest journal and a separate objective helper, backed by the offline log.</summary>
    public sealed class ClassicQuestView : MonoBehaviour
    {
        private GameWorld world;
        private Font font;
        private RectTransform panel,contents,helper,helperContents;
        private int selected,tab,offset;
        private bool dirty=true,confirmAbandon,helperCollapsed,helperHidden;
        private ClassicTooltipView tooltip;
        private const string AutoPreference = "ClassicUI.QuestHelper.AutoTrackAccepted";
        public bool Visible => panel!=null && panel.gameObject.activeSelf;
        public bool HelperVisible => helper!=null && helper.gameObject.activeSelf;
        public int SelectedQuest => selected;
        public int SelectedTab => tab;
        public void Bind(GameWorld value)
        {
            if(world!=null)world.Quests.Changed-=MarkDirty;
            world=value;world.Quests.Changed+=MarkDirty;
            if(!Application.isBatchMode)world.Quests.SetAutoTrackAccepted(PlayerPrefs.GetInt(AutoPreference,1)!=0);
            if(panel==null)
            {
                font=Font.CreateDynamicFontFromOSFont("Arial",12);
                tooltip=ClassicTooltipView.For(transform);
                panel=ClassicUI.Window("QuestPanel",transform,550,396);
                panel.GetComponent<ClassicWindow>().Configure(new Vector2(.52f,.57f),()=>Show(false));
                panel.gameObject.SetActive(false);
                helper=ClassicUI.Rect("QuestHelperPanel",transform,223,25);
                helper.gameObject.AddComponent<ClassicQuestHelperBounds>();
            }
            dirty=true;
        }
        private void MarkDirty()=>dirty=true;
        private void Update()
        {
            if(world==null)return;
            if(dirty){dirty=false;if(Visible)Rebuild();RebuildHelper();}
        }
        public void Show(bool value)
        {
            if(panel==null)return;
            panel.gameObject.SetActive(value);
            if(value){Rebuild();panel.GetComponent<ClassicWindow>().Focus();}
        }
        public void ShowHelper(bool value)
        {
            helperHidden=!value;dirty=true;tooltip?.Hide();
            if(!value && helper!=null)helper.gameObject.SetActive(false);
        }
        public void OpenQuest(int id)
        {
            if(world.Quests.Get(id)==null)return;
            selected=id;tab=(int)world.Quests.State(id);offset=0;confirmAbandon=false;Show(true);
        }
        public void SelectTab(int value)
        {tab=Mathf.Clamp(value,0,2);selected=0;offset=0;confirmAbandon=false;dirty=true;}
        private List<QuestDefinition> Entries()=>world.Quests.Definitions.Where(q=>tab==0?world.Quests.CanStart(q.Id):(int)world.Quests.State(q.Id)==tab).ToList();
        private static void Clear(ref RectTransform target)
        {if(target==null)return;target.gameObject.SetActive(false);Destroy(target.gameObject);target=null;}
        private void Rebuild()
        {
            Clear(ref contents);contents=ClassicUI.Rect("QuestContents",panel,550,396);ClassicUI.Place(contents,0,0,550,396);
            ClassicUI.Art("QuestListFrame",contents,"UIWindow.img/Quest/backgrnd",0,0).raycastTarget=true;
            ClassicUI.Art("QuestDetailFrame",contents,"UIWindow.img/Quest/backgrnd2",245,0).raycastTarget=true;
            ClassicUI.DragTitle(contents,panel,550);
            ClassicUI.ArtButton("CloseQuest",contents,"Basic.img/BtClose",530,6,()=>Show(false));
            string[] tabs={"Available","In Progress","Completed"};
            for(int i=0;i<3;i++)
            {
                int index=i;var button=ClassicUI.Button("QuestTab_"+i,contents,font,tabs[i],()=>SelectTab(index));
                ClassicUI.Place(button.GetComponent<RectTransform>(),5+i*78,24,78,19);button.GetComponentInChildren<Text>().fontSize=10;ClassicUI.TabState(button.transform,tab==i);
            }
            var entries=Entries();if(!entries.Any(q=>q.Id==selected))selected=entries.FirstOrDefault()?.Id ?? 0;
            offset=Mathf.Clamp(offset,0,Mathf.Max(0,entries.Count-7));
            ClassicWindowSkin.Label("QuestCategory",contents,font,tab==0?"Quests you can start":tab==1?"Your current quests":"Your completed quests",10,51,222,18,11,ClassicWindowSkin.Ink,true);
            for(int i=offset;i<entries.Count && i<offset+7;i++)
            {
                var q=entries[i];float y=76+(i-offset)*39;
                var row=ClassicWindowSkin.Fill("QuestRow_"+q.Id,contents,8,y,218,37,q.Id==selected?new Color32(245,227,168,255):ClassicWindowSkin.List,true);
                var b=row.gameObject.AddComponent<Button>();b.targetGraphic=row;b.onClick.AddListener(()=>{selected=q.Id;confirmAbandon=false;dirty=true;});
                var label=ClassicWindowSkin.Label("QuestName",row.transform,font,q.Name,5,2,209,31,11);label.supportRichText=false;
            }
            if(entries.Count==0)ClassicWindowSkin.Label("QuestEmpty",contents,font,tab==0?"No available quests for your level and job.\n\nBruce's mushroom studies begin at level 10 in Henesys.":tab==1?"Speak to an NPC to accept a quest.":"Completed quests will appear here.",12,83,218,165,12);
            var scrollbar=ClassicWindowSkin.Scrollbar("QuestListScroll",contents,229,90,253,v=>{offset=Mathf.RoundToInt((1-v)*Mathf.Max(0,Entries().Count-7));dirty=true;});
            scrollbar.size=Mathf.Min(1,7f/Mathf.Max(1,entries.Count));scrollbar.SetValueWithoutNotify(entries.Count<=7?1:1-offset/(float)(entries.Count-7));
            scrollbar.interactable=entries.Count>7;scrollbar.handleRect.gameObject.SetActive(entries.Count>7);
            ClassicWindowSkin.ScrollArrow("QuestListUp",contents,229,76,true,()=>{offset--;dirty=true;}).interactable=offset>0;
            ClassicWindowSkin.ScrollArrow("QuestListDown",contents,229,345,false,()=>{offset++;dirty=true;}).interactable=offset<entries.Count-7;
            contents.gameObject.AddComponent<ClassicScrollWheel>().Scroll=d=>{offset+=d;dirty=true;};
            var helperToggle=ClassicWindowSkin.Action("QuestHelperToggle",contents,font,helperHidden?"Show helper":"Hide helper",9,373,125,()=>ShowHelper(helperHidden));
            helperToggle.interactable=world.Quests.Definitions.Any(q=>world.Quests.State(q.Id)==QuestState.InProgress);
            var selectedDefinition=world.Quests.Get(selected);
            if(selectedDefinition==null)
            {
                ClassicWindowSkin.Label("QuestDetailEmpty",contents,font,"Select a quest to view its story, objectives and rewards.",268,39,260,70,13,Color.white);
                return;
            }
            var state=world.Quests.State(selected);var definition=selectedDefinition;
            // Retain the blue source header, but cover its unused divider behind a continuous title.
            ClassicWindowSkin.Fill("QuestHeaderBacking",contents,263,32,279,83,new Color32(70,137,181,255));
            var title=ClassicWindowSkin.Label("QuestDetailTitle",contents,font,definition.Name,269,37,265,43,13,Color.white,true);title.supportRichText=false;
            ClassicWindowSkin.Label("QuestNpc",contents,font,ClassicQuestText.Npc(state==QuestState.Available?definition.Start.Npc:definition.Finish.Npc),270,88,263,19,12,Color.white);
            string description=ClassicQuestText.Read(world,definition,definition.Descriptions[(int)state]);
            string body=description+"\n\n<b>Objectives</b>\n"+ClassicTooltipView.Escape(ClassicQuestText.Objectives(world,definition))+"\n\n<b>Reward</b>\n"+ClassicTooltipView.Escape(ClassicQuestText.Rewards(world,definition));
            if(state==QuestState.Available)body="Speak to "+ClassicTooltipView.Escape(ClassicQuestText.Npc(definition.Start.Npc))+" to begin.\n\n"+body;
            ScrollText("QuestStory",contents,font,body,262,129,278,228);
            if(state==QuestState.InProgress)
            {
                ClassicWindowSkin.Action("QuestTrack",contents,font,world.Quests.IsTracked(selected)?"Untrack":"Track quest",258,373,95,()=>{
                    bool track=!world.Quests.IsTracked(selected);
                    if(world.Quests.SetTracked(selected,track,out var m) && track)ShowHelper(true);Post(m);
                });
                ClassicWindowSkin.Action("QuestAbandon",contents,font,confirmAbandon?"Confirm abandon":"Abandon",361,373,116,()=>{
                    if(!confirmAbandon){confirmAbandon=true;dirty=true;return;}world.Quests.Abandon(selected,out var m);confirmAbandon=false;Post(m);
                });
                if(confirmAbandon)ClassicWindowSkin.Action("CancelQuestAbandon",contents,font,"Cancel",482,373,58,()=>{confirmAbandon=false;dirty=true;});
            }
            else ClassicWindowSkin.Label("QuestState",contents,font,state==QuestState.Completed?"Quest completed":"Level "+definition.Start.MinLevel+"+  •  Speak to the NPC to accept",260,374,278,15,10);
        }
        internal static void ScrollText(string name,Transform parent,Font font,string text,float x,float y,float w,float h,int fontSize=12)
        {
            var viewport=ClassicWindowSkin.Fill(name,parent,x,y,w-18,h,Color.clear,true).rectTransform;viewport.gameObject.AddComponent<RectMask2D>();
            var label=ClassicWindowSkin.Label("Text",viewport,font,text,2,0,w-23,1,fontSize);
            float height=Mathf.Max(h,label.preferredHeight+4);label.rectTransform.sizeDelta=new Vector2(w-23,height);
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=label.rectTransform;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=20;
            var bar=ClassicWindowSkin.Scrollbar(name+"Scroll",parent,x+w-15,y,h,null,true);scroll.verticalScrollbar=bar;
            bar.GetComponent<ClassicScrollbarSkin>().ContentScroll=scroll;
            scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
        }
        private void RebuildHelper()
        {
            Clear(ref helperContents);var tracked=world.Quests.Definitions.Where(q=>world.Quests.IsTracked(q.Id)).ToArray();
            bool active=world.Quests.Definitions.Any(q=>world.Quests.State(q.Id)==QuestState.InProgress);
            helper.gameObject.SetActive(active && !helperHidden);if(!active || helperHidden)return;
            // Keep the controls reachable when AUTO is off and no active quest is tracked.
            bool compact=helperCollapsed || tracked.Length==0;
            helperContents=ClassicUI.Rect("QuestHelperContents",helper,223,25);ClassicUI.Place(helperContents,0,0,223,25);
            var header=ClassicUI.Art("HelperTitleFrame",helperContents,"UIWindow.img/QuestAlarm/"+(compact?"backgrndmin":"backgrndmax"),0,0);header.raycastTarget=true;
            header.gameObject.AddComponent<ClassicQuestHelperDrag>().Panel=helper.GetComponent<ClassicQuestHelperBounds>();
            ClassicWindowSkin.Label("HelperTitle",helperContents,font,"Quest Helper ("+tracked.Length+"/"+OfflineQuestLog.TrackingLimit+")",5,1,159,18,12);
            const string autoPath="UIWindow.img/QuestAlarm/BtAuto";
            var auto=ClassicUI.ArtButton("QuestHelperAuto",helperContents,autoPath,168,3,ToggleAutoTracking);
            if(!world.Quests.AutoTrackAccepted)auto.GetComponent<Image>().sprite=ClassicUI.Sprite(autoPath+"/disabled/0");
            Hint(auto,"Auto tracking: "+(world.Quests.AutoTrackAccepted?"ON":"OFF"),
                "Automatically add newly accepted quests to this helper, up to five. Existing tracking choices stay unchanged.");
            var close=ClassicUI.ArtButton("QuestHelperClose",helperContents,"Basic.img/BtClose",207,3,()=>ShowHelper(false));
            Hint(close,"Hide quest helper","Keep tracked quests. Use Show helper in the quest journal to reopen this panel.");
            var collapse=ClassicUI.ArtButton("QuestHelperCollapse",helperContents,"Basic.img/"+(compact?"BtMax":"BtMin"),191,3,()=>{helperCollapsed=!helperCollapsed;tooltip.Hide();dirty=true;});
            collapse.interactable=tracked.Length>0;
            Hint(collapse,helperCollapsed?"Expand helper":"Minimize helper","Keep tracking quests while hiding their objectives.");
            float y=compact?20:24;
            if(!compact)foreach(var q in tracked)
            {
                string objectives=string.Join("\n",q.Finish.Mobs.Select(m=>$"{world.Quests.Kills(q.Id,m.Key)}/{m.Value} {ClassicQuestText.Mob(m.Key)}")
                    .Concat(q.Finish.Items.Select(i=>$"{world.Player.Inventory.GetItemCount(i.Key)}/{i.Value} {ClassicQuestText.Item(world,i.Key)}")));
                if(world.Quests.CanFinish(q.Id))objectives+=(objectives.Length>0?"\n":"")+"Return to "+ClassicQuestText.Npc(q.Finish.Npc);
                var title=ClassicWindowSkin.Label("TrackedQuest_"+q.Id,helperContents,font,q.Name,8,y+3,191,1,12,Color.black,true);title.supportRichText=false;
                float th=Mathf.Max(17,title.preferredHeight);title.rectTransform.sizeDelta=new Vector2(191,th);
                var text=ClassicWindowSkin.Label("TrackedObjectives_"+q.Id,helperContents,font,objectives,8,y+th+6,207,1,12,Color.black);text.supportRichText=false;
                float bh=Mathf.Max(17,text.preferredHeight);text.rectTransform.sizeDelta=new Vector2(207,bh);float height=th+bh+17;
                var backing=ClassicUI.Art("HelperBody",helperContents,"UIWindow.img/QuestAlarm/backgrndcenter",0,y);
                backing.rectTransform.sizeDelta=new Vector2(223,height);backing.raycastTarget=true;backing.transform.SetSiblingIndex(0);
                var button=backing.gameObject.AddComponent<Button>();button.targetGraphic=backing;button.onClick.AddListener(()=>OpenQuest(q.Id));
                var remove=ClassicUI.ArtButton("QuestHelperRemove_"+q.Id,helperContents,"Basic.img/BtClose2",203,y+4,()=>{world.Quests.SetTracked(q.Id,false,out var message);tooltip.Hide();Post(message);});
                Hint(remove,"Stop tracking",q.Name+" stays active in your quest journal.");
                y+=height;
            }
            if(!compact)ClassicUI.Art("HelperBottom",helperContents,"UIWindow.img/QuestAlarm/backgrndbottom",0,y);
            helper.sizeDelta=helperContents.sizeDelta=new Vector2(223,y+(compact?0:5));
            helper.GetComponent<ClassicQuestHelperBounds>().Layout();
        }
        private void ToggleAutoTracking()
        {
            world.Quests.SetAutoTrackAccepted(!world.Quests.AutoTrackAccepted);tooltip.Hide();
            if(!Application.isBatchMode)
            {PlayerPrefs.SetInt(AutoPreference,world.Quests.AutoTrackAccepted?1:0);PlayerPrefs.Save();}
        }
        private void Hint(Button button,string title,string description)
        {
            var hover=button.gameObject.AddComponent<ClassicHover>();
            hover.Enter=e=>tooltip.ShowText(button,e.position,title,ClassicTooltipView.Escape(description));hover.Exit=()=>tooltip.Hide(button);
        }
        private void Post(string message)=>GetComponent<StatusBar>()?.PostMessage(message);
        private void OnDestroy(){if(world!=null)world.Quests.Changed-=MarkDirty;if(font!=null)Destroy(font);}
    }
}
