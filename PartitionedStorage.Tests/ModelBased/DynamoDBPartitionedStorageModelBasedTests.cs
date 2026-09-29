using Amazon.DynamoDBv2;
using Staticsoft.PartitionedStorage.Abstractions;
using Staticsoft.PartitionedStorage.AWS;
using Staticsoft.PartitionedStorage.Tests.ScenarioBased;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Staticsoft.PartitionedStorage.Tests.ModelBased;

[Collection(nameof(DynamoDBPartitionedStorageTests))]
public class DynamoDBPartitionedStorageModelBasedTests(ITestOutputHelper output)
	: PartitionedStorageModelBasedTests(output), IAsyncLifetime
{
	readonly AmazonDynamoDBClient Client = DynamoDBPartitionedStorageTests.CreateDynamoDBClient();
	readonly ConcurrentBag<PrefixedPartitions> Created = [];

	protected override long Iterations => 1000;

	protected override Partitions CreateActual()
	{
		var partitions = new PrefixedPartitions(
			Storage(),
			$"ModelBased-{Guid.NewGuid()}-"
		);
		Created.Add(partitions);
		return partitions;
	}

	public Task InitializeAsync()
		=> Task.CompletedTask;

	public async Task DisposeAsync()
	{
		var cleanup = Storage();
		var parallelism = new ParallelOptions { MaxDegreeOfParallelism = 8 };
		await Parallel.ForEachAsync(Created.SelectMany(partitions => partitions.UsedPartitionNames), parallelism, async (name, _) =>
		{
			var partition = cleanup.Get<TestItem>(name);
			foreach (var item in await partition.Scan()) await partition.Remove(item.Id);
		});
		Client.Dispose();
	}

	DynamoDBPartitions Storage()
		=> new(new JsonItemSerializer(), Client, new DynamoDBPartitionedStorageOptions { TableNamePrefix = "PartitionedStorageTests" });
}
