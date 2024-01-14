using Staticsoft.PartitionedStorage.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Staticsoft.PartitionedStorage.Filters;

public static class OrderingExtensions
{
    public static IOrderedEnumerable<Data> Sort<Data, SortKey>(this IEnumerable<Data> items, ScanOrder order, Func<Data, SortKey> sortKey) => order switch
    {
        ScanOrder.Ascending => items.OrderBy(sortKey),
        ScanOrder.Descending => items.OrderByDescending(sortKey),
        _ => throw new NotSupportedException($"{nameof(ScanOrder)} {order} is not supported")
    };
}