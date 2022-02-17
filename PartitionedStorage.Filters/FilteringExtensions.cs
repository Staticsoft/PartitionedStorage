using Staticsoft.PartitionedStorage.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Staticsoft.PartitionedStorage.Filters
{
    public static class FilteringExtensions
    {
        public static IEnumerable<T> ApplyFilters<T>(this IEnumerable<T> entities, Func<T, string> nameResolver, ScanOptions options)
            => entities
                .ApplyFromFilter(nameResolver, options.FromItem)
                .ApplyToFilter(nameResolver, options.ToItem)
                .ApplyMaxItemsFilter(options.MaxItems);

        static IEnumerable<T> ApplyFromFilter<T>(this IEnumerable<T> entities, Func<T, string> nameResolver, string fromFilter)
            => string.IsNullOrEmpty(fromFilter)
            ? entities
            : entities.Where(named => nameResolver(named).CompareTo(fromFilter) >= 0);

        static IEnumerable<T> ApplyToFilter<T>(this IEnumerable<T> entities, Func<T, string> nameResolver, string toFilter)
            => string.IsNullOrEmpty(toFilter)
            ? entities
            : entities.Where(named => nameResolver(named).CompareTo(toFilter) < 0);

        static IEnumerable<T> ApplyMaxItemsFilter<T>(this IEnumerable<T> files, int maxItems)
            => files.Take(maxItems);
    }
}
