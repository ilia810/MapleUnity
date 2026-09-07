using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MapleClient.GameData;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameLogic.Skills;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace MapleClient.GameView.UI
{
    public class SkillMenu : MonoBehaviour
    {
        private GameWorld world;private SkillManager skills;private Player player;private Font font;
        private RectTransform panel,book,actions;private ClassicTooltipView tooltip;
        private Text points,job,pageLabel;private Image jobIcon;private Button previous,next,cast,spend;private Scrollbar scroll;
        private int offset,selected,tier=1,maxOffset;private bool dirty=true;
        private readonly List<GameObject> rows=new List<GameObject>();private readonly List<Button> tabs=new List<Button>();
        public GameObject SelectedRow { get; private set; }
        public bool Visible=>panel!=null&&panel.gameObject.activeSelf;
        public int ScrollOffset=>offset;
        private IEnumerator Start()
        {
            CreateUI();GameManager manager;while((manager=FindFirstObjectByType<GameManager>())==null||manager.World==null)yield return null;
            world=manager.World;skills=world.SkillManager;SetPlayer(world.Player);if(skills!=null)skills.SkillsChanged+=Changed;
        }
        public void SetPlayer(Player value)
        { if(player!=null)player.ProgressionChanged-=Changed;player=value;if(player!=null){player.ProgressionChanged+=Changed;tier=SkillRules.JobTier(player.JobId);}dirty=true; }
        private void Changed()=>dirty=true;
        public void Show(bool visible)
        { if(panel==null)return;panel.gameObject.SetActive(visible);if(visible)panel.GetComponent<ClassicWindow>().Focus();else{actions.gameObject.SetActive(false);tooltip.Hide();}dirty=true; }
        private void CreateUI()
        {
            font=Font.CreateDynamicFontFromOSFont("Arial",12);tooltip=ClassicTooltipView.For(transform);
            ClassicUI.ArtButton("SkillsToggle",ClassicUI.Hud(transform),"StatusBar.img/SkillKey",706,5,()=>Show(!Visible));
            panel=ClassicUI.Window("SkillPanel",transform,196,370);panel.GetComponent<ClassicWindow>().Configure(new Vector2(.75f,.62f),()=>Show(false));
            book=ClassicWindowSkin.Surface("SkillBook",panel,196,370,font,"SKILL INVENTORY","UIWindow.img/Skill/backgrnd");ClassicUI.DragTitle(book,panel,196);
            ClassicUI.ArtButton("CloseSkills",book,"Basic.img/BtClose",180,5,()=>Show(false));
            for(int i=0;i<5;i++)
            { int value=i;var tab=ClassicUI.Button("SkillTier_"+i,book,font,"",()=>{tier=value;offset=0;selected=0;actions.gameObject.SetActive(false);tooltip.Hide();dirty=true;});ClassicUI.Place(tab.GetComponent<RectTransform>(),5+i*32,24,31,19);ClassicUI.Art("TierLabel",tab.transform,"UIWindow.img/Skill/Tab/enabled/"+i,10,3);tabs.Add(tab); }
            ClassicWindowSkin.Fill("TabBaseline",book,4,44,188,2,new Color32(205,24,31,255));
            // Extend only the empty header stripe; keep the original frame/book pixels native.
            ClassicUI.Region("JobHeaderLeft",book,"UIWindow.img/Skill/backgrnd",new Rect(5,52,37,38),7,53,37,38);
            ClassicUI.Region("JobHeaderMiddle",book,"UIWindow.img/Skill/backgrnd",new Rect(60,52,1,38),44,53,140,38);
            ClassicUI.Region("JobHeaderRight",book,"UIWindow.img/Skill/backgrnd",new Rect(164,52,6,38),184,53,6,38);
            ClassicWindowSkin.Fill("BookPaper",book,10,56,29,30,new Color32(218,220,221,255));
            jobIcon=ClassicWindowSkin.Fill("BookCover",book,11,56,26,30,Color.white);jobIcon.preserveAspect=true;
            job=ClassicWindowSkin.Label("SkillJob",book,font,"Beginner Basics",45,62,135,19,12,Color.white);
            job.alignment=TextAnchor.MiddleLeft;job.resizeTextForBestFit=true;job.resizeTextMinSize=10;job.resizeTextMaxSize=12;
            ClassicUI.Region("SkillPointLabel",book,"UIWindow.img/Skill/backgrnd",new Rect(7,267,73,14),7,346,73,14);
            ClassicWindowSkin.Fill("SkillPointField",book,83,345,29,18,Color.white);points=ClassicWindowSkin.Label("SkillPoints",book,font,"0",85,345,25,18);points.alignment=TextAnchor.MiddleRight;
            ClassicWindowSkin.Label("SkillHelp",book,font,"Right-click: actions",116,349,73,12,8,Color.white);
            previous=Arrow("PreviousSkills",179,99,"prev",()=>Shift(-1));next=Arrow("NextSkills",179,323,"next",()=>Shift(1));
            scroll=ClassicWindowSkin.Scrollbar("SkillScroll",book,179,113,208,v=>{offset=Mathf.RoundToInt((1-v)*maxOffset);dirty=true;});
            book.gameObject.AddComponent<ClassicScrollWheel>().Scroll=Shift;
            actions=ClassicUI.Window("SkillActions",transform,228,140);actions.GetComponent<ClassicWindow>().CloseAction=()=>actions.gameObject.SetActive(false);actions.gameObject.SetActive(false);
            actions.GetComponent<ClassicWindow>().DismissOnOutsideClick=true;
            panel.gameObject.SetActive(false);
        }
        private Button Arrow(string name,int x,int y,string direction,UnityEngine.Events.UnityAction click)
        {
            return ClassicWindowSkin.ScrollArrow(name,book,x,y,direction=="prev",click);
        }
        private void Shift(int amount){offset=Mathf.Clamp(offset+amount,0,maxOffset);dirty=true;}
        public static int SkillTier(int id){int job=id/10000;return job==0?0:job%100==0?1:job%10==0?2:job%10==1?3:4;}
        private void Refresh()
        {
            foreach(var row in rows){row.SetActive(false);Destroy(row);}rows.Clear();SelectedRow=null;
            int highest=player==null?0:SkillRules.JobTier(player.JobId);tier=Mathf.Clamp(tier,0,highest);
            var entries=skills?.GetAvailableSkills().Values.Where(s=>!s.IsInvisible&&SkillRules.JobTier(s.JobId)==tier).OrderBy(s=>s.SkillId).ToList()??new List<SkillInfo>();
            maxOffset=Mathf.Max(0,entries.Count-6);offset=Mathf.Clamp(offset,0,maxOffset);scroll.size=entries.Count==0?1:Mathf.Min(1,6f/entries.Count);scroll.SetValueWithoutNotify(maxOffset==0?1:1-offset/(float)maxOffset);scroll.interactable=maxOffset>0;
            previous.interactable=offset>0;next.interactable=offset<maxOffset;
            int bookJob=tier==0?0:entries.FirstOrDefault()?.JobId??player?.JobId??0;
            string bookPath=bookJob.ToString("D3")+".img/info/icon";
            jobIcon.sprite=MapleClient.GameData.SpriteLoader.LoadSprite(NXAssetLoader.Instance.GetNxFile("skill")?.GetNode(bookPath),"skill/"+bookPath);
            jobIcon.enabled=jobIcon.sprite!=null;
            job.horizontalOverflow=HorizontalWrapMode.Overflow;
            ClassicMessageText.Fit(job,StatusBar.SkillBookName(bookJob),false);points.text=(player?.SkillPoints??0).ToString();
            for(int i=0;i<tabs.Count;i++)
            {
                tabs[i].gameObject.SetActive(i<=highest);ClassicUI.TabState(tabs[i].transform,tier==i);
                ClassicUI.NativeCaption(tabs[i].transform.Find("TierLabel").GetComponent<Image>(),ClassicUI.Sprite("UIWindow.img/Skill/Tab/"+(tier==i?"enabled/":"disabled/")+i),31,19);
            }
            int index=0;
            foreach(var entry in entries.Skip(offset).Take(6))
            {
                int id=entry.SkillId,level=skills.GetSkillLevel(id);float y=99+40*index++;
                var bg=ClassicWindowSkin.Fill("SkillRow_"+id,book,8,y,169,36,selected==id?new Color32(180,205,222,255):ClassicWindowSkin.List,true);
                var drag=bg.gameObject.AddComponent<ClassicQuickslotDrag>();drag.Owner=GetComponent<SkillBar>();drag.SkillId=id;
                var row=bg.rectTransform;rows.Add(row.gameObject);row.gameObject.AddComponent<Button>().onClick.AddListener(()=>Select(row,id));
                ClassicWindowSkin.Fill("IconBacking",row,0,0,33,35,ClassicWindowSkin.Paper);
                var icon=ClassicWindowSkin.Fill("Icon",row,0,1,32,32,Color.white);icon.sprite=SkillSprites.Icon(entry);icon.preserveAspect=true;
                var name=ClassicWindowSkin.Label("SkillName",row,font,entry.Name,37,1,128,17,12);name.supportRichText=false;name.horizontalOverflow=HorizontalWrapMode.Overflow;ClassicMessageText.Fit(name,entry.Name,false);
                ClassicWindowSkin.Label("SkillLevel",row,font,level.ToString(),37,19,94,16,12);
                ClassicWindowSkin.Fill("RowLine",row,35,17,134,1,Color.white);
                var plus=ClassicUI.ArtButton("SpendSkillPoint_"+id,row,"UIWindow.img/Skill/BtSpUp",154,21,()=>{selected=id;Spend();});plus.interactable=skills.CanSpendSkillPoint(id,out _);
                var hover=row.gameObject.AddComponent<ClassicHover>();hover.Enter=e=>{if(!actions.gameObject.activeSelf)ShowDescription(row,id,e.position);};hover.Exit=()=>tooltip.Hide(row);
                row.gameObject.AddComponent<ClassicItemClick>().Right=e=>{Select(row,id);OpenActions(e.position);};
                if(selected==id)SelectedRow=row.gameObject;
            }
            if(pageLabel!=null)pageLabel.text=(offset+1)+" / "+(maxOffset+1);dirty=false;
        }
        private string Description(int id)
        {
            if(skills==null||!skills.GetAvailableSkills().TryGetValue(id,out var info))return "";
            int level=skills.GetSkillLevel(id);var data=info.Levels[level>0?level:1];
            string text="Level "+level+" / "+info.MaxLevel+"\n";
            var behavior=info.Behavior;
            if(info.IsPassive)
            {
                text+=behavior.Available?"Passive — applies when its conditions match.":"Passive effect not available yet.";
                var passive=data.Passive;
                if(behavior.Available && passive!=null)
                {
                    if(passive.WeaponAttack!=0)text+="\n"+BuffDescription.Describe(BuffType.WeaponAttack,passive.WeaponAttack);
                    if(passive.MagicAttack!=0)text+="\n"+BuffDescription.Describe(BuffType.MagicAttack,passive.MagicAttack);
                    if(passive.Accuracy!=0)text+="\n"+BuffDescription.Describe(BuffType.Accuracy,passive.Accuracy);
                    if(passive.Avoidability!=0)text+="\n"+BuffDescription.Describe(BuffType.Avoidability,passive.Avoidability);
                    if(passive.ProjectileRangeBonus!=0)text+=$"\nProjectile range +{passive.ProjectileRangeBonus} px";
                    if(passive.CriticalChance.HasValue)text+=$"\nCritical chance {passive.CriticalChance.Value*100:0.#}%";
                    if(passive.CriticalDamageMultiplier.HasValue)text+=$"\nCritical damage {passive.CriticalDamageMultiplier.Value*100:0.#}%";
                }
            }
            else if(!behavior.Available)text+="Skill action not available yet.";
            else
            {
                text+=$"HP {data.HpCost} / MP {data.MpCost}";
                if(behavior.IsAttack)
                {
                    int hits=behavior.UsesAmmunition?data.BulletCount:data.AttackCount;
                    if(behavior.UsesAmmunition)text+=$" / Ammo {data.BulletConsume}";
                    text+=behavior.DamagePolicy==SkillDamagePolicy.None?$" · {data.MobCount} targets · No damage":$" · {hits} hits · {data.MobCount} targets";
                    if(behavior.DamagePolicy==SkillDamagePolicy.FixedMagic||behavior.DamagePolicy==SkillDamagePolicy.FixedPhysical)text+=$"\n{data.Damage} base damage";
                    else if(behavior.DamagePolicy!=SkillDamagePolicy.None&&behavior.AttackFamily!=SkillAttackFamily.Magic)text+=$" · {data.Damage}% damage";
                    if(data.TargetDebuff!=null)
                    {
                        var debuff=data.TargetDebuff;
                        text+=$"\nEnemy attack {debuff.PhysicalAttackChange}, defense {debuff.PhysicalDefenseChange} · {debuff.DurationMilliseconds/1000f:0.#}s";
                        if(debuff.ChancePercent<100)text+=$" · {debuff.ChancePercent}% chance on hit";
                        if(debuff.RejectSameSource)text+="\nCannot refresh an already affected enemy.";
                    }
                }
                if(behavior.CastEffects.Contains(SkillCastEffects.StatBuff))
                    text+=$" · {data.Duration/1000}s\n"+string.Join("\n",data.Buffs.Select(b=>BuffDescription.Describe(b.Key,b.Value)));
                if(behavior.CastEffects.Contains(SkillCastEffects.Recovery))
                    text+=$"\nRestore HP {data.Hp} + {data.HpR}% / MP {data.Mp} + {data.MpR}%";
            }
            if(info.RequiredSkills.Count>0)text+="\nRequires "+string.Join(", ",info.RequiredSkills.Select(r=>(skills.GetAvailableSkills().TryGetValue(r.Key,out var req)?req.Name:r.Key.ToString())+" "+r.Value));
            if(!skills.CanSpendSkillPoint(id,out var reason))text+="\n"+reason;return text;
        }
        private void ShowDescription(Object owner,int id,Vector2 point)
        { if(skills.GetAvailableSkills().TryGetValue(id,out var info))tooltip.ShowText(owner,point,info.Name,Description(id),null,"SkillDetails"); }
        private void Select(RectTransform row,int id)
        { selected=id;SelectedRow=row.gameObject;foreach(var r in rows)r.GetComponent<Image>().color=r==row.gameObject?new Color32(180,205,222,255):ClassicWindowSkin.List;ShowDescription(row,id,RectTransformUtility.WorldToScreenPoint(null,row.TransformPoint(row.rect.center))); }
        public void OpenSelectedActions()
        { if(SelectedRow!=null){var r=SelectedRow.GetComponent<RectTransform>();OpenActions(RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center)));} }
        private void OpenActions(Vector2 screen)
        {
            if(selected==0)return;tooltip.Hide();foreach(Transform child in actions){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            var surface=ClassicWindowSkin.Surface("SkillActionSurface",actions,228,140,font,"SKILL ACTIONS",null,false);
            cast=ClassicWindowSkin.Action("CastSkill",surface,font,"Cast",7,29,103,Cast);spend=ClassicWindowSkin.Action("SpendSkillPoint",surface,font,"+1 (1 SP)",118,29,103,Spend);
            ClassicWindowSkin.Label("AssignHint",surface,font,"Assign to quickslot",7,51,185,16,11);
            for(int i=0;i<8;i++){int slot=i;ClassicWindowSkin.Action("AssignSkill_"+i,surface,font,SkillBar.KeyLabel(i),7+i%4*54,71+i/4*24,51,()=>{GetComponent<SkillBar>()?.AssignSkillToSlot(selected,slot);GetComponent<StatusBar>()?.PostMessage("Assigned to "+SkillBar.KeyLabel(slot)+" / "+(slot+1)+".");},new Color32(91,147,187,255));}
            pageLabel=ClassicWindowSkin.Label("SkillPage",surface,font,"",7,123,80,12,9);pageLabel.text=(offset+1)+" / "+(maxOffset+1);
            ClassicUI.ArtButton("CloseSkillActions",surface,"Basic.img/BtClose",212,5,()=>actions.gameObject.SetActive(false));
            actions.gameObject.SetActive(true);RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,screen,null,out var point);actions.GetComponent<ClassicWindow>().SetPosition(point+new Vector2(118,-70));actions.GetComponent<ClassicWindow>().Focus();UpdateActions();
        }
        private void Spend(){if(skills!=null){skills.TrySpendSkillPoint(selected,out var result);GetComponent<StatusBar>()?.PostMessage(result);dirty=true;UpdateActions();}}
        private void Cast(){var result=skills?.UseSkill(selected);GetComponent<StatusBar>()?.PostMessage(result==null?"Skills are unavailable.":result.Success?"Cast skill.":result.ErrorMessage);UpdateActions();}
        private void UpdateActions()
        {
            if(!actions.gameObject.activeSelf||skills==null)return;
            bool learned=skills.GetSkillLevel(selected)>0;var selectedInfo=skills.GetSkillInfo(selected);cast.interactable=learned&&selectedInfo?.Behavior.Available==true&&!selectedInfo.IsPassive&&!player.IsDead&&!player.IsBasicAttacking&&player.State!=PlayerState.Climbing;
            spend.interactable=skills.CanSpendSkillPoint(selected,out _);
            foreach(var button in actions.GetComponentsInChildren<Button>())if(button.name.StartsWith("AssignSkill_"))button.interactable=learned&&skills.GetAvailableSkills().TryGetValue(selected,out var info)&&!info.IsPassive;
        }
        public void Practice(bool magic)
        {
            if(world==null)return;string result;bool success=magic?world.RequestPracticeMagicSkills(out result):world.RequestPracticeSkills(out result);
            if(success){selected=magic?2001004:1100000;tier=SkillTier(selected);offset=0;var bar=GetComponent<SkillBar>();bar?.AssignSkillToSlot(magic?2001004:1001004,0);bar?.AssignSkillToSlot(magic?2001005:1001005,1);bar?.AssignSkillToSlot(magic?0:1101004,2);}
            GetComponent<StatusBar>()?.PostMessage(result);Show(true);
        }
        public void PracticeSupport(int jobId)
        {
            if(world==null)return;
            if(world.RequestSupportPractice(jobId,out var result))
            {
                var ids=GameWorld.SupportPracticeSkills(jobId);selected=ids[0];tier=SkillTier(selected);offset=0;
                SetPlayer(world.Player);var bar=GetComponent<SkillBar>();
                for(int i=0;i<ids.Length;i++)bar?.AssignSkillToSlot(ids[i],i);
            }
            GetComponent<StatusBar>()?.PostMessage(result);Show(true);
        }
        private void Update(){if(panel==null)return;if(dirty&&Visible)Refresh();UpdateActions();}
        private void OnDestroy(){if(skills!=null)skills.SkillsChanged-=Changed;if(player!=null)player.ProgressionChanged-=Changed;if(font!=null)Destroy(font);}
    }
}
