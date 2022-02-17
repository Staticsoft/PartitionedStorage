using Staticsoft.PartitionedStorage.Abstractions;
using System.Collections.Concurrent;

namespace Staticsoft.PartitionedStorage.Memory
{
    public class MemoryPartitions : Partitions
    {
        readonly ItemSerializer Serializer;
        readonly ConcurrentDictionary<string, object> Partitions = new();

        public MemoryPartitions(ItemSerializer serializer)
            => Serializer = serializer;

        public Partition<TData> Get<TData>(string partitionName)
            where TData : new()
            => (MemoryPartition<TData>)Partitions.GetOrAdd(partitionName, (name) => new MemoryPartition<TData>(Serializer, name));
    }
}
