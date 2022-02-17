namespace Staticsoft.PartitionedStorage.Abstractions
{
    public static class ItemExtensions
    {
        public static bool HasVersion<TData>(this Item<TData> item)
            => !string.IsNullOrEmpty(item.Version);
    }
}
