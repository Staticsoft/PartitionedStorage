using Microsoft.Extensions.DependencyInjection;
using Staticsoft.PartitionedStorage.Abstractions;
using Staticsoft.PartitionedStorage.Files;
using Staticsoft.Testing;

namespace Staticsoft.PartitionedStorage.Tests;

public class FilePartitionedStorageTests : PartitionedStorageTests<FilePartitionedStorageServices> { }

public class FilePartitionedStorageServices : UnitServicesBase
{
    protected override IServiceCollection Services => base.Services
        .AddSingleton<Partitions, FilePartitions>()
        .AddSingleton<ItemSerializer, JsonItemSerializer>()
        .AddSingleton<FilePartitionedStorageOptions, TestFilePartitionedStorageOptions>();
}
