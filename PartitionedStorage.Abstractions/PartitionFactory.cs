namespace Staticsoft.PartitionedStorage.Abstractions
{
    public class PartitionFactory<TData>
        where TData : new()
    {
        readonly Partitions Partitions;
        readonly string PartitionPrefix;

        public PartitionFactory(Partitions partitions, string partitionPrefix)
        {
            Partitions = partitions;
            PartitionPrefix = partitionPrefix;
        }

        public Partition<TData> Get(string partitionSuffix)
            => Partitions.Get<TData>($"{PartitionPrefix}{partitionSuffix}");
    }
}
