namespace Staticsoft.PartitionedStorage.Abstractions
{
    public static class PartitionsExtensions
    {
        public static Partition<TData> Get<TData>(this Partitions partitions)
            where TData : new()
            => partitions.Get<TData>(typeof(TData).Name);

        public static PartitionFactory<TData> GetFactory<TData>(this Partitions partitions)
            where TData : new()
            => partitions.GetFactory<TData>(typeof(TData).Name);

        public static PartitionFactory<TData> GetFactory<TData>(this Partitions partitions, string partitionPrefix)
            where TData : new()
            => new PartitionFactory<TData>(partitions, partitionPrefix);
    }
}
