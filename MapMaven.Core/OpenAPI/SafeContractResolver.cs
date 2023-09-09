using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System.Reflection;

namespace MapMaven.Core.OpenAPI
{
    /// <summary>
    /// Ignore required properties validation, to fix invalid nullability in OpenAPI json.
    /// </summary>
    public class SafeContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var jsonProp = base.CreateProperty(member, memberSerialization);
            jsonProp.Required = Required.Default;
            return jsonProp;
        }
    }
}
