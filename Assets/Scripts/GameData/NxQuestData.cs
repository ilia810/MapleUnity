using System;
using System.Collections.Generic;
using System.Linq;
using MapleClient.GameLogic.Data;

namespace MapleClient.GameData
{
    /// <summary>A deliberately audited subset of Quest.nx. Server-scripted and event quests are not auto-enabled.</summary>
    public static class NxQuestData
    {
        public static readonly int[] SupportedIds = { 1037, 1038, 1039, 2088 };
        public static IReadOnlyList<QuestDefinition> Read(INxFile file)
        {
            var result = new List<QuestDefinition>();
            foreach (int id in SupportedIds)
            {
                var info = file?.GetNode("QuestInfo.img/"+id);
                var check = file?.GetNode("Check.img/"+id); var act = file?.GetNode("Act.img/"+id);
                if (info == null || check == null) continue;
                if (!Requirements(check["0"],out var start) || !Requirements(check["1"],out var finish) ||
                    !Reward(act?["0"],out var onStart) || !Reward(act?["1"],out var onFinish)) continue;
                var quest = new QuestDefinition { Id=id, Name=info["name"]?.GetValue<string>() ?? "Quest "+id,
                    Area=info["area"]?.GetValue<int>() ?? 0, Start=start, Finish=finish, OnStart=onStart, OnFinish=onFinish };
                for (int i=0;i<3;i++) quest.Descriptions[i]=info[i.ToString()]?.GetValue<string>() ?? "";
                foreach (string branch in new[] { "0", "0/yes", "0/no", "1", "1/yes", "1/stop/item", "1/stop/mob", "1/stop/npc", "1/lost" })
                {
                    var node=file.GetNode("Say.img/"+id+"/"+branch);
                    quest.Dialogue[branch]=(node?.Children ?? Enumerable.Empty<INxNode>()).Where(n=>int.TryParse(n.Name,out _))
                        .OrderBy(n=>int.Parse(n.Name)).Select(n=>n.GetValue<string>()).Where(s=>!string.IsNullOrEmpty(s)).ToArray();
                }
                result.Add(quest);
            }
            return result.AsReadOnly();
        }
        private static bool Requirements(INxNode node,out QuestRequirement value)
        {
            value=new QuestRequirement(); if(node==null)return false;
            var allowed=new[]{"npc","lvmin","lvmax","job","item","mob","quest"};
            if(node.Children.Any(n=>!allowed.Contains(n.Name)))return false;
            value.Npc=node["npc"]?.GetValue<int>() ?? 0;
            value.MinLevel=node["lvmin"]?.GetValue<int>() ?? 1; value.MaxLevel=node["lvmax"]?.GetValue<int>() ?? 200;
            value.Jobs=(node["job"]?.Children ?? Enumerable.Empty<INxNode>()).Select(n=>n.GetValue<int>()).ToArray();
            return value.Npc>0 && Counts(node["item"],value.Items,"count",false) && Counts(node["mob"],value.Mobs,"count",false) && Counts(node["quest"],value.Quests,"state",false);
        }
        private static bool Counts(INxNode node,Dictionary<int,int> target,string count,bool signed)
        {
            foreach(var n in node?.Children ?? Enumerable.Empty<INxNode>())
            {
                // Random, job-filtered and selectable rewards require separate implementations.
                if(n.Children.Any(c=>c.Name!="id" && c.Name!=count))return false;
                int id=n["id"]?.GetValue<int>() ?? 0, amount=n[count]?.GetValue<int>() ?? 0;
                if(id<=0 || (!signed && amount<=0) || amount==int.MinValue || target.ContainsKey(id))return false;
                if(amount!=0)target.Add(id,amount);
            }
            return true;
        }
        private static bool Reward(INxNode node,out QuestReward value)
        {
            value=new QuestReward(); if(node==null)return true;
            // Some versions duplicate localized Say text here. nextQuest is a journal hint, not auto-acceptance.
            if(node.Children.Any(n=>!int.TryParse(n.Name,out _) && !new[]{"yes","no","item","exp","money","nextQuest"}.Contains(n.Name)))return false;
            value.Experience=node["exp"]?.GetValue<int>() ?? 0;value.Mesos=node["money"]?.GetValue<int>() ?? 0;
            return value.Experience>=0 && value.Mesos>=0 && Counts(node["item"],value.Items,"count",true);
        }
    }
}
