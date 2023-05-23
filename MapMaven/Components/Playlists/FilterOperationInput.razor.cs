using Microsoft.AspNetCore.Components;
using MapMaven.Core.Models.DynamicPlaylists;
using MapMaven.Utility;

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
                SelectedFieldOption = DynamicPlaylistFields.FieldOptions.FirstOrDefault(field => field.Value == FilterOperation.Field);
        }

        void OnFieldChanged(DynamicPlaylistFieldOption selectedField)
        {
            SelectedFieldOption = selectedField;
            FilterOperation.Field = SelectedFieldOption.Value;
            FilterOperation.Value = null;
            if (SelectedFieldOption.Type == typeof(bool))
            {
                FilterOperation.Operator = FilterOperator.Equals;
                BooleanValueChanged(false);
            }
        }

        void BooleanValueChanged(bool value)
        {
            FilterOperation.Value = value.ToString();
        }
    }
}