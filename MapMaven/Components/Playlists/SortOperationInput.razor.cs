using MapMaven.Core.Models.AdvancedSearch;
using MapMaven.Core.Models.LivePlaylists;
using MapMaven.Utility;
using Microsoft.AspNetCore.Components;

namespace MapMaven.Components.Playlists
{
    public partial class SortOperationInput
    {
        [Parameter]
        public MapPool MapPool { get; set; }

        [Parameter]
        public bool FirstSortOperation { get; set; } = true;

        [Parameter]
        public SortOperation SortOperation { get; set; }

        [Parameter]
        public EventCallback OnRemove { get; set; }

        LivePlaylistFieldOption SelectedFieldOption { get; set; }

        protected override void OnParametersSet()
        {
            if (SortOperation.Field != null)
                SelectedFieldOption = LivePlaylistFields.FieldOptions(MapPool).FirstOrDefault(field => field.Value == SortOperation.Field);
        }

        void OnFieldChanged(LivePlaylistFieldOption selectedField)
        {
            SelectedFieldOption = selectedField;
            SortOperation.Field = SelectedFieldOption.Value;
        }
    }
}