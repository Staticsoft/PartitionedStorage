using Staticsoft.PartitionedStorage.Abstractions;
using System.IO;

namespace Staticsoft.PartitionedStorage.Files;

public class FilePartitions : Partitions
{
    readonly ItemSerializer Serializer;
    readonly FilePartitionedStorageOptions Options;

    public FilePartitions(ItemSerializer serializer, FilePartitionedStorageOptions options)
    {
        Serializer = serializer;
        Options = options;
    }

    public Partition<TData> Get<TData>(string partitionName) where TData : new()
        => new FilePartition<TData>(Serializer, GetPartitionName(partitionName));

    string GetPartitionName(string partitionName)
        => Path.Combine(Options.PartitionedStoragePath, partitionName);
}
