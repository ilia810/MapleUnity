using System.Collections.Generic;
using System.Linq;
using MapleClient.GameLogic.Core;
using MapleClient.GameData;
using GameData;
using UnityEngine;
using UnityEngine.UI;

namespace MapleClient.GameView.UI
{
    public sealed class ClassicBuffView : MonoBehaviour
    {
        private Player player;
        private MapleClient.GameLogic.Skills.SkillManager skills;
        private RectTransform root;
        private Font font;
        private ClassicTooltipView tooltip;
        private readonly Dictionary<int,Text> timers=new Dictionary<int,Text>();
        public void Bind(Player value, MapleClient.GameLogic.Skills.SkillManager skillManager = null)
        {
            player=value;skills=skillManager;if(root!=null)return;
            font=Font.CreateDynamicFontFromOSFont("Arial",10);tooltip=ClassicTooltipView.For(transform);
            root=ClassicUI.Rect("ActiveBuffs",transform,400,36);root.anchorMin=root.anchorMax=root.pivot=Vector2.one;root.anchoredPosition=new Vector2(-4,-4);
        }
        private void Update()
        {
            if(player==null||root==null)return;
            var buffs=player.ActiveBuffs.GroupBy(b=>b.SourceId).ToArray();
            foreach(int id in timers.Keys.Where(id=>!buffs.Any(b=>b.Key==id)).ToArray())
            { timers[id].transform.parent.gameObject.SetActive(false);Destroy(timers[id].transform.parent.gameObject);timers.Remove(id); }
            foreach(var group in buffs)
            {
                int id=group.Key;var buff=group.First();
                if(!timers.TryGetValue(id,out var timer))
                {
                    var cell=ClassicWindowSkin.Fill("Buff_"+id,root,0,0,32,32,new Color(.9f,.95f,1,.8f),true);
                    ClassicWindowSkin.Border("Frame",cell.transform,0,0,32,32);
                    var image=ClassicWindowSkin.Fill("Icon",cell.transform,1,1,30,30,Color.white);
                    if(id<0)image.sprite=NXAssetLoader.Instance.LoadItemIcon(-id);
                    else image.sprite=SkillSprites.Icon(NXDataManagerSingleton.Instance.DataManager.SkillData.GetSkill(id));
                    image.preserveAspect=true;image.enabled=image.sprite!=null;
                    timer=ClassicWindowSkin.Label("BuffTime",cell.transform,font,"",0,19,31,13,10,Color.white,true);timer.alignment=TextAnchor.LowerRight;
                    var outline=timer.gameObject.AddComponent<Outline>();outline.effectColor=new Color(0,0,0,.9f);outline.effectDistance=new Vector2(1,-1);
                    var hover=cell.gameObject.AddComponent<ClassicHover>();hover.Enter=e=>{
                        var active=player.ActiveBuffs.Where(b=>b.SourceId==id).ToArray();if(active.Length==0)return;
                        tooltip.ShowText(cell,e.position,active[0].Name,string.Join("\n",active.Select(b=>BuffDescription.Describe(b.Type,b.Value)))+"\n"+Mathf.CeilToInt(active.Min(b=>b.RemainingMilliseconds)/1000f)+" seconds remaining.\nRight-click to cancel.");
                    };hover.Exit=()=>tooltip.Hide(cell);timers.Add(id,timer);
                    cell.gameObject.AddComponent<ClassicItemClick>().Right=e=>{
                        if(skills?.TryCancelBuff(id)==true)tooltip.Hide(cell);
                    };
                }
                timer.text=Mathf.CeilToInt(group.Min(b=>b.RemainingMilliseconds)/1000f)+"s";
            }
            Layout();
        }
        public void Layout()
        {
            if(root==null)return;
            int columns=Mathf.Max(1,Mathf.FloorToInt(Mathf.Min(400,((RectTransform)root.parent).rect.width-8)/36));
            int rows=Mathf.CeilToInt(timers.Count/(float)columns);
            root.sizeDelta=new Vector2(timers.Count==0?0:Mathf.Min(columns,timers.Count)*36-4,rows==0?0:rows*36-4);
            int index=0;foreach(var entry in timers.OrderBy(e=>e.Key))
            {
                var r=(RectTransform)entry.Value.transform.parent;r.anchorMin=r.anchorMax=r.pivot=Vector2.one;
                r.anchoredPosition=new Vector2(-36*(index%columns),-36*(index/columns));index++;
            }
        }
        private void LateUpdate()=>Layout();
        private void OnDestroy(){if(font!=null)Destroy(font);}
    }
}
