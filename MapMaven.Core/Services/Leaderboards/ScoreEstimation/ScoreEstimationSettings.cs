using System.Reactive.Subjects;

namespace MapMaven.Core.Services.Leaderboards.ScoreEstimation
{
    public class ScoreEstimationSettings
    {
        public BehaviorSubject<int> DifficultyModifierValue { get; set; } = new(0);
    }
}
