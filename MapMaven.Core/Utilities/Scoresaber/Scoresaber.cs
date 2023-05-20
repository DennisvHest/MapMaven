using MapMaven.Core.ApiClients;
using MapMaven.Core.Models.Data.ScoreSaber;

namespace MapMaven.Core.Utilities.Scoresaber
{
    public class Scoresaber
    {
        private readonly Player _player;
        private readonly IEnumerable<PlayerScore> _playerScores;

        public const double PPDecay = .965D;

        public static readonly List<PPCurveItem> PPCurve = new List<PPCurveItem>
        {
            new PPCurveItem { At = 0D, Value = 0D },
            new PPCurveItem { At = 59.86D, Value = 0.24D },
            new PPCurveItem { At = 64.9D, Value = 0.31D },
            new PPCurveItem { At = 69.87D, Value = 0.34D },
            new PPCurveItem { At = 74.92D, Value = 0.4D },
            new PPCurveItem { At = 79.89D, Value = 0.47D },
            new PPCurveItem { At = 82.37D, Value = 0.5D },
            new PPCurveItem { At = 84.86D, Value = 0.56D },
            new PPCurveItem { At = 87.42D, Value = 0.65D },
            new PPCurveItem { At = 89.9D, Value = 0.75D },
            new PPCurveItem { At = 90.95D, Value = 0.79D },
            new PPCurveItem { At = 91.91D, Value = 0.83D },
            new PPCurveItem { At = 92.87D, Value = 0.87D },
            new PPCurveItem { At = 93.91D, Value = 0.92D },
            new PPCurveItem { At = 94.95D, Value = 0.99D },
            new PPCurveItem { At = 95.51D, Value = 1.06D },
            new PPCurveItem { At = 96.07D, Value = 1.12D },
            new PPCurveItem { At = 96.55D, Value = 1.21D },
            new PPCurveItem { At = 96.96D, Value = 1.3D },
            new PPCurveItem { At = 97.36D, Value = 1.4D },
            new PPCurveItem { At = 97.68D, Value = 1.5D },
            new PPCurveItem { At = 98D, Value = 1.62D },
            new PPCurveItem { At = 98.16D, Value = 1.76D },
            new PPCurveItem { At = 98.4D, Value = 1.99D },
            new PPCurveItem { At = 98.72D, Value = 2.32D },
            new PPCurveItem { At = 98.88D, Value = 2.72D },
            new PPCurveItem { At = 99.28D, Value = 3.3D },
            new PPCurveItem { At = 99.52D, Value = 4.13D },
            new PPCurveItem { At = 99.68D, Value = 5.3D },
            new PPCurveItem { At = 99.76D, Value = 6.23D },
            new PPCurveItem { At = 100D, Value = 7D },
        };

        public Scoresaber(Player player, IEnumerable<PlayerScore> playerScores)
        {
            _player = player;
            _playerScores = playerScores;
        }

        /// <summary>
        /// Gets the total of the PP from the given scores and applies the PP decay rules from ScoreSaber.
        /// </summary>
        /// <param name="scores">Included scores.</param>
        /// <param name="newPP">New PP score to add to the total.</param>
        /// <param name="replaceMapIds">Map ids of which the PP should not be included in the total.</param>
        /// <returns></returns>
        public static double GetTotalPP(IEnumerable<PlayerScore> scores, double newPP = 0, IEnumerable<string>? replaceMapIds = null)
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
                    ppDecayMultiplier *= score.Index != 0 ? PPDecay : 1;

                    return total + score.PP * ppDecayMultiplier;
                });
        }

        public ScoreEstimate GetScoreEstimate(RankedMap map, double accuracy)
        {
            var estimatedPP = map.PP * ApplyCurve(accuracy);

            var totalPPEstimate = GetTotalPP(_playerScores, estimatedPP, new string[] { map.Id });

            return new ScoreEstimate
            {
                MapId = map.Id,
                Accuracy = accuracy,
                Pp = estimatedPP,
                TotalPP = totalPPEstimate,
                PPIncrease = Math.Max(totalPPEstimate - _player.Pp, 0),
                Difficulty = map.Difficulty,
                Stars = map.Stars
            };
        }

        /// <summary>
        /// Calculates the multiplication value for the given accuracy. This value can then be used to calculate the acual PP gain.
        /// </summary>
        /// <param name="accuracy">The accuracy to calculate the multiplication value from.</param>
        /// <returns>The multiplication value.</returns>
        private static double ApplyCurve(double accuracy)
        {
            var curveItem = PPCurve.FirstOrDefault(x => x.At >= accuracy);

            // Impossible accuracy, but just return the last value
            if (curveItem == null)
                return PPCurve.Last().Value;

            var index = PPCurve.IndexOf(curveItem);

            // If the accuracy is the lowest possbile, just return the first value in the curve
            if (index == 0)
                return PPCurve.First().Value;

            var from = PPCurve[index - 1];
            var to = PPCurve[index];

            // Calculate the PP multiplier value at the given accuracy
            var progress = (accuracy - from.At) / (to.At - from.At);

            return from.Value + (to.Value - from.Value) * progress;
        }
    }
}
