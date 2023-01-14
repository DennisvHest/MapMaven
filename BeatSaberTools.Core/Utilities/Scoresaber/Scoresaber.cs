using BeatSaberTools.Core.ApiClients;
using BeatSaberTools.Core.Models.Data.ScoreSaber;

namespace BeatSaberTools.Core.Utilities.Scoresaber
{
    public class Scoresaber
    {
        private readonly Player _player;
        private readonly IEnumerable<PlayerScore> _playerScores;

        public const decimal PPDecay = .965M;

        public static readonly List<PPCurveItem> PPCurve = new List<PPCurveItem>
        {
            new PPCurveItem { At = 0M, Value = 0M },
            new PPCurveItem { At = 59.86M, Value = 0.24M },
            new PPCurveItem { At = 64.9M, Value = 0.31M },
            new PPCurveItem { At = 69.87M, Value = 0.34M },
            new PPCurveItem { At = 74.92M, Value = 0.4M },
            new PPCurveItem { At = 79.89M, Value = 0.47M },
            new PPCurveItem { At = 82.37M, Value = 0.5M },
            new PPCurveItem { At = 84.86M, Value = 0.56M },
            new PPCurveItem { At = 87.42M, Value = 0.65M },
            new PPCurveItem { At = 89.9M, Value = 0.75M },
            new PPCurveItem { At = 90.95M, Value = 0.79M },
            new PPCurveItem { At = 91.91M, Value = 0.83M },
            new PPCurveItem { At = 92.87M, Value = 0.87M },
            new PPCurveItem { At = 93.91M, Value = 0.92M },
            new PPCurveItem { At = 94.95M, Value = 0.99M },
            new PPCurveItem { At = 95.51M, Value = 1.06M },
            new PPCurveItem { At = 96.07M, Value = 1.12M },
            new PPCurveItem { At = 96.55M, Value = 1.21M },
            new PPCurveItem { At = 96.96M, Value = 1.3M },
            new PPCurveItem { At = 97.36M, Value = 1.4M },
            new PPCurveItem { At = 97.68M, Value = 1.5M },
            new PPCurveItem { At = 98M, Value = 1.62M },
            new PPCurveItem { At = 98.16M, Value = 1.76M },
            new PPCurveItem { At = 98.4M, Value = 1.99M },
            new PPCurveItem { At = 98.72M, Value = 2.32M },
            new PPCurveItem { At = 98.88M, Value = 2.72M },
            new PPCurveItem { At = 99.28M, Value = 3.3M },
            new PPCurveItem { At = 99.52M, Value = 4.13M },
            new PPCurveItem { At = 99.68M, Value = 5.3M },
            new PPCurveItem { At = 99.76M, Value = 6.23M },
            new PPCurveItem { At = 100M, Value = 7M },
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
        public static decimal GetTotalPP(IEnumerable<PlayerScore> scores, decimal newPP = 0, IEnumerable<string>? replaceMapIds = null)
        {
            if (replaceMapIds == null)
                replaceMapIds = Enumerable.Empty<string>();

            if (replaceMapIds.Any())
                scores = scores.Where(s => !replaceMapIds.Contains(s.Leaderboard.SongHash));

            var ppDecayMultiplier = 1M;

            /*
             * Total PP is calculated as follows. PP Scores are sorted in descending order and summed.
             * Every time a PP score is summed, a decay value is applied (PPDecay), so lowest scores
             * weigh even less.
             */
            return scores
                .Select(s => Convert.ToDecimal(s.Score.Pp))
                .Concat(new decimal[] { newPP })
                .OrderByDescending(pp => pp)
                .Select((pp, index) => new { PP = pp, Index = index })
                .Aggregate(0M, (total, score) =>
                {
                    ppDecayMultiplier *= score.Index != 0 ? PPDecay : 1;

                    return total + score.PP * ppDecayMultiplier;
                });
        }

        public ScoreEstimate GetScoreEstimate(RankedMap map, decimal accuracy)
        {
            var estimatedPP = map.PP * ApplyCurve(accuracy);

            var totalPPEstimate = GetTotalPP(_playerScores, estimatedPP, new string[] { map.Id });

            return new ScoreEstimate
            {
                MapId = map.Id,
                Accuracy = accuracy,
                PP = estimatedPP,
                TotalPP = totalPPEstimate,
                PPIncrease = Math.Max(totalPPEstimate - Convert.ToDecimal(_player.Pp), 0),
                Difficulty = map.Difficulty,
                Stars = map.Stars
            };
        }

        /// <summary>
        /// Calculates the multiplication value for the given accuracy. This value can then be used to calculate the acual PP gain.
        /// </summary>
        /// <param name="accuracy">The accuracy to calculate the multiplication value from.</param>
        /// <returns>The multiplication value.</returns>
        private static decimal ApplyCurve(decimal accuracy)
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
