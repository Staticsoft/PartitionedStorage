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

public class DynamoDBPartitionedStorageModelBasedTests(ITestOutputHelper output)
	: PartitionedStorageModelBasedTests(output), IAsyncLifetime
{
	readonly AmazonDynamoDBClient Client = DynamoDBPartitionedStorageTests.CreateDynamoDBClient();
	readonly ConcurrentBag<PrefixedPartitions> Created = [];

	// Every operation is a network call, so far fewer sequences than for the local implementations.
	protected override long Iterations => 100;

	// Every generated sequence gets its own partitions in the shared table, so runs never share state.
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
		await Task.WhenAll(Created.SelectMany(partitions => partitions.UsedPartitionNames).Select(async name =>
		{
			var partition = cleanup.Get<TestItem>(name);
			var items = await partition.Scan();
			await Task.WhenAll(items.Select(item => partition.Remove(item.Id)));
		}));
		Client.Dispose();
	}

	DynamoDBPartitions Storage()
		=> new(new JsonItemSerializer(), Client, new DynamoDBPartitionedStorageOptions { TableNamePrefix = "PartitionedStorageTests" });
}
