using MapMaven.Components.Shared;
using MapMaven.Core.Models.Data;
using MudBlazor;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Map = MapMaven.Models.Map;

namespace MapMaven.Extensions
{
    public static class SnackbarExtensions
    {
        public static MapDownloadProgressSnackbar AddMapDownloadProgressSnackbar(this ISnackbar snackbarService)
        {
            var subject = new BehaviorSubject<ItemProgress<Map>>(null);

            var cancellationToken = new CancellationTokenSource();

            var snackbar = snackbarService.Add<MapDownloadProgressMessage>(new Dictionary<string, object>
            {
                { nameof(MapDownloadProgressMessage.ProgressReport), subject.Sample(TimeSpan.FromSeconds(0.2)).AsObservable() },
                { nameof(MapDownloadProgressMessage.CreatingPlaylist), true },
                { nameof(MapDownloadProgressMessage.CancellationToken), cancellationToken },
            }, configure: config =>
            {
                config.RequireInteraction = true;
                config.ShowCloseIcon = false;
            });

            var progress = new Progress<ItemProgress<Map>>(subject.OnNext);

            progress.ProgressChanged += (sender, p) =>
            {
                if (p.TotalProgress >= 1 || cancellationToken.IsCancellationRequested)
                    subject.OnCompleted();
            };

            return new MapDownloadProgressSnackbar
            {
                Progress = progress,
                CancellationToken = cancellationToken.Token,
                Snackbar = snackbar
            };
        }
    }

    public class MapDownloadProgressSnackbar
    {
        public Progress<ItemProgress<Map>> Progress { get; set; }
        public CancellationToken CancellationToken { get; set; }
        public Snackbar Snackbar { get; set; }
    }
}
