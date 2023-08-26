using Newtonsoft.Json;
using MapMaven.Core.OpenAPI;

namespace MapMaven.Core.ApiClients.ScoreSaber
{
    public partial class ScoreSaberApiClient
    {
        partial void UpdateJsonSerializerSettings(JsonSerializerSettings settings)
        {
            settings.ContractResolver = new SafeContractResolver();
        }
    }
}
