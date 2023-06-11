using MapMaven.Core.Models;
using MapMaven.Models;

namespace MapMaven.Core.Services.Interfaces
{
    public interface IMapService
    {
        IObservable<IEnumerable<Map>> CompleteMapData { get; }
        IObservable<IEnumerable<Map>> CompleteRankedMapData { get; }
        IObservable<IEnumerable<MapFilter>> MapFilters { get; }
        IObservable<IEnumerable<Map>> Maps { get; }
        IObservable<Dictionary<string, Map>> MapsByHash { get; }
        IObservable<IEnumerable<Map>> RankedMaps { get; }
        IObservable<HashSet<Map>> SelectedMaps { get; }

        void AddMapFilter(MapFilter filter);
        void ClearMapFilters();
        void ClearSelectedMaps();
        Task DownloadMap(Map map, bool force = false, IProgress<double>? progress = null, bool loadMapInfo = true, CancellationToken cancellationToken = default);
        Task HideUnhideMap(Map map);
        Task LoadHiddenMaps();
        bool MapIsInstalled(Map map);
        Task RefreshDataAsync(bool forceRefresh = false);
        void RemoveMapFilter(MapFilter filter);
        void SelectMaps(IEnumerable<Map> selectedMaps);
        void SetSelectedMaps(HashSet<Map> selectedMaps);
    }
}