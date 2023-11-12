using MapMaven.Core.Models;
using MapMaven.Core.Models.Data.RankedMaps;
using MapMaven.Core.Utilities.Scoresaber;
using static System.Math;

namespace MapMaven.Core.Utilities.BeatLeader
{
    public class BeatLeader
    {
        private readonly PlayerProfile _player;
        private readonly IEnumerable<PlayerScore> _playerScores;

        public const double PPDecay = .965D;

        public static readonly List<PPCurveItem> AccCurve = new List<PPCurveItem>
        {
            new PPCurveItem { At = 0, Value = 0 },
            new PPCurveItem { At = 0.6, Value = 0.256 },
            new PPCurveItem { At = 0.65, Value = 0.296 },
            new PPCurveItem { At = 0.7, Value = 0.345 },
            new PPCurveItem { At = 0.75, Value = 0.404 },
            new PPCurveItem { At = 0.8, Value = 0.473 },
            new PPCurveItem { At = 0.825, Value = 0.522 },
            new PPCurveItem { At = 0.85, Value = 0.581 },
            new PPCurveItem { At = 0.875, Value = 0.65 },
            new PPCurveItem { At = 0.9, Value = 0.729 },
            new PPCurveItem { At = 0.91, Value = 0.768 },
            new PPCurveItem { At = 0.92, Value = 0.813 },
            new PPCurveItem { At = 0.93, Value = 0.867 },
            new PPCurveItem { At = 0.94, Value = 0.931 },
            new PPCurveItem { At = 0.95, Value = 1 },
            new PPCurveItem { At = 0.955, Value = 1.039 },
            new PPCurveItem { At = 0.96, Value = 1.094 },
            new PPCurveItem { At = 0.965, Value = 1.167 },
            new PPCurveItem { At = 0.97, Value = 1.256 },
            new PPCurveItem { At = 0.9725, Value = 1.315 },
            new PPCurveItem { At = 0.975, Value = 1.392 },
            new PPCurveItem { At = 0.9775, Value = 1.49 },
            new PPCurveItem { At = 0.98, Value = 1.618 },
            new PPCurveItem { At = 0.9825, Value = 1.786 },
            new PPCurveItem { At = 0.985, Value = 2.007 },
            new PPCurveItem { At = 0.9875, Value = 2.303 },
            new PPCurveItem { At = 0.99, Value = 2.7 },
            new PPCurveItem { At = 0.9925, Value = 3.241 },
            new PPCurveItem { At = 0.995, Value = 4.01 },
            new PPCurveItem { At = 0.9975, Value = 5.158 },
            new PPCurveItem { At = 0.999, Value = 6.241 },
            new PPCurveItem { At = 1, Value = 7.424 }
        };

        private double _passMultiplier = 15.2;
        private double _passExponential = 0.381679;
        private double _passShift = -30;
        private double _accMultiplier = 34;
        private double _techMultiplier = 1.08;
        private double _techExponentialMultiplier = 1.9;
        private double _inflateMultiplier = 650;
        private double _inflateExponential = 1.3;

        public BeatLeader(PlayerProfile player, IEnumerable<PlayerScore> playerScores)
        {
            _player = player;
            _playerScores = playerScores;
        }

        /// <summary>
        /// Gets the total of the PP from the given scores and applies the PP decay rules from BeatLeader.
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
            var passPP = _passMultiplier * Exp(Pow(passRating, _passExponential)) + _passShift;

            if (double.IsInfinity(passPP) || double.IsNaN(passPP) || double.IsNegativeInfinity(passPP) || passPP < 0)
                passPP = 0;

            return passPP;
        }

        private double GetAccPP(double accRating, double accuracy) => ApplyCurve(accuracy) * accRating * _accMultiplier;

        private double GetTechPP(double techRating, double accuracy) => Exp(_techExponentialMultiplier * accuracy) * _techMultiplier * techRating;

        private double Inflate(double pp) => _inflateMultiplier * Pow(pp, _inflateExponential) / Pow(_inflateMultiplier, _inflateExponential);

        /// <summary>
        /// Calculates the multiplication value for the given accuracy. This value can then be used to calculate the acual PP gain.
        /// </summary>
        /// <param name="accuracy">The accuracy to calculate the multiplication value from.</param>
        /// <returns>The multiplication value.</returns>
        private static double ApplyCurve(double accuracy)
        {
            var curveItem = AccCurve.FirstOrDefault(x => x.At >= accuracy);

            // Impossible accuracy, but just return the last value
            if (curveItem == null)
                return AccCurve.Last().Value;

            var index = AccCurve.IndexOf(curveItem);

            // If the accuracy is the lowest possbile, just return the first value in the curve
            if (index == 0)
                return AccCurve.First().Value;

            var from = AccCurve[index - 1];
            var to = AccCurve[index];

            // Calculate the PP multiplier value at the given accuracy
            var progress = (accuracy - from.At) / (to.At - from.At);

            return from.Value + (to.Value - from.Value) * progress;
        }
    }
}
