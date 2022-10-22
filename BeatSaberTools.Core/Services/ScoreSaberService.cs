﻿using BeatSaberTools.Core.ApiClients;
using BeatSaberTools.Core.Models.Data.ScoreSaber;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace BeatSaberTools.Core.Services
{
    public class ScoreSaberService
    {
        private readonly ScoreSaberApiClient _scoreSaber;
        private readonly IBeatSaverFileService _fileService;
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly BehaviorSubject<string?> _playerId = new(null);
        private readonly BehaviorSubject<IEnumerable<RankedMap>> _rankedMaps = new(Enumerable.Empty<RankedMap>());

        public readonly IObservable<IEnumerable<PlayerScore>> PlayerScores;
        public IObservable<IEnumerable<RankedMap>> RankedMaps => _rankedMaps;

        private const string _replayBaseUrl = "https://www.replay.beatleader.xyz";

        public ScoreSaberService(
            ScoreSaberApiClient scoreSaber,
            IBeatSaverFileService fileService,
            IHttpClientFactory httpClientFactory)
        {
            _scoreSaber = scoreSaber;
            _fileService = fileService;
            _httpClientFactory = httpClientFactory;

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

        public async Task LoadRankedMaps()
        {
            var rankedMaps = await GetRankedMaps();

            _rankedMaps.OnNext(rankedMaps);
        }

        public async Task<IEnumerable<RankedMap>> GetRankedMaps()
        {
            var httpClient = _httpClientFactory.CreateClient("RankedScoresaber");

            var response = await httpClient.GetFromJsonAsync<RankedMapResponse>("/ranked");

            return response?.List ?? Enumerable.Empty<RankedMap>();
        }

        public string? GetScoreSaberReplayUrl(string mapId, PlayerScore score)
        {
            if (!score.Score.HasReplay)
                return null;

            return $"{_replayBaseUrl}/?id={mapId}&difficulty={score.Leaderboard.Difficulty.DifficultyName}&playerID={_playerId.Value}";
        }
    }
}
