﻿using MapMaven.Core.OpenAPI;
using Newtonsoft.Json;

namespace MapMaven.Core.ApiClients.BeatLeader
{
    public partial class BeatLeaderApiClient
    {
        public BeatLeaderApiClient() { }

        partial void UpdateJsonSerializerSettings(JsonSerializerSettings settings)
        {
            settings.ContractResolver = new SafeContractResolver();
        }
    }
}
