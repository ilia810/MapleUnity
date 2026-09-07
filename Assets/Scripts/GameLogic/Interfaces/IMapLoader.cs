namespace MapleClient.GameLogic.Interfaces
{
    public interface IMapLoader
    {
        MapleClient.GameLogic.MapData GetMap(int mapId);
    }
    // Validation can inspect a destination without replacing the active collision service.
    public interface IMapPreviewLoader
    {
        MapleClient.GameLogic.MapData PreviewMap(int mapId);
    }
}
