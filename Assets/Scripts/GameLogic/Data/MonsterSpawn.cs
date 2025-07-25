namespace MapleClient.GameLogic
{
    public class MonsterSpawn
    {
        public int MonsterId { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float SpawnInterval { get; set; }
        public int MaxCount { get; set; }
    }
}