using BeatSaberTools.Components.Shared;
using BeatSaberTools.Core.Models;
using BeatSaberTools.Core.Models.Data;
using BeatSaberTools.Models;
using BeatSaberTools.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Map = BeatSaberTools.Models.Map;

namespace BeatSaberTools.Components.Maps
{
    public partial class ImprovementTweaker
    {
        [Inject]
        MapService MapService { get; set; }

        [Inject]
        PlaylistService PlaylistService { get; set; }

        [Inject]
        ISnackbar Snackbar { get; set; }

        [Parameter]
        public EventCallback<MapSelectionConfig> OnMapSelectionChanged { get; set; }

        string PlayedFilter = "Both";
        MapFilter PlayedMapFilter = null;

        HashSet<Map> SelectedMaps = new();

        bool CreatingPlaylist = false;

        int MapSelectNumber = 10;
        int MapSelectStartFromNumber = 1;

        protected override void OnInitialized()
        {
            SubscribeAndBind(MapService.SelectedMaps, selectedMaps => SelectedMaps = selectedMaps);
            SubscribeAndBind(PlaylistService.CreatingPlaylist, creatingPlaylist => CreatingPlaylist = creatingPlaylist);
        }

        void OnPlayedFilterChanged(string value)
        {
            PlayedFilter = value;

            if (PlayedMapFilter != null)
                MapService.RemoveMapFilter(PlayedMapFilter);

            if (PlayedFilter == "Both")
            {
                PlayedMapFilter = null;
                return;
            }

            PlayedMapFilter = new MapFilter
            {
                Type = MapFilterType.Played,
                Name = PlayedFilter,
                Visible = false
            };

            PlayedMapFilter.Filter = PlayedFilter switch
            {
                "Not played" => map => map.PlayerScore == null,
                "Played" => map => map.PlayerScore != null
            };

            MapService.AddMapFilter(PlayedMapFilter);
        }

        async Task CreatePlaylistFromSelectedMaps()
        {
            var subject = new BehaviorSubject<ItemProgress<Map>>(null);

            var snackbar = Snackbar.Add<MapDownloadProgressMessage>(new Dictionary<string, object>
            {
                { nameof(MapDownloadProgressMessage.ProgressReport), subject.Sample(TimeSpan.FromSeconds(0.2)).AsObservable() },
                { nameof(MapDownloadProgressMessage.CreatingPlaylist), true },
            }, configure: config =>
            {
                config.RequireInteraction = true;
                config.ShowCloseIcon = false;
            });

            var playlistModel = new EditPlaylistModel
            {
                Name = $"Improvement Maps ({DateTime.Now:dd-MM-yyyy HH:mm:ss})",
                FileName = $"Improvement Maps ({DateTime.Now:dd-MM-yyyy_HH-mm-ss})"
            };

            var progress = new Progress<ItemProgress<Map>>(subject.OnNext);

            await PlaylistService.AddPlaylistAndDownloadMaps(playlistModel, SelectedMaps, progress: progress);

            MapService.ClearSelectedMaps();

            Snackbar.Remove(snackbar);

            Snackbar.Add($"Created playlist: {playlistModel.Name}", Severity.Normal, config => config.Icon = Icons.Filled.Check);
        }

        void ClearSelection()
        {
            MapService.ClearSelectedMaps();
        }

        async Task ApplyMapSelection()
        {
            await OnMapSelectionChanged.InvokeAsync(new MapSelectionConfig
            {
                MapSelectNumber = MapSelectNumber,
                MapSelectStartFromNumber = MapSelectStartFromNumber
            });
        }
    }
}