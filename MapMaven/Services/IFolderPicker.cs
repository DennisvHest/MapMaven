namespace MapMaven.Services
{
    public interface IFolderPicker
    {
        Task<string> PickFolder();
    }
}
