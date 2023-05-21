using Microsoft.AspNetCore.Components;
using MapMaven.Core.Models.DynamicPlaylists;
using MapMaven.Core.Models.DynamicPlaylists.MapInfo;

namespace MapMaven.Components.Playlists
{
    public partial class FilterOperationInput
    {
        [Parameter]
        public FilterOperation FilterOperation { get; set; }

        [Parameter]
        public EventCallback OnRemove { get; set; }

        DynamicPlaylistFieldOption SelectedFieldOption { get; set; }

        protected override void OnParametersSet()
        {
            if (FilterOperation.Field != null)
                SelectedFieldOption = FieldOptions.FirstOrDefault(field => field.Value == FilterOperation.Field);
        }

        IEnumerable<DynamicPlaylistFieldOption> FieldOptions => GetFieldOptionsForType(typeof(DynamicPlaylistMap));

        private IEnumerable<DynamicPlaylistFieldOption> GetFieldOptionsForType(Type type, string? parentObjectName = null)
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

        void OnFieldChanged(DynamicPlaylistFieldOption selectedField)
        {
            SelectedFieldOption = selectedField;
            FilterOperation.Field = SelectedFieldOption.Value;
            FilterOperation.Value = null;
            if (SelectedFieldOption.Type == typeof(bool))
            {
                FilterOperation.Operator = MapMaven.Core.Models.DynamicPlaylists.FilterOperator.Equals;
                BooleanValueChanged(false);
            }
        }

        void BooleanValueChanged(bool value)
        {
            FilterOperation.Value = value.ToString();
        }
    }
}