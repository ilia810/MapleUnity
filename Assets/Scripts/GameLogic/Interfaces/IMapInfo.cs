using System.Collections.Generic;
using MapleClient.GameLogic.Data;

namespace MapleClient.GameLogic.Interfaces
{
    /// <summary>
    /// Provides detailed map information from NX data
    /// </summary>
    public interface IMapInfo
    {
        int MapId { get; }
        string Name { get; }
        
        IEnumerable<IBackgroundInfo> GetBackgrounds();
        IEnumerable<ITileInfo> GetTiles();
        IEnumerable<IObjectInfo> GetObjects();
        IEnumerable<IForegroundInfo> GetForegrounds();
        IMapBounds GetBounds();
        object GetNode(string path); // Platform-independent node reference
    }
    
    public interface IBackgroundInfo
    {
        string Name { get; }
        SpriteData Sprite { get; }
        float X { get; }
        float Y { get; }
        float ScrollRate { get; }
        int Type { get; }
    }
    
    public interface ITileInfo
    {
        int Id { get; }
        SpriteData Sprite { get; }
        float X { get; }
        float Y { get; }
        float Width { get; }
        float Height { get; }
        bool IsSolid { get; }
    }
    
    public interface IObjectInfo
    {
        int Id { get; }
        string Name { get; }
        SpriteData Sprite { get; }
        float X { get; }
        float Y { get; }
        int Z { get; }
        bool IsAnimated { get; }
        SpriteData[] AnimationFrames { get; }
    }
    
    public interface IForegroundInfo
    {
        int Id { get; }
        SpriteData Sprite { get; }
        float X { get; }
        float Y { get; }
    }
    
    public interface IMapBounds
    {
        float Left { get; }
        float Right { get; }
        float Top { get; }
        float Bottom { get; }
    }
}