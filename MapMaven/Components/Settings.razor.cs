using MapMaven.Components.Shared;
using MapMaven.Components.Updates;
using MapMaven.Core.Models;
using MapMaven.Core.Services;
using MapMaven.Core.Services.Interfaces;
using MapMaven.Core.Services.Leaderboards;
using MapMaven.Services;
using MapMaven.Utility;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Text.RegularExpressions;

namespace MapMaven.Components
{
    public partial class Settings
    {
        private static readonly Regex _scoreSaberPlayerIdUrlRegex = new Regex(@"scoresaber.com\/u\/(?<playerId>[^\/\?]+)");
        private static readonly Regex _beatLeaderPlayerIdUrlRegex = new Regex(@"beatleader.xyz\/u\/(?<playerId>[^\/\?]+)");

        [Inject]
        protected IFolderPicker FolderPicker { get; set; }

        [Inject]
        protected BeatSaberFileService BeatSaberToolFileService { get; set; }

        [Inject]
        protected IBeatSaberDataService BeatSaberDataService { get; set; }

        [Inject]
        protected ILeaderboardService LeaderboardService { get; set; }

        [Inject]
        protected ScoreSaberService ScoreSaberService { get; set; }

        [Inject]
        protected BeatLeaderService BeatLeaderService { get; set; }

        [Inject]
        protected ApplicationFilesService ApplicationFilesService { get; set; }

        [Inject]
        protected IDialogService DialogService { get; set; }

        [Inject]
        protected UpdateService UpdateService { get; set; }

        [CascadingParameter]
        MudDialogInstance MudDialog { get; set; }

        [Parameter]
        public bool InitialSetup { get; set; } = false;

        public string BeatSaberInstallLocation { get; set; }
        public LeaderboardProvider? ActiveLeaderboardProvider { get; set; }
        public string ScoreSaberPlayerId { get; set; }
        public string BeatLeaderPlayerId { get; set; }

        private string? OldBeatSaberInstallLocation { get; set; }

        private bool UpdateIsAvailable;

        protected override void OnInitialized()
        {
            SubscribeAndBind(BeatSaberToolFileService.BeatSaberInstallLocationObservable, installLocation =>
            {
                OldBeatSaberInstallLocation = installLocation;
                BeatSaberInstallLocation = installLocation;
            });
            SubscribeAndBind(LeaderboardService.ActiveLeaderboardProviderName, provider => ActiveLeaderboardProvider = provider);
            SubscribeAndBind(ScoreSaberService.PlayerIdObservable, playerId => ScoreSaberPlayerId = playerId);
            SubscribeAndBind(BeatLeaderService.PlayerIdObservable, playerId => BeatLeaderPlayerId = playerId);
            SubscribeAndBind(UpdateService.AvailableUpdate, update => UpdateIsAvailable = true);
        }

        public void AutoFillPlayerId(string beatSaberInstallLocation)
        {
            if (string.IsNullOrEmpty(ScoreSaberPlayerId))
            {
                var scoreSaberPlayerId = ScoreSaberService.GetPlayerIdFromReplays(beatSaberInstallLocation);

                if (!string.IsNullOrEmpty(scoreSaberPlayerId))
                    ScoreSaberPlayerId = scoreSaberPlayerId;
            }

            if (string.IsNullOrEmpty(BeatLeaderPlayerId))
            {
                var beatLeaderPlayerId = BeatLeaderService.GetPlayerIdFromReplays(beatSaberInstallLocation);

                if (!string.IsNullOrEmpty(beatLeaderPlayerId))
                    BeatLeaderPlayerId = beatLeaderPlayerId;
            }
        }

        public async Task PickFolder()
        {
            BeatSaberInstallLocation = await FolderPicker.PickFolder();

            AutoFillPlayerId(BeatSaberInstallLocation);
        }

        public async Task SaveSettings()
        {
            if (!string.IsNullOrEmpty(ScoreSaberPlayerId))
            {
                var playerIdMatch = _scoreSaberPlayerIdUrlRegex.Match(ScoreSaberPlayerId);

                if (playerIdMatch.Success)
                    ScoreSaberPlayerId = playerIdMatch.Groups.GetValueOrDefault("playerId")?.Value;
            }

            if (!string.IsNullOrEmpty(BeatLeaderPlayerId))
            {
                var playerIdMatch = _beatLeaderPlayerIdUrlRegex.Match(BeatLeaderPlayerId);

                if (playerIdMatch.Success)
                    BeatLeaderPlayerId = playerIdMatch.Groups.GetValueOrDefault("playerId")?.Value;
            }

            Close();

            if (BeatSaberInstallLocation != OldBeatSaberInstallLocation)
            {
                await BeatSaberDataService.ClearMapCache();
                InitialSetup = true;
            }

            BeatSaberDataService.SetInitialMapLoad(InitialSetup);

            Task.Run(async () =>
            {
                await BeatSaberToolFileService.SetBeatSaberInstallLocation(BeatSaberInstallLocation);
                await ScoreSaberService.SetPlayerId(ScoreSaberPlayerId);
                await BeatLeaderService.SetPlayerId(BeatLeaderPlayerId);

                if (ActiveLeaderboardProvider.HasValue)
                    await LeaderboardService.SetActiveLeaderboardProviderAsync(ActiveLeaderboardProvider.Value);
            });
        }

        public async Task ResetApplicationAsync()
        {
            var dialog = DialogService.Show<ConfirmationDialog>(null, new DialogParameters
            {
                { nameof(ConfirmationDialog.DialogText), "This will reset all settings and remove all cache. Map Maven will restart with the initial setup. Are you sure?" },
                { nameof(ConfirmationDialog.ConfirmText), "Reset Map Maven" }
            });

            var result = await dialog.Result;

            if (result.Cancelled)
                return;

            ApplicationFilesService.DeleteApplicationFiles();

            ApplicationUtils.RestartApplication();
        }

        void OpenReleaseNotes(bool updateAvailable)
        {
            DialogService.Show<ReleaseNotes>(
                title: $"Map Maven {UpdateService.CurrentVersion}",
                parameters: new()
                {
                    { nameof(ReleaseNotes.UpdateAvailable), updateAvailable }
                },
                options: new DialogOptions
                {
                    CloseButton = true
                }
            );
        }

        public void Close()
        {
            MudDialog.Close(DialogResult.Ok(true));
        }
    }
}