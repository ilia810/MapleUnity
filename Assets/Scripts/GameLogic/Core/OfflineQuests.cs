using System;
using System.Collections.Generic;
using System.Linq;
using MapleClient.GameLogic.Data;

namespace MapleClient.GameLogic.Core
{
    public sealed class OfflineQuestLog
    {
        public const int TrackingLimit=5;
        private readonly Player player;
        private readonly Dictionary<int,QuestDefinition> definitions;
        private readonly Dictionary<int,SavedQuest> entries=new Dictionary<int,SavedQuest>();
        private bool changing;
        public bool AutoTrackAccepted { get; private set; } = true;
        public IEnumerable<QuestDefinition> Definitions => definitions.Values.OrderBy(q=>q.Id);
        public event Action Changed;
        public OfflineQuestLog(Player player,IQuestDataProvider data)
        {
            this.player=player;definitions=(data?.Quests ?? new QuestDefinition[0]).ToDictionary(q=>q.Id);
            player.Inventory.Changed+=Notify; player.ProgressionChanged+=Notify;
        }
        private void Notify() { if(!changing)Changed?.Invoke(); }
        public QuestDefinition Get(int id) => definitions.TryGetValue(id,out var q)?q:null;
        public QuestState State(int id) => entries.TryGetValue(id,out var q)?(QuestState)q.State:QuestState.Available;
        public bool IsTracked(int id) => entries.TryGetValue(id,out var q)&&q.Tracked;
        // A presentation preference for future acceptances. Never rewrite manual tracking choices.
        public void SetAutoTrackAccepted(bool value)
        {
            if(changing || AutoTrackAccepted==value)return;
            AutoTrackAccepted=value;Notify();
        }
        public int Kills(int id,int mob)
        {
            if(!entries.TryGetValue(id,out var saved) || !definitions.TryGetValue(id,out var q))return 0;
            int index=Array.IndexOf(q.Finish.Mobs.Keys.OrderBy(k=>k).ToArray(),mob);
            return index<0?0:saved.Kills[index];
        }
        private bool Meets(QuestRequirement r,int id,bool kills)
        {
            return player.Level>=r.MinLevel && player.Level<=r.MaxLevel && (r.Jobs.Length==0 || r.Jobs.Contains(player.JobId)) &&
                r.Items.All(i=>player.Inventory.GetItemCount(i.Key)>=i.Value) && r.Quests.All(q=>(int)State(q.Key)==q.Value) &&
                (!kills || r.Mobs.All(m=>Kills(id,m.Key)>=m.Value));
        }
        public bool CanStart(int id) => Get(id)!=null && State(id)==QuestState.Available && Meets(Get(id).Start,id,false);
        public bool CanFinish(int id) => Get(id)!=null && State(id)==QuestState.InProgress && Meets(Get(id).Finish,id,true);
        public bool SetTracked(int id,bool tracked,out string message)
        {
            message="Only active quests can be tracked.";
            if(changing || State(id)!=QuestState.InProgress)return false;
            if(tracked && !IsTracked(id) && entries.Values.Count(q=>q.Tracked)>=TrackingLimit)
            {message="You can track up to five quests.";return false;}
            entries[id].Tracked=tracked;Notify();message=tracked?"Quest added to the helper.":"Quest removed from the helper.";return true;
        }
        internal bool Start(int id,out string message)
        {
            message="This quest is not available to your character.";
            if(changing || !CanStart(id))return false;
            var q=Get(id);if(!CanReward(q.OnStart,out message))return false;
            changing=true;
            try
            {
                entries.Add(id,new SavedQuest{Id=id,State=1,Kills=new int[q.Finish.Mobs.Count],Tracked=AutoTrackAccepted && entries.Values.Count(e=>e.Tracked)<TrackingLimit});
                Reward(q.OnStart);
            }
            finally {changing=false;}
            Notify();message="Started: "+q.Name;return true;
        }
        internal bool Finish(int id,out string message)
        {
            message="The quest objectives are not complete yet.";
            if(changing || !CanFinish(id))return false;
            var q=Get(id);if(!CanReward(q.OnFinish,out message))return false;
            changing=true;
            try
            {
                // Set state before inventory/EXP callbacks so a re-entrant completion cannot award twice.
                entries[id].State=2;entries[id].Tracked=false;Reward(q.OnFinish);
            }
            finally {changing=false;}
            Notify();message="Completed: "+q.Name;return true;
        }
        public bool Abandon(int id,out string message)
        {
            message="Only active quests can be abandoned.";
            if(changing || State(id)!=QuestState.InProgress)return false;
            changing=true;
            try
            {
                // Reclaim issued delivery items before permitting another acceptance; never duplicate the letter.
                var remove=Get(id).OnStart.Items.Where(i=>i.Value>0 && player.Inventory.GetItemCount(i.Key)>0).ToDictionary(i=>i.Key,i=>Math.Min(i.Value,player.Inventory.GetItemCount(i.Key)));
                entries.Remove(id);player.Inventory.TryExchange(remove,null);
            }
            finally {changing=false;}
            Notify();message="Quest abandoned. Its hunt counts have been reset.";return true;
        }
        internal void CreditKill(int mob)
        {
            bool dirty=false;
            foreach(var entry in entries.Values.Where(e=>e.State==1))
            {
                var q=Get(entry.Id);var ids=q.Finish.Mobs.Keys.OrderBy(k=>k).ToArray();int index=Array.IndexOf(ids,mob);
                if(index>=0 && entry.Kills[index]<q.Finish.Mobs[mob]){entry.Kills[index]++;dirty=true;}
            }
            if(dirty)Notify();
        }
        private bool CanReward(QuestReward r,out string message)
        {
            message="The quest's item data is unavailable.";
            if(r.Items.Any(i=>player.GetItemInfo(i.Key)==null))return false;
            message="Make room in your bag for the quest reward.";
            if(!player.Inventory.CanExchange(r.Items.Where(i=>i.Value<0).ToDictionary(i=>i.Key,i=>-i.Value),r.Items.Where(i=>i.Value>0).ToDictionary(i=>i.Key,i=>i.Value)))return false;
            if((long)player.Mesos+r.Mesos>int.MaxValue){message="Your meso wallet is full.";return false;}
            return true;
        }
        internal bool RecoverDelivery(int id,out string message)
        {
            message="There are no missing delivery items for this quest.";
            if(changing || State(id)!=QuestState.InProgress)return false;
            var items=Get(id).OnStart.Items.Where(i=>i.Value>0 && player.GetItemInfo(i.Key)?.IsQuest==true && player.Inventory.GetItemCount(i.Key)<i.Value)
                .ToDictionary(i=>i.Key,i=>i.Value-player.Inventory.GetItemCount(i.Key));
            if(items.Count==0)return false;
            var reward=new QuestReward{Items=items};if(!CanReward(reward,out message))return false;
            changing=true;try{Reward(reward);}finally{changing=false;}
            Notify();message="The missing delivery item has been replaced.";return true;
        }
        private void Reward(QuestReward r)
        {
            player.Inventory.TryExchange(r.Items.Where(i=>i.Value<0).ToDictionary(i=>i.Key,i=>-i.Value),r.Items.Where(i=>i.Value>0).ToDictionary(i=>i.Key,i=>i.Value));
            if(r.Mesos>0)player.TryGainMesos(r.Mesos);if(r.Experience>0)player.AddExperience(r.Experience);
        }
        public SavedQuest[] Capture() => entries.Values.OrderBy(e=>e.Id).Select(Clone).ToArray();
        private static SavedQuest Clone(SavedQuest q) => new SavedQuest{Id=q.Id,State=q.State,Tracked=q.Tracked,Kills=(int[])q.Kills.Clone()};
        public bool CanRestore(SavedQuest[] saved)
        {
            if(saved==null || saved.Length>definitions.Count || saved.Any(e=>e==null) || saved.Select(e=>e.Id).Distinct().Count()!=saved.Length || saved.Count(e=>e.Tracked)>TrackingLimit)return false;
            foreach(var e in saved)
            {
                var q=Get(e.Id);
                if(q==null || (e.State!=1 && e.State!=2) || (e.State==2 && e.Tracked) || e.Kills==null || e.Kills.Length!=q.Finish.Mobs.Count)return false;
                var goals=q.Finish.Mobs.OrderBy(m=>m.Key).Select(m=>m.Value).ToArray();
                for(int i=0;i<goals.Length;i++)if(e.Kills[i]<0 || e.Kills[i]>goals[i] || (e.State==2 && e.Kills[i]!=goals[i]))return false;
                if(q.Start.Quests.Any(p=>!saved.Any(s=>s.Id==p.Key && s.State==p.Value)))return false;
            }
            return true;
        }
        internal void Restore(SavedQuest[] saved)
        {entries.Clear();foreach(var e in saved)entries.Add(e.Id,Clone(e));Notify();}
    }
    public partial class GameWorld
    {
        public OfflineQuestLog Quests { get; private set; }
        public NpcSpawn NearestNpc => currentMap?.NpcSpawns.Where(CanTalkToNpc).OrderBy(n=>Math.Abs(n.X/100-player.Position.X)).FirstOrDefault();
        public bool CanTalkToNpc(NpcSpawn npc)
        {
            if(!IsLocal || player.IsDead || player.IsBasicAttacking || player.State==PlayerState.Climbing || npc==null || currentMap?.NpcSpawns.Contains(npc)!=true)return false;
            float ground=footholdService.GetGroundBelow(npc.X,npc.Y-5);if(ground==float.MaxValue)ground=npc.Y;
            return Math.Abs(player.Position.X*100-npc.X)<=160 && Math.Abs(-(player.Position.Y-Player.Height/2)*100-ground)<=80;
        }
        public bool StartQuest(NpcSpawn npc,int id,out string message)
        {
            message="Speak to the quest's starting NPC while nearby and idle.";
            return CanTalkToNpc(npc) && Quests.Get(id)?.Start.Npc==npc.NpcId && Quests.Start(id,out message);
        }
        public event Action<int> QuestCompleted;
        public bool FinishQuest(NpcSpawn npc,int id,out string message)
        {
            message="Return to the quest's recipient while nearby and idle.";
            if(!CanTalkToNpc(npc) || Quests.Get(id)?.Finish.Npc!=npc.NpcId || !Quests.Finish(id,out message))return false;
            QuestCompleted?.Invoke(id);return true;
        }
        public bool RecoverQuestDelivery(NpcSpawn npc,int id,out string message)
        {
            message="Speak to the NPC who gave you the delivery item.";
            return CanTalkToNpc(npc) && Quests.Get(id)?.Start.Npc==npc.NpcId && Quests.RecoverDelivery(id,out message);
        }
    }
}
