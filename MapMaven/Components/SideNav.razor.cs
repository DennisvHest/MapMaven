using MapMaven.Core.Services.Interfaces;
using MapMaven.Core.Services.Leaderboards;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace MapMaven.Components
{
    public partial class SideNav
    {
        [Inject]
        IPlaylistService PlaylistService { get; set; }

        [Inject]
        ILeaderboardService ScoreSaberService { get; set; }

        [Inject]
        IDialogService DialogService { get; set; }

        [Inject]
        IJSRuntime JS { get; set; }

        public string? PlayerId { get; set; } = null;

        protected override void OnInitialized()
        {
            SubscribeAndBind(ScoreSaberService.PlayerIdObservable, playerId => PlayerId = playerId);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
                await JS.InvokeVoidAsync("window.sideNavResizer.initialize");
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