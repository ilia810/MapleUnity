namespace MapleClient.GameView
{
    /// <summary>
    /// HeavenClient Stage::draw traverses layers 0..7. Each layer draws objects,
    /// tiles, reactors, NPCs, mobs, remote characters, the player, then drops.
    /// Objects occupy offsets 0..255 and tiles 256..511 within each layer.
    /// </summary>
    public static class StageRenderOrder
    {
        public const string SortingLayerName = "Objects";
        public const int LayerStride = 1000;

        public static int NpcOrder(int footholdLayer) => LayerBase(footholdLayer) + 600;
        public static int MonsterOrder(int footholdLayer) => LayerBase(footholdLayer) + 650;
        public static int PlayerOrder(int footholdLayer) => LayerBase(footholdLayer) + 800;
        public static int DropOrder(int footholdLayer) => LayerBase(footholdLayer) + 850;

        private static int LayerBase(int layer) => (layer >= 0 && layer < 8 ? layer : 0) * LayerStride;
    }
}
