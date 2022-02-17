namespace Staticsoft.PartitionedStorage.Abstractions
{
    public static class PartitionFactoryExtensions
    {
        public static Partition<TData> Get<TData>(this PartitionFactory<TData> factory)
            where TData : new()
            => factory.Get(typeof(TData).Name);
    }
}
