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
        public NavigationManager NavigationManager { get; set; }

        [Inject]
        public IDialogService DialogService { get; set; }

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