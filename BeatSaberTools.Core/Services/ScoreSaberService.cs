using BeatSaberTools.Core.ApiClients;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace BeatSaberTools.Core.Services
{
    public class ScoreSaberService
    {
        private readonly ScoreSaberApiClient _scoreSaber;
        private readonly IBeatSaverFileService _fileService;

        private readonly BehaviorSubject<string?> _playerId = new(null);

        public readonly IObservable<IEnumerable<PlayerScore>> PlayerScores;

        public ScoreSaberService(
            ScoreSaberApiClient scoreSaber,
            IBeatSaverFileService fileService)
        {
            _scoreSaber = scoreSaber;

            PlayerScores = _playerId.Select(playerId =>
            {
                if (string.IsNullOrEmpty(playerId))
                    Observable.Return(Enumerable.Empty<PlayerScore>());

                return Observable.FromAsync(async () =>
                {
                    var scoreCollection = await _scoreSaber.Scores2Async(
                        playerId: playerId,
                        limit: 100,
                        sort: Sort.Top,
                        page: 1,
                        withMetadata: true
                    );

                    return scoreCollection.PlayerScores;
                });
            }).Concat();
            _fileService = fileService;
        }

        public void LoadPlayerScores()
        {
            var scoreSaberReplaysLocation = Path.Combine(_fileService.UserDataLocation, "ScoreSaber", "Replays");

            if (!Directory.Exists(scoreSaberReplaysLocation))
                return;

            var replayFileName = Directory.EnumerateFiles(scoreSaberReplaysLocation, "*.dat").FirstOrDefault();

            if (string.IsNullOrEmpty(replayFileName))
                return;

            var replayFile = new FileInfo(replayFileName);

            var playerId = replayFile.Name
                .Split('-')
                .First();

            _playerId.OnNext(playerId);
        }
    }
}
