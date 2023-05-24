using MapMaven.Core.Models.DynamicPlaylists.MapInfo;
using MapMaven.Core.Models.DynamicPlaylists;
using System.Reflection;
using System.ComponentModel;

namespace MapMaven.Utility
{
    public static class DynamicPlaylistFields
    {
        public static IEnumerable<DynamicPlaylistFieldOption> FieldOptions => GetFieldOptionsForType(typeof(DynamicPlaylistMap));

        private static IEnumerable<DynamicPlaylistFieldOption> GetFieldOptionsForType(Type type, string? parentObjectName = null)
        {
            return type.GetProperties().SelectMany(property =>
            {
                if (property.PropertyType.IsClass && property.PropertyType != typeof(string))
                    return GetFieldOptionsForType(property.PropertyType, parentObjectName: property.Name);

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
