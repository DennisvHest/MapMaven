using MapMaven.Core.Models;
using MapMaven.Core.Models.Data.Leaderboards;

namespace MapMaven.Core.Services.Leaderboards.ScoreEstimation
{
    public interface IScoreEstimationService
    {
        IObservable<IEnumerable<ScoreEstimate>> RankedMapScoreEstimates { get; }
        LeaderboardProvider LeaderboardProviderName { get; }
    }
}