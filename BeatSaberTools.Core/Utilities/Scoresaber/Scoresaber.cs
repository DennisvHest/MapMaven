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
            new PPCurveItem { At = 45M, Value = 0.015M },
            new PPCurveItem { At = 50M, Value = 0.03M },
            new PPCurveItem { At = 55M, Value = 0.06M },
            new PPCurveItem { At = 60M, Value = 0.105M },
            new PPCurveItem { At = 65M, Value = 0.16M },
            new PPCurveItem { At = 68M, Value = 0.24M },
            new PPCurveItem { At = 70M, Value = 0.285M },
            new PPCurveItem { At = 80M, Value = 0.563M },
            new PPCurveItem { At = 84M, Value = 0.695M },
            new PPCurveItem { At = 88M, Value = 0.826M },
            new PPCurveItem { At = 94.5M, Value = 1.015M },
            new PPCurveItem { At = 95M, Value = 1.046M },
            new PPCurveItem { At = 100M, Value = 1.12M },
            new PPCurveItem { At = 110M, Value = 1.18M },
            new PPCurveItem { At = 114M, Value = 1.25M },
        };

        public Scoresaber(Player player, IEnumerable<PlayerScore> playerScores)
        {
            _player = player;
            _playerScores = playerScores;
        }

        public ScoreEstimate GetScoreEstimate(RankedMap map)
        {
            return new ScoreEstimate
            {
                MapId = map.Id,
                Accuracy = 0,
                PP = 0,
                TotalPP = 0,
                PPIncrease = 0,
                Difficulty = map.Difficulty,
                Stars = map.Stars
            };

            var now = DateTimeOffset.Now;
            var decay = TimeSpan.FromDays(15).TotalMilliseconds;

            var estimatedPercentage = _playerScores.Average(score =>
            {
                var starFactor = 1 / (map.Stars / Convert.ToDecimal(score.Leaderboard.Stars));

                var accuracy = score.AccuracyWithMods();

                // Scores that were set longer ago, don't weigh as much in the comparison between star difficulties, compared to scores set more recently.
                var at = score.Score.TimeSet != default ? score.Score.TimeSet : now;
                var time = (now - at).TotalMilliseconds / decay;

                var timeFactor = 1 / Math.Pow(0.9, time);

                return accuracy * starFactor * Convert.ToDecimal(timeFactor);
            });

            var estimatedPP = map.PP * ApplyCurve(estimatedPercentage);

            var totalPPEstimate = GetTotalPP(_playerScores, estimatedPP, new string[] { map.Id });

            return new ScoreEstimate
            {
                MapId = map.Id,
                Accuracy = estimatedPercentage,
                PP = estimatedPP,
                TotalPP = totalPPEstimate,
                PPIncrease = Math.Max(totalPPEstimate - Convert.ToDecimal(_player.Pp), 0),
                Difficulty = map.Difficulty,
                Stars = map.Stars
            };
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
