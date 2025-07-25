namespace MapleClient.GameLogic.Interfaces
{
    public interface INetworkClient
    {
        void Connect(string serverAddress, int port);
        void Disconnect();
        void SendPacket(byte[] data);
        event System.Action<byte[]> OnPacketReceived;
        bool IsConnected { get; }
    }
}