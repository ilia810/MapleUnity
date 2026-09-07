using System;
using System.Collections.Generic;

namespace MapleClient.GameLogic.Data
{
    public enum QuestState { Available = 0, InProgress = 1, Completed = 2 }
    public interface IQuestDataProvider { IReadOnlyList<QuestDefinition> Quests { get; } }
    public sealed class QuestRequirement
    {
        public int Npc, MinLevel = 1, MaxLevel = 200;
        public int[] Jobs = new int[0];
        public Dictionary<int,int> Items = new Dictionary<int,int>();
        public Dictionary<int,int> Mobs = new Dictionary<int,int>();
        public Dictionary<int,int> Quests = new Dictionary<int,int>();
    }
    public sealed class QuestReward
    {
        public int Experience, Mesos;
        public Dictionary<int,int> Items = new Dictionary<int,int>();
    }
    public sealed class QuestDefinition
    {
        public int Id, Area;
        public string Name;
        public string[] Descriptions = new string[3];
        public QuestRequirement Start, Finish;
        public QuestReward OnStart, OnFinish;
        public Dictionary<string,string[]> Dialogue = new Dictionary<string,string[]>();
    }
    [Serializable] public sealed class SavedQuest
    {
        public int Id, State;
        public int[] Kills;
        public bool Tracked;
    }
}
