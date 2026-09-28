using Staticsoft.PartitionedStorage.Abstractions;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Staticsoft.PartitionedStorage.Tests.ModelBased;

/// <summary>
/// Prefixes every partition name, so that several test runs can share one physical storage
/// (e.g. a DynamoDB table) without seeing each other's items.
/// Remembers which partitions were used, so they can be cleaned up afterwards.
/// </summary>
public class PrefixedPartitions(Partitions partitions, string prefix) : Partitions
{
	readonly Partitions Partitions = partitions;
	readonly string Prefix = prefix;
	readonly ConcurrentDictionary<string, byte> Used = [];

	public IEnumerable<string> UsedPartitionNames
		=> Used.Keys;

	public Partition<TData> Get<TData>(string partitionName)
		where TData : new()
	{
		var name = $"{Prefix}{partitionName}";
		Used.TryAdd(name, 0);
		return Partitions.Get<TData>(name);
	}
}
