using BeatSaberTools.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace BeatSaberTools.Pages
{
    public partial class InitialSetup
    {
        [Inject]
        protected IFolderPicker FolderPicker { get; set; }

        [Inject]
        protected BeatSaberToolFileService BeatSaberToolFileService { get; set; }

        [CascadingParameter]
        MudDialogInstance MudDialog { get; set; }

        public string BeatSaberInstallLocation { get; set; }

        public async Task PickFolder()
        {
            BeatSaberInstallLocation = await FolderPicker.PickFolder();
        }

        public void SaveInititalSetup()
        {
            MudDialog.Close(DialogResult.Ok(true));

            BeatSaberToolFileService.SetBeatSaberInstallLocation(BeatSaberInstallLocation);
        }
    }
}