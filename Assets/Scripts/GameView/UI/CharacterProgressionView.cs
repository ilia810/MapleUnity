using System.Collections;
using System.Collections.Generic;
using MapleClient.GameLogic.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MapleClient.GameView.UI
{
    public sealed class CharacterProgressionView : MonoBehaviour
    {
        private GameWorld world;private RectTransform panel,detailPanel,advancement;private CanvasGroup visibility;
        private Font font;private Text message,readout,detailHint,jobDetail;private bool details=true;
        private readonly Dictionary<string,Text> values=new Dictionary<string,Text>();
        private readonly Dictionary<PrimaryAttribute,Button> spendButtons=new Dictionary<PrimaryAttribute,Button>();
        private readonly Dictionary<int,Button> jobs=new Dictionary<int,Button>();
        public bool Visible=>visibility!=null&&visibility.alpha>0;
        private IEnumerator Start()
        { GameManager manager;while((manager=FindFirstObjectByType<GameManager>())==null||manager.World==null)yield return null;world=manager.World;CreateUI(); }
        public void Show(bool visible)
        { if(panel==null)return;visibility.alpha=visible?1:0;visibility.blocksRaycasts=visible;visibility.interactable=visible;if(visible)panel.GetComponent<ClassicWindow>().Focus(); }
        public void ShowAdvancement()
        { if(advancement==null)return;advancement.gameObject.SetActive(true);advancement.GetComponent<ClassicWindow>().Focus(); }
        private void CreateUI()
        {
            font=Font.CreateDynamicFontFromOSFont("Arial",12);
            panel=ClassicUI.Window("CombatStatsPanel",transform,373,282);panel.GetComponent<ClassicWindow>().Configure(new Vector2(.567f,.50f),()=>Show(false));
            visibility=panel.gameObject.AddComponent<CanvasGroup>();
            var main=ClassicWindowSkin.Surface("CharacterStatWindow",panel,186,282,font,"CHARACTER STAT","UIWindow.img/Stat/backgrnd");ClassicUI.DragTitle(main,panel,186);
            ClassicUI.ArtButton("CloseStats",main,"Basic.img/BtClose",170,5,()=>Show(false));
            Color pink=new Color32(193,78,133,255),green=new Color32(174,196,65,255),blue=new Color32(94,145,184,255);
            Row(main,"Name","NAME",30,17,pink);Row(main,"Job","JOB",48,28,pink);Row(main,"Level","LEVEL",77,17,pink);
            ClassicUI.Place(values["Job"].rectTransform,58,49,119,14);values["Job"].fontSize=11;
            jobDetail=ClassicWindowSkin.Label("StatJobDetail",main,font,"",58,63,119,12,9,new Color32(133,111,115,255));
            Row(main,"HP","HP",95,17,pink);Row(main,"MP","MP",113,17,pink);Row(main,"EXP","EXP",131,17,pink);Row(main,"Fame","FAME",149,17,pink);
            int i=0;foreach(PrimaryAttribute attribute in System.Enum.GetValues(typeof(PrimaryAttribute)))
            {
                var stat=attribute;float y=176+18*i++;Row(main,stat.ToString(),stat.ToString(),y,17,green);
                var button=ClassicUI.ArtButton("SpendAP_"+stat,main,"UIWindow.img/Stat/BtApUp",163,y+2,()=>{world.TrySpendAbilityPoint(stat,out var result);GetComponent<StatusBar>()?.PostMessage(result);});spendButtons[stat]=button;
                values[stat.ToString()].rectTransform.sizeDelta=new Vector2(102,17);
            }
            ClassicWindowSkin.Label("AbilityPointLabel",main,font,"ABILITY POINT",7,262,77,15,9,Color.white,true);
            ClassicWindowSkin.Fill("AbilityPointField",main,87,260,31,17,Color.white);values["AP"]=ClassicWindowSkin.Label("AbilityPoints",main,font,"0",88,260,28,17);values["AP"].alignment=TextAnchor.MiddleRight;
            ClassicUI.ArtButton("ToggleStatDetails",main,"UIWindow.img/Stat/BtDetail",130,259,()=>SetDetails(!details));
            detailPanel=ClassicUI.Rect("CharacterStatDetails",panel,187,221);ClassicUI.Place(detailPanel,186,61,187,221);
            ClassicWindowSkin.Fill("DetailPaper",detailPanel,3,3,181,215,new Color32(247,247,244,255),true);ClassicWindowSkin.Fill("DetailFooter",detailPanel,4,195,179,22,ClassicWindowSkin.Footer);ClassicWindowSkin.Border("DetailFrame",detailPanel,0,0,187,221);
            string[] keys={"Attack","WeaponDefense","Magic","MagicDefense","Accuracy","Evasion","CritRate","CritDamage","Speed","Jump"};
            string[] labels={"ATTACK","WEAPON DEF.","MAGIC","MAGIC DEF.","ACCURACY","EVASION","CRIT. RATE","CRIT. DAMAGE","SPEED","JUMP"};
            for(i=0;i<keys.Length;i++)Row(detailPanel,keys[i],labels[i],8+i*18,17,blue,62);
            detailHint=ClassicWindowSkin.Label("StatDetailHint",detailPanel,font,"Includes equipment and buffs",7,201,174,12,8,Color.white);
            ClassicUI.ArtButton("StatsToggle",ClassicUI.Hud(transform),"StatusBar.img/StatKey",676,5,()=>Show(!Visible));
            CreateAdvancement();Show(false);
        }
        private void Row(Transform parent,string key,string title,float y,float height,Color tint,float labelWidth=48)
        {
            ClassicWindowSkin.Fill(key+"LabelCell",parent,7,y,labelWidth,height,tint);ClassicWindowSkin.Fill(key+"ValueCell",parent,8+labelWidth,y,170-labelWidth,height,ClassicWindowSkin.Paper);
            var label=ClassicWindowSkin.Label(key+"Label",parent,font,title,10,y+1,labelWidth-4,height-2,9,Color.white);label.alignment=TextAnchor.MiddleLeft;label.horizontalOverflow=HorizontalWrapMode.Overflow;
            // These newer Classic labels have no bitmap in this older NX pack.
            // Keep a consistent cap height and condense the complete caption to its cell.
            float captionWidth=Mathf.Max(labelWidth-4,label.preferredWidth);
            label.rectTransform.sizeDelta=new Vector2(captionWidth,height-2);
            label.rectTransform.localScale=new Vector3((labelWidth-4)/captionWidth,1,1);
            if(NativeLabel(key,out var path,out var crop))
            {
                label.enabled=false;
                ClassicUI.Region(key+"LabelArt",parent,path,crop,7,y,crop.width,crop.height);
            }
            values[key]=ClassicWindowSkin.Label(key=="Attack"?"CombatStats":"Stat"+key,parent,font,"",10+labelWidth,y,167-labelWidth,height,12);values[key].alignment=TextAnchor.MiddleLeft;
            values[key].horizontalOverflow=HorizontalWrapMode.Overflow;values[key].supportRichText=false;
        }
        private static bool NativeLabel(string key,out string path,out Rect crop)
        {
            path="UIWindow.img/Stat/backgrnd";int sourceY=-1;
            switch(key)
            {
                case "Name":sourceY=32;break;case "Job":sourceY=50;break;case "Level":sourceY=78;break;
                case "HP":sourceY=114;break;case "MP":sourceY=132;break;case "EXP":sourceY=150;break;case "Fame":sourceY=168;break;
                case "STR":sourceY=244;break;case "DEX":sourceY=262;break;case "INT":sourceY=280;break;case "LUK":sourceY=298;break;
            }
            crop=new Rect(8,sourceY,47,key=="Job"?27:17);if(sourceY>=0)return true;
            path="UIWindow.img/Stat/backgrnd3";
            switch(key){case "Attack":sourceY=7;break;case "WeaponDefense":sourceY=25;break;case "Magic":sourceY=43;break;case "MagicDefense":sourceY=61;break;case "Accuracy":sourceY=79;break;case "Speed":sourceY=151;break;case "Jump":sourceY=169;break;}
            crop=new Rect(13,sourceY,61,17);return sourceY>=0;
        }
        private void SetDetails(bool show)
        {
            float width=show?373:186,delta=width-panel.rect.width;details=show;detailPanel.gameObject.SetActive(show);panel.sizeDelta=new Vector2(width,282);panel.anchoredPosition+=new Vector2(delta/2,0);
        }
        private void CreateAdvancement()
        {
            advancement=ClassicUI.Window("CharacterProgressionPanel",transform,250,244);advancement.GetComponent<ClassicWindow>().Configure(new Vector2(.36f,.58f),()=>advancement.gameObject.SetActive(false));
            var frame=ClassicWindowSkin.Surface("ProgressionFrame",advancement,250,244,font,"FIRST JOB ADVANCEMENT");ClassicUI.DragTitle(frame,advancement,250);
            ClassicUI.ArtButton("CloseProgression",frame,"Basic.img/BtClose",234,5,()=>advancement.gameObject.SetActive(false));
            readout=ClassicWindowSkin.Label("ProgressionReadout",frame,font,"",10,32,230,34,12);
            int i=0;foreach(var choice in OfflineProgression.FirstJobs)
            { int id=choice.JobId;var b=ClassicWindowSkin.Action("AdvanceJob_"+id,frame,font,choice.Name+" · Level "+choice.Level,10,75+i++*25,230,()=>{world.TryAdvanceFirstJob(id,out var result);message.text=result;GetComponent<StatusBar>()?.PostMessage(result);},new Color32(92,150,187,255));jobs[id]=b; }
            message=ClassicWindowSkin.Label("ProgressionMessage",frame,font,"Choose a first job when you meet its requirements.",10,205,230,32,10);advancement.gameObject.SetActive(false);
        }
        private void Update()
        {
            if(world==null||panel==null)return;
            var p=world.Player;var attack=p.PhysicalAttackStats;
            values["Name"].text=p.Name;values["Job"].text=StatusBar.JobFamily(p.JobId).ToUpperInvariant();jobDetail.text=StatusBar.JobName(p.JobId).ToUpperInvariant();values["Level"].text=p.Level.ToString();values["HP"].text=p.CurrentHP+"/"+p.MaxHP;values["MP"].text=p.CurrentMP+"/"+p.MaxMP;
            values["EXP"].text=p.Experience+" ("+(p.ExperienceToNextLevel>0?(100d*p.Experience/p.ExperienceToNextLevel).ToString("0.##"):"100")+"%)";values["Fame"].text="—";
            foreach(var pair in spendButtons){values[pair.Key.ToString()].text=(pair.Key==PrimaryAttribute.STR?p.STR:pair.Key==PrimaryAttribute.DEX?p.DEX:pair.Key==PrimaryAttribute.INT?p.INT:p.LUK).ToString();pair.Value.interactable=p.CanSpendAbilityPoint(pair.Key);}
            values["AP"].text=p.AbilityPoints.ToString();values["Attack"].text=$"{attack.Minimum:0.#}–{attack.Maximum:0.#}";values["WeaponDefense"].text=p.WeaponDefense.ToString();values["Magic"].text=p.MagicAttack.ToString();values["MagicDefense"].text=p.MagicDefense.ToString();
            values["Accuracy"].text=p.Accuracy.ToString();values["Evasion"].text=p.Avoidability.ToString();values["CritRate"].text=attack.CriticalChance.ToString("P0");values["CritDamage"].text=attack.CriticalDamageMultiplier.ToString("P0");values["Speed"].text=p.Speed+"%";values["Jump"].text=p.JumpPower+"%";
            foreach(var text in values.Values)ClassicMessageText.Fit(text,text.text,false);
            ClassicMessageText.Fit(jobDetail,jobDetail.text,false);
            detailHint.text=p.UsesAmmunition?(p.HasUsableAmmunition?"Ammunition: "+p.AmmunitionCount:"Out of ammunition"):"Includes equipment and buffs";
            detailHint.color=p.UsesAmmunition&&!p.HasUsableAmmunition?new Color32(125,26,26,255):Color.white;
            readout.text=$"Level {p.Level} · {StatusBar.JobName(p.JobId)}\nAP: {p.AbilityPoints}   SP: {p.SkillPoints}";
            foreach(var pair in jobs)pair.Value.interactable=world.CanAdvanceFirstJob(pair.Key,out _);
        }
        private void OnDestroy(){if(font!=null)Destroy(font);}
    }
}
