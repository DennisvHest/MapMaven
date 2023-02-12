using MapMaven.Core.Services;
using MapMaven.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Text.RegularExpressions;

namespace MapMaven.Components
{
    public partial class Settings
    {
        private static readonly Regex _scoreSaberPlayerIdUrlRegex = new Regex(@"scoresaber.com\/u\/(?<playerId>[^\/\?]+)");

        [Inject]
        protected IFolderPicker FolderPicker { get; set; }

        [Inject]
        protected BeatSaverFileService BeatSaberToolFileService { get; set; }

        [Inject]
        protected ScoreSaberService ScoreSaberService { get; set; }

        [CascadingParameter]
        MudDialogInstance MudDialog { get; set; }

        [Parameter]
        public bool InitialSetup { get; set; } = false;

        public string BeatSaberInstallLocation { get; set; }
        public string PlayerId { get; set; }

        protected override void OnInitialized()
        {
            SubscribeAndBind(BeatSaberToolFileService.BeatSaberInstallLocationObservable, installLocation => BeatSaberInstallLocation = installLocation);
            SubscribeAndBind(ScoreSaberService.PlayerIdObservable, playerId => PlayerId = playerId);
        }

        public void AutoFillPlayerId(string beatSaberInstallLocation)
        {
            if (!string.IsNullOrEmpty(PlayerId))
                return;

            var playerId = ScoreSaberService.GetPlayerIdFromReplays(beatSaberInstallLocation);

            if (!string.IsNullOrEmpty(playerId))
                PlayerId = playerId;
        }

        public async Task PickFolder()
        {
            BeatSaberInstallLocation = await FolderPicker.PickFolder();

            AutoFillPlayerId(BeatSaberInstallLocation);
        }

        public async Task SaveSettings()
        {
            if (!string.IsNullOrEmpty(PlayerId))
            {
                var playerIdMatch = _scoreSaberPlayerIdUrlRegex.Match(PlayerId);

                if (playerIdMatch.Success)
                    PlayerId = playerIdMatch.Groups.GetValueOrDefault("playerId")?.Value;
            }

            Close();

            await BeatSaberToolFileService.SetBeatSaberInstallLocation(BeatSaberInstallLocation);
            await ScoreSaberService.SetPlayerId(PlayerId);
        }

        public void Close()
        {
            MudDialog.Close(DialogResult.Ok(true));
        }
    }
}