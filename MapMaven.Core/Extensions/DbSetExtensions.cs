using MapMaven.Core.Models.Data;
using Microsoft.EntityFrameworkCore;

namespace MapMaven.Core.Extensions
{
    public static class DbSetExtensions
    {
        public static async Task AddOrUpdateAsync<T>(this DbSet<ApplicationSetting> applicationSettings, string key, T value)
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

            applicationSetting.StringValue = value switch
            {
                string => value as string,
                int => value.ToString(),
                _ => null
            };

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
