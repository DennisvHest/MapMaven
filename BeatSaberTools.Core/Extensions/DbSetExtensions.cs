using BeatSaberTools.Core.Models.Data;
using Microsoft.EntityFrameworkCore;

namespace BeatSaberTools.Core.Extensions
{
    public static class DbSetExtensions
    {
        public static async Task AddOrUpdateAsync<T>(this DbSet<ApplicationSetting> applicationSettings, string key, T value) where T : class
        {
            ApplicationSetting applicationSetting;

            var existing = await applicationSettings.FirstOrDefaultAsync(x => x.Key == key);

            if (existing == null)
            {
                applicationSetting = new ApplicationSetting();
            }
            else
            {
                applicationSetting = existing;
            }

            applicationSetting.Key = key;
            applicationSetting.StringValue = value is string stringValue ? stringValue : null;

            if (existing == null)
            {
                applicationSettings.Add(applicationSetting);
            }
            else
            {
                applicationSettings.Update(applicationSetting);
            }
        }
    }
}
