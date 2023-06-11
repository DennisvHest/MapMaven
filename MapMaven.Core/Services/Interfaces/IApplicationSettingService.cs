using MapMaven.Core.Models.Data;

namespace MapMaven.Core.Services.Interfaces
{
    public interface IApplicationSettingService
    {
        IObservable<Dictionary<string, ApplicationSetting>> ApplicationSettings { get; }

        Task AddOrUpdateAsync<T>(string key, T value) where T : class;
        Task LoadAsync();
    }
}