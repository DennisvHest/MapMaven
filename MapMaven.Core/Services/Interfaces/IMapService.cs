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
        IObservable<bool> Selectable { get; }

        void AddMapFilter(MapFilter filter);
        void CancelSelection();
        void ClearMapFilters();
        void ClearSelectedMaps();
        Task DeleteMap(string mapHash);
        Task DeleteMaps(IEnumerable<string> mapHashes);
        Task DownloadMap(Map map, bool force = false, IProgress<double>? progress = null, bool loadMapInfo = true, CancellationToken cancellationToken = default);
        Task<Map> GetMapDetails(Map map);
        Task HideUnhideMap(Map map);
        Task HideUnhideMap(IEnumerable<Map> maps, bool hide);
        Task LoadHiddenMaps();
        bool MapIsInstalled(Map map);
        Task RefreshDataAsync(bool forceRefresh = false);
        void RemoveMapFilter(MapFilter filter);
        void ResetSelectedMaps();
        void SelectMaps(IEnumerable<Map> selectedMaps);
        void SetSelectable(bool selectable);
        void SetSelectedMaps(HashSet<Map> selectedMaps);
    }
}