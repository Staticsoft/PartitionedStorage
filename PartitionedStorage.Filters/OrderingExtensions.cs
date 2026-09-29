using Staticsoft.PartitionedStorage.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Staticsoft.PartitionedStorage.Filters;

public static class OrderingExtensions
{
	public static IOrderedEnumerable<Data> Sort<Data>(this IEnumerable<Data> items, ScanOrder order, Func<Data, string> sortKey) => order switch
	{
		ScanOrder.Ascending => items.OrderBy(sortKey, StringComparer.Ordinal),
		ScanOrder.Descending => items.OrderByDescending(sortKey, StringComparer.Ordinal),
		_ => throw new NotSupportedException($"{nameof(ScanOrder)} {order} is not supported")
	};
}