using MapMaven.Core.ApiClients;
using MapMaven.Core.Models.Data.ScoreSaber;
using MapMaven.Core.Utilities.Scoresaber;

namespace MapMaven.Core.Services.Interfaces
{
    public interface IScoreSaberService
    {
        string? PlayerId { get; }
        IObservable<string?> PlayerIdObservable { get; }
        IObservable<IEnumerable<RankedMap>> RankedMaps { get; }
        IObservable<Player?> PlayerProfile { get; }
        IObservable<IEnumerable<PlayerScore>> PlayerScores { get; }
        IObservable<IEnumerable<ScoreEstimate>> ScoreEstimates { get; }
        IObservable<IEnumerable<ScoreEstimate>> RankedMapScoreEstimates { get; }

        string? GetPlayerIdFromReplays(string beatSaberInstallLocation);
        Task<IEnumerable<RankedMap>> GetRankedMaps();
        string? GetScoreSaberReplayUrl(string mapId, PlayerScore score);
        Task LoadRankedMaps();
        void RefreshPlayerData();
        Task SetPlayerId(string playerId);
    }
}