using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace MapMaven.Components.Shared
{
    public class SortableTemplateColumn<T, TProperty> : PropertyColumn<T, TProperty>
    {
        [Parameter]
        public bool Visible { get; set; } = true;

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            if (!Visible)
            {
                CellStyle = "display: none;" + CellStyle;
                HeaderStyle = "display: none;" + HeaderStyle;
            }
            else
            {
                CellStyle = CellStyle?.Replace("display: none;", string.Empty);
                HeaderStyle = HeaderStyle?.Replace("display: none;", string.Empty);
            }
        }

        protected override object CellContent(T item) => null;

        protected override object PropertyFunc(T item) => null;

        protected override void SetProperty(object item, object value)
        {
        }
    }
}
