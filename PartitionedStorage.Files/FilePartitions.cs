using Staticsoft.PartitionedStorage.Abstractions;
using System.Collections.Concurrent;
using System.IO;

namespace Staticsoft.PartitionedStorage.Files;

public class FilePartitions(
    ItemSerializer serializer,
    FilePartitionedStorageOptions options
) : Partitions
{
    readonly ItemSerializer Serializer = serializer;
    readonly FilePartitionedStorageOptions Options = options;

    readonly ConcurrentDictionary<string, object> Partitions = [];

    public Partition<TData> Get<TData>(string partitionName) where TData : new()
    {
        var partition = Partitions.GetOrAdd(partitionName, (_) => new FilePartition<TData>(Serializer, GetPartitionName(partitionName)));

        return (FilePartition<TData>)partition;
    }

    string GetPartitionName(string partitionName)
        => Path.Combine(Options.PartitionedStoragePath, partitionName);
}
