namespace BeatSaberTools.Services
{
    public interface IFolderPicker
    {
        Task<string> PickFolder();
    }
}
