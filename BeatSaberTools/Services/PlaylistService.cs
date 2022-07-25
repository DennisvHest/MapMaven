using BeatSaberTools.Models;
using System.Reactive.Linq;

namespace BeatSaberTools.Services
{
    public class PlaylistService
    {
        private readonly BeatSaberDataService _beatSaberDataService;

        public IObservable<IEnumerable<Playlist>> Playlists { get; private set; }

        public PlaylistService(BeatSaberDataService beatSaberDataService)
        {
            _beatSaberDataService = beatSaberDataService;

            Playlists = _beatSaberDataService.PlaylistInfo.Select(x => x.Select(i => i.ToPlaylist()));
        }
    }
}
