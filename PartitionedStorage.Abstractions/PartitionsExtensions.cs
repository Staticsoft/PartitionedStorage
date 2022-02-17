namespace Staticsoft.PartitionedStorage.Abstractions
{
    public static class PartitionsExtensions
    {
        public static Partition<TData> Get<TData>(this Partitions storage)
            where TData : new()
            => storage.Get<TData>(typeof(TData).Name);

        public static PartitionFactory<TData> GetFactory<TData>(this Partitions storage)
            where TData : new()
            => storage.GetFactory<TData>(typeof(TData).Name);

        public static PartitionFactory<TData> GetFactory<TData>(this Partitions storage, string partitionPrefix)
            where TData : new()
            => new PartitionFactory<TData>(storage, partitionPrefix);
    }
}
