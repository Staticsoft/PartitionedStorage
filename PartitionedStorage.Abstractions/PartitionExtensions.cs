using System.Threading.Tasks;

namespace Staticsoft.PartitionedStorage.Abstractions
{
    public static class PartitionExtensions
    {
        public static Task<Item<TData>[]> Scan<TData>(this Partition<TData> partition)
            where TData : new()
            => partition.Scan(new ScanOptions());

        public static Task<string> Save<TData>(this Partition<TData> partition, string id, TData data)
            where TData : new()
            => partition.Save(new Item<TData> { Id = id, Data = data });

        public static Task<string> Save<TData>(this Partition<TData> partition, string id, TData data, string version)
            where TData : new()
            => partition.Save(new Item<TData> { Id = id, Data = data, Version = version });
    }
}
