using Microsoft.Extensions.DependencyInjection;
using Staticsoft.PartitionedStorage.Abstractions;
using Staticsoft.PartitionedStorage.Files;

namespace Staticsoft.PartitionedStorage.Tests;

public class FilePartitionedStorageTests : PartitionedStorageTests
{
    protected override IServiceCollection Services => base.Services
        .AddSingleton<Partitions, FilePartitions>()
        .AddSingleton<ItemSerializer, JsonItemSerializer>()
        .AddSingleton<FilePartitionedStorageOptions, TestFilePartitionedStorageOptions>();
}