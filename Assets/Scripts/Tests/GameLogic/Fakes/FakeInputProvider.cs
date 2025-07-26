using MapleClient.GameLogic.Interfaces;

namespace MapleClient.GameLogic.Tests.Fakes
{
    public class FakeInputProvider : IInputProvider
    {
        public bool IsLeftPressed { get; set; }
        public bool IsRightPressed { get; set; }
        public bool IsJumpPressed { get; set; }
        public bool IsAttackPressed { get; set; }
        public bool IsUpPressed { get; set; }
        public bool IsDownPressed { get; set; }
    }
}