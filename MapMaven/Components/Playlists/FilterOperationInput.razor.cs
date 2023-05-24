using Microsoft.AspNetCore.Components;
using MapMaven.Core.Models.DynamicPlaylists;
using MapMaven.Utility;
using System.Globalization;

namespace MapMaven.Components.Playlists
{
    public partial class FilterOperationInput
    {
        [Parameter]
        public MapPool MapPool { get; set; }

        [Parameter]
        public FilterOperation FilterOperation { get; set; }

        [Parameter]
        public EventCallback OnRemove { get; set; }

        DynamicPlaylistFieldOption SelectedFieldOption { get; set; }

        protected override void OnParametersSet()
        {
            if (FilterOperation.Field != null)
                SelectedFieldOption = DynamicPlaylistFields.FieldOptions(MapPool).FirstOrDefault(field => field.Value == FilterOperation.Field);
        }

        void OnFieldChanged(DynamicPlaylistFieldOption selectedField)
        {
            SelectedFieldOption = selectedField;
            FilterOperation.Field = SelectedFieldOption.Value;
            FilterOperation.Operator = default;
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
        
        void DateValueChanged(DateTime? value)
        {
            FilterOperation.Value = value?.ToString(CultureInfo.InvariantCulture);
        }
    }
}