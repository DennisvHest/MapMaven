using BeatSaberTools.Core.ApiClients;
using BeatSaberTools.Core.Models.Data.ScoreSaber;

namespace BeatSaberTools.Core.Utilities.Scoresaber
{
    public static class Scoresaber_old
    {
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
        /// Returns a <see cref="ScoreEstimate"/> for the given map based on the given existing scores.
        /// </summary>
        /// <param name="map">Map to get the score estimate for.</param>
        /// <param name="mapScores">Existing scores.</param>
        /// <returns>A <see cref="ScoreEstimate"/> containing estimated accuracy, PP gain and total PP.</returns>
        public static ScoreEstimate GetScoreEstimate(RankedMap map, IEnumerable<RankedMapScorePair> mapScores, decimal currentTotalPP)
        {
            var now = DateTimeOffset.Now;
            var decay = 1000 * 60 * 60 * 24 * 15;
            var maxStars = mapScores.Max(s => s.Map.Stars);

            (decimal Weight, decimal Sum) total = (0, 0);

            var data = mapScores.Aggregate(total, (runningTotal, mapPlayerScore) =>
            {
                // If the map has a higher star difficulty than the existing score map, it weighs more.
                var d = 2 * Math.Abs(map.Stars - mapPlayerScore.Map.Stars);
                var front = map.Stars > mapPlayerScore.Map.Stars ? d * d * d : 1;

                // Scores that were set longer ago, don't weigh as much in the comparison between star difficulties, compared to scores set more recently.
                var at = mapPlayerScore.PlayerScore.Score.TimeSet != default ? mapPlayerScore.PlayerScore.Score.TimeSet : now;
                var time = 1 + Math.Max(now.ToUnixTimeMilliseconds() - at.ToUnixTimeMilliseconds(), 0) / decay;

                var weight = 1 / (1 + d * time * front);

                runningTotal.Weight += weight;
                runningTotal.Sum += mapPlayerScore.PlayerScore.AccuracyWithMods() * weight;

                return runningTotal;
            });

            var result = data.Weight != 0 ? data.Sum / data.Weight : 0;

            // If the map is more difficult than the most difficult map that has been scored on, it weighs more towards a higher score.
            if (map.Stars > maxStars)
            {
                var d = 2 * Math.Abs(map.Stars - maxStars);
                result /= (1 + d * d);
            }

            var estimatedPP = map.PP * ApplyCurve(result);

            var totalPPEstimate = GetTotalPP(mapScores.Select(m => m.PlayerScore), estimatedPP, new string[] { map.Id });

            return new ScoreEstimate
            {
                MapId = map.Id,
                Accuracy = result,
                PP = estimatedPP,
                TotalPP = totalPPEstimate,
                PPIncrease = Math.Max(totalPPEstimate - currentTotalPP, 0),
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
