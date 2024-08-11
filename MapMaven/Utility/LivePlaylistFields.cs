using MapMaven.Core.Models.LivePlaylists.MapInfo;
using MapMaven.Core.Models.LivePlaylists;
using System.Reflection;
using System.ComponentModel;
using MapMaven.Utilities.LivePlaylists;
using MapMaven.Core.Utilities.LivePlaylists;
using System.Collections;

namespace MapMaven.Utility
{
    public static class LivePlaylistFields
    {
        public static IEnumerable<LivePlaylistFieldOption> FieldOptions(MapPool mapPool) => GetFieldOptionsForType(typeof(AdvancedSearchMap), mapPool);

        private static IEnumerable<LivePlaylistFieldOption> GetFieldOptionsForType(Type type, MapPool mapPool, string? parentObjectName = null)
        {
            return type.GetProperties().SelectMany(property =>
            {
                if (property.PropertyType.IsClass && property.PropertyType != typeof(string))
                    return GetFieldOptionsForType(property.PropertyType, mapPool, parentObjectName: property.Name);

                var applicableForMapPoolAttribute = property.GetCustomAttribute<ApplicableForMapPoolAttribute>();

                if (applicableForMapPoolAttribute != null && !applicableForMapPoolAttribute.MapPools.Contains(mapPool))
                    return Array.Empty<LivePlaylistFieldOption>();

                var value = parentObjectName != null ? $"{parentObjectName}.{property.Name}" : property.Name;

                var displayNameAttribute = property.GetCustomAttribute<DisplayNameAttribute>();

                var name = displayNameAttribute?.DisplayName ?? property.Name;

                return new[]
                {
                    new LivePlaylistFieldOption
                    {
                        Value = value,
                        Name = name,
                        Type = property.PropertyType,
                        HasPredefinedOptions = property.GetCustomAttribute<HasPredefinedOptions>() is not null,
                        Sortable = !typeof(IEnumerable).IsAssignableFrom(property.PropertyType) || property.PropertyType == typeof(string),
                    }
                };
            });
        }
    }
}
