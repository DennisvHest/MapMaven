using MapMaven.Core.Models.DynamicPlaylists.MapInfo;
using MapMaven.Core.Models.DynamicPlaylists;
using System.Reflection;
using System.ComponentModel;
using MapMaven.Utilities.DynamicPlaylists;

namespace MapMaven.Utility
{
    public static class DynamicPlaylistFields
    {
        public static IEnumerable<DynamicPlaylistFieldOption> FieldOptions(MapPool mapPool) => GetFieldOptionsForType(typeof(DynamicPlaylistMap), mapPool);

        private static IEnumerable<DynamicPlaylistFieldOption> GetFieldOptionsForType(Type type, MapPool mapPool, string? parentObjectName = null)
        {
            return type.GetProperties().SelectMany(property =>
            {
                if (property.PropertyType.IsClass && property.PropertyType != typeof(string))
                    return GetFieldOptionsForType(property.PropertyType, mapPool, parentObjectName: property.Name);

                var applicableForMapPoolAttribute = property.GetCustomAttribute<ApplicableForMapPoolAttribute>();

                if (applicableForMapPoolAttribute != null && !applicableForMapPoolAttribute.MapPools.Contains(mapPool))
                    return Array.Empty<DynamicPlaylistFieldOption>();

                var value = parentObjectName != null ? $"{parentObjectName}.{property.Name}" : property.Name;

                var displayNameAttribute = property.GetCustomAttribute<DisplayNameAttribute>();

                var name = displayNameAttribute?.DisplayName ?? property.Name;

                return new[]
                {
                    new DynamicPlaylistFieldOption
                    {
                        Value = value,
                        Name = name,
                        Type = property.PropertyType
                    }
                };
            });
        }
    }
}
