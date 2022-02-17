using Microsoft.Extensions.DependencyInjection;
using Staticsoft.PartitionedStorage.Abstractions;
using Staticsoft.PartitionedStorage.Memory;
using Staticsoft.Testing;

namespace Staticsoft.PartitionedStorage.Tests
{
    public class MemoryPartitionedStorageTests : PartitionedStorageTests<MemoryPartitionedStorageServices> { }

    public class MemoryPartitionedStorageServices : UnitServicesBase
    {
        protected override IServiceCollection Services => base.Services
            .AddSingleton<Partitions, MemoryPartitions>()
            .AddSingleton<ItemSerializer, JsonItemSerializer>();
    }
}
