using MapMaven.Core.Models.DynamicPlaylists;

namespace MapMaven.Extensions
{
    public static class EnumExtensions
    {
        public static string DisplayName(this FilterOperator filterOperator)
        {
            return filterOperator switch
            {
                FilterOperator.Equals => "=",
                FilterOperator.NotEquals => "!=",
                FilterOperator.GreaterThan => ">",
                FilterOperator.GreaterThanOrEqual => ">=",
                FilterOperator.LessThan => "<",
                FilterOperator.LessThanOrEqual => "<=",
                FilterOperator.Contains => "contains",
                FilterOperator.NotContains => "doesn't contain",
                _ => string.Empty
            };
        }
    }
}
