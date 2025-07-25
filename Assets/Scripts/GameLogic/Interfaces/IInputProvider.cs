namespace MapleClient.GameLogic.Interfaces
{
    public interface IInputProvider
    {
        bool IsLeftPressed { get; }
        bool IsRightPressed { get; }
        bool IsJumpPressed { get; }
        bool IsAttackPressed { get; }
        bool IsUpPressed { get; }
        bool IsDownPressed { get; }
    }
}