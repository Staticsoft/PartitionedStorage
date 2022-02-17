using Staticsoft.PartitionedStorage.Abstractions;
using Staticsoft.PartitionedStorage.Filters;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace Staticsoft.PartitionedStorage.Memory
{
    public class MemoryPartition<TData> : Partition<TData>
        where TData : new()
    {
        readonly string PartitionName;
        readonly ItemSerializer Serializer;
        readonly ConcurrentDictionary<string, MemoryItem> Items = new();

        public MemoryPartition(ItemSerializer serializer, string partitionName)
        {
            Serializer = serializer;
            PartitionName = partitionName;
        }

        public Task<Item<TData>[]> Scan(ScanOptions options)
            => Task.FromResult(
                Items
                    .OrderBy(item => item.Key)
                    .Select(item => item.Value)
                    .ApplyFilters(item => item.Id, options)
                    .Select(ToItem)
                    .ToArray()
            );

        public Task<Item<TData>> Get(string id)
        {
            if (!Items.TryGetValue(id, out var item))
            {
                throw new PartitionedStorageItemNotFoundException(id, PartitionName);
            }

            return Task.FromResult(ToItem(item));
        }

        public Task<string> Save(Item<TData> item)
        {
            var data = Serializer.Serialize(item.Data);
            return Task.FromResult(item.HasVersion()
                ? Save(item.Id, data, item.Version)
                : Save(item.Id, data)
            );
        }

        string Save(string id, string data)
        {
            var item = ToMemoryItem(id, data, GenerateVersion());
            if (!Items.TryAdd(id, item))
            {
                throw new PartitionedStorageItemAlreadyExistsException(id, PartitionName);
            }

            return item.Version;
        }

        string Save(string id, string data, string version)
        {
            var item = ToMemoryItem(id, data, GenerateVersion());
            var comparisonItem = ToMemoryItem(id, string.Empty, version);
            if (!Items.TryUpdate(id, item, comparisonItem))
            {
                throw new PartitionedStorageItemVersionMismatchException(id, version);
            }

            return item.Version;
        }

        public Task Remove(string id)
        {
            Items.TryRemove(id, out var _);

            return Task.CompletedTask;
        }

        Item<TData> ToItem(MemoryItem item) => new Item<TData>
        {
            Id = item.Id,
            Data = Serializer.Deserialize<TData>(item.Data),
            Version = item.Version
        };

        static MemoryItem ToMemoryItem(string id, string value, string version) => new MemoryItem
        {
            Id = id,
            Data = value,
            Version = version,
        };

        static string GenerateVersion()
            => $"{Guid.NewGuid()}";
    }
}
