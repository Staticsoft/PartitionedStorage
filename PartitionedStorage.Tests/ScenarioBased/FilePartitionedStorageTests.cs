using Microsoft.Extensions.DependencyInjection;
using Staticsoft.PartitionedStorage.Abstractions;
using Staticsoft.PartitionedStorage.Files;
using Xunit;

namespace Staticsoft.PartitionedStorage.Tests.ScenarioBased;

[Collection(nameof(FilePartitionedStorageTests))]
public class FilePartitionedStorageTests : PartitionedStorageTests
{
	protected override IServiceCollection Services => base.Services
		.AddSingleton<Partitions, FilePartitions>()
		.AddSingleton<ItemSerializer, JsonItemSerializer>()
		.AddSingleton<FilePartitionedStorageOptions, TestFilePartitionedStorageOptions>();
}