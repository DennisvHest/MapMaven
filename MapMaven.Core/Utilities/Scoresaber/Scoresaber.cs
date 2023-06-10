using MapMaven.Core.ApiClients;
using MapMaven.Core.Models.Data.ScoreSaber;

namespace MapMaven.Core.Utilities.Scoresaber
{
    public class Scoresaber
    {
        private readonly Player _player;
        private readonly IEnumerable<PlayerScore> _playerScores;

        public const decimal PPDecay = .965M;

        public static readonly List<PPCurveItem> PPCurve = new List<PPCurveItem>
        {
            new PPCurveItem { At = 0M, Value = 0M },
            new PPCurveItem { At = 60M, Value = 0.18223233667439062M },
            new PPCurveItem { At = 65M, Value = 0.5866010012767576M },
            new PPCurveItem { At = 70M, Value = 0.6125565959114954M },
            new PPCurveItem { At = 75M, Value = 0.6451808210101443M },
            new PPCurveItem { At = 80M, Value = 0.6872268862950283M },
            new PPCurveItem { At = 82.5M, Value = 0.7150465663454271M },
            new PPCurveItem { At = 85M, Value = 0.7462290664143185M },
            new PPCurveItem { At = 87.5M, Value = 0.7816934560296046M },
            new PPCurveItem { At = 90M, Value = 0.825756123560842M },
            new PPCurveItem { At = 91M, Value = 0.8488375988124467M },
            new PPCurveItem { At = 93M, Value = 0.9039994071865736M },
            new PPCurveItem { At = 94M, Value = 0.9417362980580238M },
            new PPCurveItem { At = 95M, Value = 1M },
            new PPCurveItem { At = 95.5M, Value = 1.0388633331418984M },
            new PPCurveItem { At = 96M, Value = 1.0871883573850478M },
            new PPCurveItem { At = 96.5M, Value = 1.1552120359501035M },
            new PPCurveItem { At = 97M, Value = 1.2485807759957321M },
            new PPCurveItem { At = 97.25M, Value = 1.3090333065057616M },
            new PPCurveItem { At = 97.5M, Value = 1.3807102743105126M },
            new PPCurveItem { At = 97.75M, Value = 1.4664726399289512M },
            new PPCurveItem { At = 98M, Value = 1.5702410055532239M },
            new PPCurveItem { At = 98.25M, Value = 1.697536248647543M },
            new PPCurveItem { At = 98.5M, Value = 1.8563887693647105M },
            new PPCurveItem { At = 98.75M, Value = 2.058947159052738M },
            new PPCurveItem { At = 99M, Value = 2.324506282149922M },
            new PPCurveItem { At = 99.125M, Value = 2.4902905794106913M },
            new PPCurveItem { At = 99.25M, Value = 2.685667856592722M },
            new PPCurveItem { At = 99.375M, Value = 2.9190155639254955M },
            new PPCurveItem { At = 99.5M, Value = 3.2022017597337955M },
            new PPCurveItem { At = 99.625M, Value = 3.5526145337555373M },
            new PPCurveItem { At = 99.75M, Value = 3.996793606763322M },
            new PPCurveItem { At = 99.825M, Value = 4.325027383589547M },
            new PPCurveItem { At = 99.9M, Value = 4.715470646416203M },
            new PPCurveItem { At = 99.95M, Value = 5.019543595874787M },
            new PPCurveItem { At = 100M, Value = 5.367394282890631M },
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
