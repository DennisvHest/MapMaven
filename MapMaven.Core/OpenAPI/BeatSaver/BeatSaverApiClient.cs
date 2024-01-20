using Newtonsoft.Json;

namespace MapMaven.Core.ApiClients.BeatSaver
{
    public partial class BeatSaverApiClient
    {
        static partial void UpdateJsonSerializerSettings(JsonSerializerSettings settings)
        {
            settings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
        }
    }
}
