using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System.Reflection;

namespace MapMaven.Core.ApiClients
{
    public partial class ScoreSaberApiClient
    {
        partial void UpdateJsonSerializerSettings(JsonSerializerSettings settings)
        {
            settings.ContractResolver = new SafeContractResolver();
        }

        /// <summary>
        /// Ignore required properties validation, to fix invalid nullability in OpenAPI json.
        /// </summary>
        class SafeContractResolver : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var jsonProp = base.CreateProperty(member, memberSerialization);
                jsonProp.Required = Required.Default;
                return jsonProp;
            }
        }
    }
}
