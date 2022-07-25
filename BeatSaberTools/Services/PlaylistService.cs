using System.Reactive.Linq;
using System.Reactive.Subjects;
using Playlist = BeatSaberTools.Models.Playlist;

namespace BeatSaberTools.Services
{
    public class PlaylistService
    {
        private readonly BeatSaberDataService _beatSaberDataService;

        private readonly BehaviorSubject<Playlist> _selectedPlaylist = new(null);

        public IObservable<IEnumerable<Playlist>> Playlists { get; private set; }
        public IObservable<Playlist> SelectedPlaylist => _selectedPlaylist;

        public PlaylistService(BeatSaberDataService beatSaberDataService)
        {
            _beatSaberDataService = beatSaberDataService;

            Playlists = _beatSaberDataService.PlaylistInfo.Select(x => x.Select(i => new Playlist(i)));
        }

        public void SetSelectedPlaylist(Playlist playlist)
        {
            _selectedPlaylist.OnNext(playlist);
        }
    }
}
