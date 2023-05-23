using MapMaven.Core.Models.DynamicPlaylists.MapInfo;
using MapMaven.Core.Models.DynamicPlaylists;

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
                var name = parentObjectName != null ? $"{property.Name} ({parentObjectName})" : property.Name;
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
