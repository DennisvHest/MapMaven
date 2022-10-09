using BeatSaberTools.Core.ApiClients;
using System.Reactive.Subjects;

namespace BeatSaberTools.Core.Services
{
    public class ScoreSaberService
    {
        private readonly ScoreSaberApiClient _scoreSaber;

        private readonly BehaviorSubject<IEnumerable<PlayerScore>> _playerScores = new(Array.Empty<PlayerScore>());

        public IObservable<IEnumerable<PlayerScore>> PlayerScores => _playerScores;

        public ScoreSaberService(ScoreSaberApiClient scoreSaber)
        {
            _scoreSaber = scoreSaber;
        }

        public async Task LoadPlayerScores(string playerId)
        {
            var scoreCollection = await _scoreSaber.Scores2Async(
                playerId: playerId,
                limit: 100,
                sort: Sort.Top,
                page: 1,
                withMetadata: true
            );

            _playerScores.OnNext(scoreCollection.PlayerScores);
        }
    }
}
