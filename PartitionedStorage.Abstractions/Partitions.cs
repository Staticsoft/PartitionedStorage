namespace Staticsoft.PartitionedStorage.Abstractions
{
    public interface Partitions
    {
        Partition<TData> Get<TData>(string partitionName)
            where TData : new();
    }
}
