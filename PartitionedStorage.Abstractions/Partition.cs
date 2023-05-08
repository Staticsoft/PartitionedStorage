using System.Threading.Tasks;

namespace Staticsoft.PartitionedStorage.Abstractions;

public interface Partition<TData>
    where TData : new()
{
    Task<Item<TData>> Get(string id);

    Task<string> Save(Item<TData> item);

    Task<Item<TData>[]> Scan(ScanOptions options);

    Task Remove(string id);
}
