using MapMaven.Core.Services.Interfaces;
using MapMaven.Core.Services.Leaderboards;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace MapMaven.Components
{
    public partial class SideNav
    {
        [Inject]
        protected IPlaylistService PlaylistService { get; set; }

        [Inject]
        ILeaderboardService ScoreSaberService { get; set; }

        [Inject]
        public IDialogService DialogService { get; set; }

        public string? PlayerId { get; set; } = null;

        protected override void OnInitialized()
        {
            SubscribeAndBind(ScoreSaberService.PlayerIdObservable, playerId => PlayerId = playerId);
        }

        protected void OnMapsNavigation()
        {
            PlaylistService.SetSelectedPlaylist(null);
        }

        protected void OpenSettings()
        {
            DialogService.Show<Settings>(null);
        }
    }
}