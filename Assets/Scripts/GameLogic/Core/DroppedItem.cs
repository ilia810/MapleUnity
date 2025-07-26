namespace MapleClient.GameLogic.Core
{
    public class DroppedItem
    {
        public int ObjectId { get; set; }
        public int ItemId { get; }
        public int Quantity { get; }
        public Vector2 Position { get; set; }
        public float LifeTime { get; set; }

        public DroppedItem(int itemId, int quantity, Vector2 position)
        {
            ItemId = itemId;
            Quantity = quantity;
            Position = position;
            LifeTime = 180f; // 3 minutes in MapleStory
        }

        public void Update(float deltaTime)
        {
            LifeTime -= deltaTime;
        }

        public bool IsExpired => LifeTime <= 0;
    }
}