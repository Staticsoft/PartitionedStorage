using Staticsoft.PartitionedStorage.Abstractions;

namespace Staticsoft.PartitionedStorage.Files
{
    public class FilePartitions : Partitions
    {
        readonly ItemSerializer Serializer;

        public FilePartitions(ItemSerializer serializer)
            => Serializer = serializer;

        public Partition<TData> Get<TData>(string partitionName) where TData : new()
            => new FilePartition<TData>(Serializer, partitionName);
    }
}
