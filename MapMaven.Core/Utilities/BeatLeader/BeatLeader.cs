using MapMaven.Core.Models;
using MapMaven.Core.Models.Data.Leaderboards;
using MapMaven.Core.Models.Data.Leaderboards.BeatLeader;
using MapMaven.Core.Models.Data.RankedMaps;
using static System.Math;

namespace MapMaven.Core.Utilities.BeatLeader
{
    public class BeatLeader
    {
        private readonly PlayerProfile _player;
        private readonly IEnumerable<PlayerScore> _playerScores;

        private BeatLeaderLeaderboardData _leaderboardData;

        public BeatLeader(PlayerProfile player, IEnumerable<PlayerScore> playerScores, BeatLeaderLeaderboardData leaderboardData)
        {
            _player = player;
            _playerScores = playerScores;
            _leaderboardData = leaderboardData;
        }

        /// <summary>
        /// Gets the total of the PP from the given scores and applies the PP decay rules from BeatLeader.
        /// </summary>
        /// <param name="scores">Included scores.</param>
        /// <param name="newPP">New PP score to add to the total.</param>
        /// <param name="replaceMapIds">Map ids of which the PP should not be included in the total.</param>
        /// <returns></returns>
        public double GetTotalPP(IEnumerable<PlayerScore> scores, double newPP = 0, IEnumerable<string>? replaceMapIds = null)
        {
            if (replaceMapIds == null)
                replaceMapIds = Enumerable.Empty<string>();

            if (replaceMapIds.Any())
                scores = scores.Where(s => !replaceMapIds.Contains(s.Leaderboard.SongHash));

            var ppDecayMultiplier = 1D;

            /*
             * Total PP is calculated as follows. PP Scores are sorted in descending order and summed.
             * Every time a PP score is summed, a decay value is applied (PPDecay), so lowest scores
             * weigh even less.
             */
            return scores
                .Select(s => s.Score.Pp)
                .Concat(new double[] { newPP })
                .OrderByDescending(pp => pp)
                .Select((pp, index) => new { PP = pp, Index = index })
                .Aggregate(0D, (total, score) =>
                {
                    ppDecayMultiplier *= score.Index != 0 ? _leaderboardData.PpDecay : 1;

                    return total + score.PP * ppDecayMultiplier;
                });
        }

        public ScoreEstimate GetScoreEstimate(RankedMapInfoItem map, RankedMapDifficultyInfo difficulty, double accuracy)
        {
            accuracy /= 100;

            var passPP = GetPassPP(difficulty.BeatLeaderRating.PassRating);

            var accPP = GetAccPP(difficulty.BeatLeaderRating.AccRating, accuracy);
            var techPP = GetTechPP(difficulty.BeatLeaderRating.TechRating, accuracy);

            var estimatedPP = Inflate(passPP + accPP + techPP);

            if (double.IsInfinity(estimatedPP) || double.IsNaN(estimatedPP) || double.IsNegativeInfinity(estimatedPP))
                estimatedPP = 0;

            var totalPPEstimate = GetTotalPP(_playerScores, estimatedPP, new string[] { map.SongHash });

            return new ScoreEstimate
            {
                MapHash = map.SongHash,
                Accuracy = accuracy * 100,
                Pp = estimatedPP,
                TotalPP = totalPPEstimate,
                PPIncrease = Max(totalPPEstimate - _player.Pp, 0),
                Difficulty = difficulty.Difficulty,
                Stars = difficulty.Stars
            };
        }

        private double GetPassPP(double passRating)
        {
            var passPP = _leaderboardData.PassMultiplier * Exp(Pow(passRating, _leaderboardData.PassExponential)) + _leaderboardData.PassShift;

            if (double.IsInfinity(passPP) || double.IsNaN(passPP) || double.IsNegativeInfinity(passPP) || passPP < 0)
                passPP = 0;

            return passPP;
        }

        private double GetAccPP(double accRating, double accuracy) => ApplyCurve(accuracy) * accRating * _leaderboardData.AccMultiplier;

        private double GetTechPP(double techRating, double accuracy) => Exp(_leaderboardData.TechExponentialMultiplier * accuracy) * _leaderboardData.TechMultiplier * techRating;

        private double Inflate(double pp) => _leaderboardData.InflateMultiplier * Pow(pp, _leaderboardData.InflateExponential) / Pow(_leaderboardData.InflateMultiplier, _leaderboardData.InflateExponential);

        /// <summary>
        /// Calculates the multiplication value for the given accuracy. This value can then be used to calculate the acual PP gain.
        /// </summary>
        /// <param name="accuracy">The accuracy to calculate the multiplication value from.</param>
        /// <returns>The multiplication value.</returns>
        private double ApplyCurve(double accuracy)
        {
            var curveItem = _leaderboardData.AccCurve.FirstOrDefault(x => x.At >= accuracy);

            // Impossible accuracy, but just return the last value
            if (curveItem == null)
                return _leaderboardData.AccCurve.Last().Value;

            var index = Array.IndexOf(_leaderboardData.AccCurve, curveItem);

            // If the accuracy is the lowest possbile, just return the first value in the curve
            if (index == 0)
                return _leaderboardData.AccCurve.First().Value;

            var from = _leaderboardData.AccCurve[index - 1];
            var to = _leaderboardData.AccCurve[index];

            // Calculate the PP multiplier value at the given accuracy
            var progress = (accuracy - from.At) / (to.At - from.At);

            return from.Value + (to.Value - from.Value) * progress;
        }
    }
}
