using Staticsoft.PartitionedStorage.Abstractions;
using Staticsoft.PartitionedStorage.Files;
using Staticsoft.PartitionedStorage.Tests.ScenarioBased;
using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace Staticsoft.PartitionedStorage.Tests.ModelBased;

[Collection(nameof(FilePartitionedStorageTests))]
public class FilePartitionedStorageModelBasedTests(ITestOutputHelper output)
	: PartitionedStorageModelBasedTests(output), IDisposable
{
	readonly string Root = Path.Combine(Path.GetTempPath(), $"PartitionedStorageModelBased-{Guid.NewGuid()}");

	protected override Partitions CreateActual()
		=> new FilePartitions(new JsonItemSerializer(), new Options(Path.Combine(Root, $"{Guid.NewGuid()}")));

	public void Dispose()
	{
		if (Directory.Exists(Root)) Directory.Delete(Root, recursive: true);
	}

	record Options(string PartitionedStoragePath) : FilePartitionedStorageOptions;
}
