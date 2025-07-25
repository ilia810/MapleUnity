namespace MapleClient.GameLogic.Interfaces
{
    public interface IAudioPlayer
    {
        void PlaySound(string soundId);
        void PlayBGM(string bgmId);
        void StopBGM();
        void SetVolume(float volume);
    }
}