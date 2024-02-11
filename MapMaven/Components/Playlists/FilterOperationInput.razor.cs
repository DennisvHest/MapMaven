using Microsoft.AspNetCore.Components;
using MapMaven.Core.Models.DynamicPlaylists;
using MapMaven.Utility;
using System.Globalization;
using MapMaven.Core.Models.DynamicPlaylists.MapInfo;
using MapMaven.Core.Services.Interfaces;
using MapMaven.Core.Services;
using System.Reflection;
using MapMaven.Core.Utilities.DynamicPlaylists;
using MapMaven.Core.Utilities;

namespace MapMaven.Components.Playlists
{
    public partial class FilterOperationInput
    {
        [Inject]
        IMapService MapService { get; set; }

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
            if (SelectedFieldOption?.Value == selectedField.Value)
                return;

            SelectedFieldOption = selectedField;
            FilterOperation.Field = SelectedFieldOption.Value;
            FilterOperation.Operator = default;
            FilterOperation.Value = null;

            FilterOperation.Operator = DynamicPlaylistArrangementService.FilterOperatorsForType[SelectedFieldOption.Type].First();

            if (SelectedFieldOption.Type == typeof(bool))
                BooleanValueChanged(false);

            StateHasChanged();
        }

        void BooleanValueChanged(bool value)
        {
            FilterOperation.Value = value.ToString();
        }
        
        void DateValueChanged(DateTime? value)
        {
            FilterOperation.Value = value?.ToString(CultureInfo.InvariantCulture);
        }

        void DoubleValueChanged(string value)
        {
            FilterOperation.Value = value?.Replace(',', '.');
        }

        ICollection<string> GetFieldOptions()
        {
            var options = FilterOperation.Field switch
            {
                nameof(DynamicPlaylistMap.Difficulty) => DifficultyUtils.Difficulties,
                nameof(DynamicPlaylistMap.Tags) => MapService.MapTags,
                _ => []
            };

            if (FilterOperation.Value is not null)
                options = options.Concat([FilterOperation.Value]);

            options = options.Distinct();

            if (FilterOperation.Field != nameof(DynamicPlaylistMap.Difficulty))
                options = options.OrderBy(x => x);

            return options.ToList();
        }
    }
}