namespace BeatSaberTools.Core.Models.DynamicPlaylists
{
    public class DynamicPlaylistConfiguration
    {
        public IEnumerable<FilterOperation> FilterOperations { get; set; } = Enumerable.Empty<FilterOperation>();
        public IEnumerable<SortOperation> SortOperations { get; set; } = Enumerable.Empty<SortOperation>();
        public int MapCount { get; set; }
    }
}
