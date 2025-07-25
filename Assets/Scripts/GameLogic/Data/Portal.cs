namespace MapleClient.GameLogic
{
    public class Portal
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public int TargetMapId { get; set; }
        public string TargetPortalName { get; set; }
        public PortalType Type { get; set; }
    }

    public enum PortalType
    {
        Spawn,
        Regular,
        Normal,
        Hidden,
        Script
    }
}