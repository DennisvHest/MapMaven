using MapMaven.Core.Services;
using MapMaven.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace MapMaven.Components
{
    public partial class SideNav
    {
        [Inject]
        protected PlaylistService PlaylistService { get; set; }

        [Inject]
        ScoreSaberService ScoreSaberService { get; set; }

        [Inject]
        public NavigationManager NavigationManager { get; set; }

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