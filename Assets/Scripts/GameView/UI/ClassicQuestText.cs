using System.Linq;
using System.Text.RegularExpressions;
using MapleClient.GameData;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Data;

namespace MapleClient.GameView.UI
{
    public static class ClassicQuestText
    {
        public static string Npc(int id) => NXAssetLoader.Instance.GetNxFile("string")?.GetNode("Npc.img/"+id+"/name")?.GetValue<string>() ?? "NPC "+id;
        public static string Mob(int id) => NXAssetLoader.Instance.GetNxFile("string")?.GetNode("Mob.img/"+id+"/name")?.GetValue<string>() ?? "Monster "+id;
        public static string Item(GameWorld world,int id) => world.Player.GetItemInfo(id)?.Name ?? "Item "+id;
        public static string Read(GameWorld world,QuestDefinition quest,string source)
        {
            string text=(source ?? "").Replace("\\r\\n","\n").Replace("\\n","\n").Replace("\r","");
            text=Regex.Replace(text,@"#([tpocim])(\d+)#",m=>{
                int id=int.Parse(m.Groups[2].Value);
                switch(m.Groups[1].Value)
                {
                    case "p":return Npc(id);case "o":return Mob(id);case "c":return world.Player.Inventory.GetItemCount(id).ToString();
                    case "m":return NxMapNames.Name(NXAssetLoader.Instance.GetNxFile("string")?.GetNode("Map.img"),id);
                    default:return Item(world,id);
                }
            });
            text=Regex.Replace(text,@"#a\d+#",m=>""); // Counts are rendered explicitly from the objective model below.
            text=Regex.Replace(text,@"#h\s*#",m=>world.Player.Name ?? "Traveler");
            text=ClassicTooltipView.Escape(text);
            // A small, balanced subset of Maple's text commands; no scripts are evaluated.
            string color=null;bool bold=false;
            text=Regex.Replace(text,@"#[brgkedn]",m=>{
                string close=(bold?"</b>":"")+(color!=null?"</color>":"");
                if(m.Value=="#e")bold=true;
                else if(m.Value=="#n")bold=false;
                else color=m.Value=="#b"?"#235BA5":m.Value=="#r"?"#B42434":m.Value=="#g"?"#307036":null;
                return close+(color!=null?"<color="+color+">":"")+(bold?"<b>":"");
            });
            return text+(bold?"</b>":"")+(color!=null?"</color>":"");
        }
        public static string Objectives(GameWorld world,QuestDefinition q)
        {
            bool completed=world.Quests.State(q.Id)==QuestState.Completed;
            var lines=q.Finish.Mobs.Select(m=>$"{Mob(m.Key)}  {world.Quests.Kills(q.Id,m.Key)}/{m.Value}")
                .Concat(q.Finish.Items.Select(i=>$"{Item(world,i.Key)}  {(completed?i.Value:world.Player.Inventory.GetItemCount(i.Key))}/{i.Value}"));
            return string.Join("\n",lines)+(completed?"":"\nReturn to "+Npc(q.Finish.Npc));
        }
        public static string Rewards(GameWorld world,QuestDefinition q)
        {
            var lines=q.OnFinish.Items.Where(i=>i.Value>0).Select(i=>$"{Item(world,i.Key)} × {i.Value}").ToList();
            if(q.OnFinish.Experience>0)lines.Insert(0,q.OnFinish.Experience+" EXP");if(q.OnFinish.Mesos>0)lines.Add(q.OnFinish.Mesos+" mesos");
            return lines.Count>0?string.Join("\n",lines):"Continue the quest story.";
        }
    }
}
