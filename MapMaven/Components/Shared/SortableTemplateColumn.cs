using MudBlazor;

namespace MapMaven.Components.Shared
{
    public class SortableTemplateColumn<T, TProperty> : PropertyColumn<T, TProperty>
    {
        protected override object CellContent(T item) => null;

        protected override object PropertyFunc(T item) => null;

        protected override void SetProperty(object item, object value)
        {
        }
    }
}
