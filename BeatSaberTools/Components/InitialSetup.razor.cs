using BeatSaberTools.Core.Services;
using BeatSaberTools.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace BeatSaberTools.Components
{
    public partial class InitialSetup
    {
        [Inject]
        protected IFolderPicker FolderPicker { get; set; }

        [Inject]
        protected BeatSaverFileService BeatSaberToolFileService { get; set; }

        [CascadingParameter]
        MudDialogInstance MudDialog { get; set; }

        public string BeatSaberInstallLocation { get; set; }

        protected override void OnInitialized()
        {
            SubscribeAndBind(BeatSaberToolFileService.BeatSaberInstallLocationObservable, installLocation => BeatSaberInstallLocation = installLocation);
        }

        public async Task PickFolder()
        {
            BeatSaberInstallLocation = await FolderPicker.PickFolder();
        }

        public async Task SaveInititalSetup()
        {
            MudDialog.Close(DialogResult.Ok(true));

            await BeatSaberToolFileService.SetBeatSaberInstallLocation(BeatSaberInstallLocation);
        }
    }
}