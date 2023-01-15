using BeatSaberTools.Core.Extensions;
using BeatSaberTools.Core.Models.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reactive.Subjects;

namespace BeatSaberTools.Core.Services
{
    public class ApplicationSettingService
    {
        private readonly IServiceProvider _serviceProvider;

        private BehaviorSubject<Dictionary<string, ApplicationSetting>> _applicationSettings = new(new Dictionary<string, ApplicationSetting>());
        public IObservable<Dictionary<string, ApplicationSetting>> ApplicationSettings => _applicationSettings;

        public ApplicationSettingService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task LoadAsync()
        {
            using var scope = _serviceProvider.CreateScope();

            var dataStore = scope.ServiceProvider.GetRequiredService<IDataStore>();

            var settings = await dataStore
                .Set<ApplicationSetting>()
                .ToListAsync();

            _applicationSettings.OnNext(settings.ToDictionary(s => s.Key));
        }

        public async Task AddOrUpdateAsync<T>(string key, T value) where T : class
        {
            using var scope = _serviceProvider.CreateScope();

            var dataStore = scope.ServiceProvider.GetRequiredService<IDataStore>();

            await dataStore
                .Set<ApplicationSetting>()
                .AddOrUpdateAsync(key, value);

            await dataStore.SaveChangesAsync();

            await LoadAsync();
        }
    }
}
