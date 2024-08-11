using MapMaven.Components.Shared;
using MapMaven.Core.Models.Data;
using MapMaven.Core.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Map = MapMaven.Models.Map;

namespace MapMaven.Components.Maps
{
    public partial class RankedMapBrowserRow
    {
        [Inject]
        protected IBeatSaberDataService BeatSaberDataService { get; set; }

        [Inject]
        protected IMapService MapService { get; set; }

        [Inject]
        ISnackbar Snackbar { get; set; }

        [Inject]
        IDialogService DialogService { get; set; }

        [Parameter]
        public IEnumerable<Map> FilteredMaps { get; set; }

        bool MapIsInstalled(Map map)
        {
            return MapService.MapIsInstalled(map);
        }

        void OpenDetails(Map map)
        {
            var fullMap = MapService.GetMapById(map.Id);

            DialogService.Show<MapDetail>(
                title: null,
                parameters: new() { { nameof(MapDetail.Map), fullMap }, { nameof(MapDetail.MapHash), map.Hash } },
                options: new()
                {
                    MaxWidth = MaxWidth.Small,
                    FullWidth = true,
                    CloseButton = true
                }
            );
        }

        async Task DownloadMap(Map map)
        {
            var subject = new BehaviorSubject<ItemProgress<Map>>(null);

            var cancellationToken = new CancellationTokenSource();

            var snackbar = Snackbar.Add<MapDownloadProgressMessage>(new Dictionary<string, object>
            {
                { nameof(MapDownloadProgressMessage.ProgressReport), subject.Sample(TimeSpan.FromSeconds(0.2)).AsObservable() },
                { nameof(MapDownloadProgressMessage.CancellationToken), cancellationToken },
            }, configure: config =>
            {
                config.RequireInteraction = true;
                config.ShowCloseIcon = false;
            });

            var progress = new Progress<double>(itemProgress => subject.OnNext(new ItemProgress<Map>
            {
                CompletedItems = 0,
                CurrentItem = map,
                CurrentMapProgress = itemProgress,
                TotalItems = 1
            }));

            await MapService.DownloadMap(map, progress: progress);

            Snackbar.Remove(snackbar);

            if (!cancellationToken.IsCancellationRequested)
            {
                Snackbar.Add($"Added map: {map.Name}", Severity.Normal, config => config.Icon = Icons.Material.Filled.Check);
            }
            else
            {
                Snackbar.Add($"Canceled downloading map.", Severity.Normal, config => config.Icon = Icons.Material.Filled.Cancel);
            }

            MapService.ResetSelectedMaps();
        }

        async Task HideUnhideMap(Map map)
        {
            await MapService.HideUnhideMap(map);
        }
    }
}