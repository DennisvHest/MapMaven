using Microsoft.AspNetCore.Components;
using MapMaven.Core.Models.LivePlaylists;
using MapMaven.Utility;
using System.Globalization;
using MapMaven.Core.Models.LivePlaylists.MapInfo;
using MapMaven.Core.Services.Interfaces;
using MapMaven.Core.Services;
using System.Reflection;
using MapMaven.Core.Utilities.LivePlaylists;
using MapMaven.Core.Utilities;
using MapMaven.Core.Models.AdvancedSearch;

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

        LivePlaylistFieldOption SelectedFieldOption { get; set; }

        protected override void OnParametersSet()
        {
            if (FilterOperation.Field != null)
                SelectedFieldOption = LivePlaylistFields.FieldOptions(MapPool).FirstOrDefault(field => field.Value == FilterOperation.Field);
        }

        void OnFieldChanged(LivePlaylistFieldOption selectedField)
        {
            if (SelectedFieldOption?.Value == selectedField.Value)
                return;

            SelectedFieldOption = selectedField;
            FilterOperation.Field = SelectedFieldOption.Value;
            FilterOperation.Operator = default;
            FilterOperation.Value = null;

            FilterOperation.Operator = LivePlaylistArrangementService.FilterOperatorsForType[SelectedFieldOption.Type].First();

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
                nameof(AdvancedSearchMap.Difficulty) => DifficultyUtils.Difficulties,
                nameof(AdvancedSearchMap.Tags) => MapService.MapTags,
                _ => []
            };

            if (FilterOperation.Value is not null)
                options = options.Concat([FilterOperation.Value]);

            options = options.Distinct();

            if (FilterOperation.Field != nameof(AdvancedSearchMap.Difficulty))
                options = options.OrderBy(x => x);

            return options.ToList();
        }
    }
}