using Microsoft.Extensions.DependencyInjection;
using Staticsoft.PartitionedStorage.Abstractions;
using Staticsoft.PartitionedStorage.Memory;

namespace Staticsoft.PartitionedStorage.Tests;

public class MemoryPartitionedStorageTests : PartitionedStorageTests
{
    protected override IServiceCollection Services => base.Services
        .AddSingleton<Partitions, MemoryPartitions>()
        .AddSingleton<ItemSerializer, JsonItemSerializer>();
}
