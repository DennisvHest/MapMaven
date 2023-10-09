using MapMaven.Core.Services;
using MapMaven.Core.Services.Interfaces;
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
        protected BeatSaberFileService BeatSaberToolFileService { get; set; }

        [Inject]
        protected IBeatSaberDataService BeatSaberDataService { get; set; }

        [Inject]
        protected IScoreSaberService ScoreSaberService { get; set; }

        [CascadingParameter]
        MudDialogInstance MudDialog { get; set; }

        [Parameter]
        public bool InitialSetup { get; set; } = false;

        public string BeatSaberInstallLocation { get; set; }
        public string PlayerId { get; set; }

        private string? OldBeatSaberInstallLocation { get; set; }

        protected override void OnInitialized()
        {
            SubscribeAndBind(BeatSaberToolFileService.BeatSaberInstallLocationObservable, installLocation =>
            {
                OldBeatSaberInstallLocation = installLocation;
                BeatSaberInstallLocation = installLocation;
            });
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

            if (BeatSaberInstallLocation != OldBeatSaberInstallLocation)
            {
                await BeatSaberDataService.ClearMapCache();
                InitialSetup = true;
            }

            BeatSaberDataService.SetInitialMapLoad(InitialSetup);

            Task.Run(() => BeatSaberToolFileService.SetBeatSaberInstallLocation(BeatSaberInstallLocation));
            Task.Run(() => ScoreSaberService.SetPlayerId(PlayerId));
        }

        public void Close()
        {
            MudDialog.Close(DialogResult.Ok(true));
        }
    }
}