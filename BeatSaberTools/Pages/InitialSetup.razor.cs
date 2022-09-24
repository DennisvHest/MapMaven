using BeatSaberTools.Core.Services;
using BeatSaberTools.Services;
using Microsoft.AspNetCore.Components;

namespace BeatSaberTools.Pages
{
    public partial class InitialSetup
    {
        [Inject]
        protected IFolderPicker FolderPicker { get; set; }

        [Inject]
        protected IBeatSaverFileService BeatSaberToolFileService { get; set; }

        public async Task PickFolder()
        {
            var installLocation = await FolderPicker.PickFolder();

            BeatSaberToolFileService.SetBeatSaberInstallLocation(installLocation);
        }
    }
}