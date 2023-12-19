using MapMaven.Core.Services.Interfaces;
using System.Reactive.Linq;

namespace MapMaven.Core.Services.Leaderboards.ScoreEstimation
{
    public class ScoreEstimationSettings
    {
        private readonly IApplicationSettingService _applicationSettingService;

        public IObservable<int> DifficultyModifierValue { get; private set; }

        public ScoreEstimationSettings(IApplicationSettingService applicationSettingService)
        {
            _applicationSettingService = applicationSettingService;

            DifficultyModifierValue = _applicationSettingService.ApplicationSettings
                .Select(x => x.GetValueOrDefault("DifficultyModifier")?.GetValue<int>() ?? 0)
                .DistinctUntilChanged();
        }

        public async Task SetDifficultyModifierValueAsync(int value)
        {
            await _applicationSettingService.AddOrUpdateAsync("DifficultyModifier", value);
        }
    }
}
