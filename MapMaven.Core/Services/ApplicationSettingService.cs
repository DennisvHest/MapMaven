﻿using MapMaven.Core.Extensions;
using MapMaven.Core.Models.Data;
using MapMaven.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reactive.Subjects;

namespace MapMaven.Core.Services
{
    public class ApplicationSettingService : IApplicationSettingService
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

        public async Task AddOrUpdateAsync<T>(string key, T value)
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
