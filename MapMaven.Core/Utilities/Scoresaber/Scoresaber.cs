using MapMaven.Core.ApiClients.ScoreSaber;
using MapMaven.Core.Models;
using MapMaven.Core.Models.Data.RankedMaps;

namespace MapMaven.Core.Utilities.Scoresaber
{
    public class Scoresaber
    {
        private readonly PlayerProfile _player;
        private readonly IEnumerable<PlayerScore> _playerScores;

        public const double PPDecay = .965D;
        public const double PPPerStar = 42.114296; // Approximation

        public static readonly List<PPCurveItem> PPCurve = new List<PPCurveItem>
        {
            new PPCurveItem { At = 0D, Value = 0D },
            new PPCurveItem { At = 60D, Value = 0.18223233667439062D },
            new PPCurveItem { At = 65D, Value = 0.5866010012767576D },
            new PPCurveItem { At = 70D, Value = 0.6125565959114954D },
            new PPCurveItem { At = 75D, Value = 0.6451808210101443D },
            new PPCurveItem { At = 80D, Value = 0.6872268862950283D },
            new PPCurveItem { At = 82.5D, Value = 0.7150465663454271D },
            new PPCurveItem { At = 85D, Value = 0.7462290664143185D },
            new PPCurveItem { At = 87.5D, Value = 0.7816934560296046D },
            new PPCurveItem { At = 90D, Value = 0.825756123560842D },
            new PPCurveItem { At = 91D, Value = 0.8488375988124467D },
            new PPCurveItem { At = 93D, Value = 0.9039994071865736D },
            new PPCurveItem { At = 94D, Value = 0.9417362980580238D },
            new PPCurveItem { At = 95D, Value = 1D },
            new PPCurveItem { At = 95.5D, Value = 1.0388633331418984D },
            new PPCurveItem { At = 96D, Value = 1.0871883573850478D },
            new PPCurveItem { At = 96.5D, Value = 1.1552120359501035D },
            new PPCurveItem { At = 97D, Value = 1.2485807759957321D },
            new PPCurveItem { At = 97.25D, Value = 1.3090333065057616D },
            new PPCurveItem { At = 97.5D, Value = 1.3807102743105126D },
            new PPCurveItem { At = 97.75D, Value = 1.4664726399289512D },
            new PPCurveItem { At = 98D, Value = 1.5702410055532239D },
            new PPCurveItem { At = 98.25D, Value = 1.697536248647543D },
            new PPCurveItem { At = 98.5D, Value = 1.8563887693647105D },
            new PPCurveItem { At = 98.75D, Value = 2.058947159052738D },
            new PPCurveItem { At = 99D, Value = 2.324506282149922D },
            new PPCurveItem { At = 99.125D, Value = 2.4902905794106913D },
            new PPCurveItem { At = 99.25D, Value = 2.685667856592722D },
            new PPCurveItem { At = 99.375D, Value = 2.9190155639254955D },
            new PPCurveItem { At = 99.5D, Value = 3.2022017597337955D },
            new PPCurveItem { At = 99.625D, Value = 3.5526145337555373D },
            new PPCurveItem { At = 99.75D, Value = 3.996793606763322D },
            new PPCurveItem { At = 99.825D, Value = 4.325027383589547D },
            new PPCurveItem { At = 99.9D, Value = 4.715470646416203D },
            new PPCurveItem { At = 99.95D, Value = 5.019543595874787D },
            new PPCurveItem { At = 100D, Value = 5.367394282890631D },
        };

        public Scoresaber(PlayerProfile player, IEnumerable<PlayerScore> playerScores)
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

        public ScoreEstimate GetScoreEstimate(RankedMapInfoItem map, RankedMapDifficultyInfo difficulty, double accuracy)
        {
            var estimatedPP = difficulty.MaxPP * ApplyCurve(accuracy);

            var totalPPEstimate = GetTotalPP(_playerScores, estimatedPP, new string[] { map.SongHash });

            return new ScoreEstimate
            {
                MapHash = map.SongHash,
                Accuracy = accuracy,
                Pp = estimatedPP,
                TotalPP = totalPPEstimate,
                PPIncrease = Math.Max(totalPPEstimate - _player.Pp, 0),
                Difficulty = difficulty.Difficulty,
                Stars = difficulty.Stars
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
