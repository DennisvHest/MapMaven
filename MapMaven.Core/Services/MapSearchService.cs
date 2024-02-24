using MapMaven.Core.Models.AdvancedSearch;
using MapMaven.Core.Models.DynamicPlaylists;
using MapMaven.Core.Models.DynamicPlaylists.MapInfo;
using Pather.CSharp;
using System.Globalization;

namespace MapMaven.Core.Services
{
    public class MapSearchService
    {
        private static readonly IResolver _resolver = new Resolver();

        public static bool FilterOperationMatches(AdvancedSearchMap map, FilterOperation filterOperation)
        {
            var value = _resolver.ResolveSafe(map, filterOperation.Field);

            if (value is string stringValue)
            {
                return filterOperation.Operator switch
                {
                    FilterOperator.Equals => stringValue.Equals(filterOperation.Value, StringComparison.OrdinalIgnoreCase),
                    FilterOperator.NotEquals => !stringValue.Equals(filterOperation.Value, StringComparison.OrdinalIgnoreCase),
                    FilterOperator.Contains => stringValue.Contains(filterOperation.Value, StringComparison.OrdinalIgnoreCase),
                    FilterOperator.NotContains => !stringValue.Contains(filterOperation.Value, StringComparison.OrdinalIgnoreCase),
                    _ => false
                };
            }

            if (value is double doubleValue)
            {
                var compareValue = double.Parse(filterOperation.Value, CultureInfo.InvariantCulture);

                return filterOperation.Operator switch
                {
                    FilterOperator.Equals => doubleValue == compareValue,
                    FilterOperator.NotEquals => doubleValue != compareValue,
                    FilterOperator.GreaterThan => doubleValue > compareValue,
                    FilterOperator.LessThan => doubleValue < compareValue,
                    FilterOperator.LessThanOrEqual => doubleValue <= compareValue,
                    FilterOperator.GreaterThanOrEqual => doubleValue >= compareValue,
                    _ => false
                };
            }

            if (value is DateTime dateTimeValue)
            {
                var compareValue = DateTime.Parse(filterOperation.Value, CultureInfo.InvariantCulture);

                return filterOperation.Operator switch
                {
                    FilterOperator.Equals => dateTimeValue == compareValue,
                    FilterOperator.NotEquals => dateTimeValue != compareValue,
                    FilterOperator.GreaterThan => dateTimeValue > compareValue,
                    FilterOperator.LessThan => dateTimeValue < compareValue,
                    FilterOperator.LessThanOrEqual => dateTimeValue <= compareValue,
                    FilterOperator.GreaterThanOrEqual => dateTimeValue >= compareValue,
                    _ => false
                };
            }

            if (value is bool boolValue)
            {
                var compareValue = bool.Parse(filterOperation.Value);

                return filterOperation.Operator switch
                {
                    FilterOperator.Equals => boolValue == compareValue,
                    FilterOperator.NotEquals => boolValue != compareValue,
                    _ => false
                };
            }

            if (value is IEnumerable<string> stringEnumerableValue)
            {
                return filterOperation.Operator switch
                {
                    FilterOperator.Contains => stringEnumerableValue.Contains(filterOperation.Value, StringComparer.OrdinalIgnoreCase),
                    FilterOperator.NotContains => !stringEnumerableValue.Contains(filterOperation.Value, StringComparer.OrdinalIgnoreCase),
                    _ => false
                };
            }

            return false;
        }

        public static IEnumerable<TSourceType> SortMaps<TSourceType>(IEnumerable<TSourceType> maps, IEnumerable<SortOperation> sortOperations, Func<TSourceType, AdvancedSearchMap> advancedSearchMapGetter)
        {
            var firstSortOperation = sortOperations.FirstOrDefault();

            if (firstSortOperation != null)
            {
                IOrderedEnumerable<TSourceType> orderedMaps;

                if (firstSortOperation.Direction == SortDirection.Ascending)
                {
                    orderedMaps = maps.OrderBy(m => _resolver.ResolveSafe(advancedSearchMapGetter(m), firstSortOperation.Field));
                }
                else
                {
                    orderedMaps = maps.OrderByDescending(m => _resolver.ResolveSafe(advancedSearchMapGetter(m), firstSortOperation.Field));
                }

                foreach (var otherSortOperation in sortOperations.Skip(1))
                {
                    if (otherSortOperation.Direction == SortDirection.Ascending)
                    {
                        orderedMaps = orderedMaps.ThenBy(m => _resolver.ResolveSafe(advancedSearchMapGetter(m), otherSortOperation.Field));
                    }
                    else
                    {
                        orderedMaps = orderedMaps.ThenByDescending(m => _resolver.ResolveSafe(advancedSearchMapGetter(m), otherSortOperation.Field));
                    }
                }

                maps = orderedMaps;
            }

            return maps;
        }
    }
}
