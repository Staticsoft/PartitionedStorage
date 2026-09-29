using Amazon;
using Amazon.DynamoDBv2;
using Microsoft.Extensions.DependencyInjection;
using Staticsoft.PartitionedStorage.Abstractions;
using Staticsoft.PartitionedStorage.AWS;
using System;
using Xunit;

namespace Staticsoft.PartitionedStorage.Tests.ScenarioBased;

[Collection(nameof(DynamoDBPartitionedStorageTests))]
public class DynamoDBPartitionedStorageTests : PartitionedStorageTests
{
	protected override IServiceCollection Services => base.Services
		.AddSingleton<Partitions, DynamoDBPartitions>()
		.AddSingleton<ItemSerializer, JsonItemSerializer>()
		.AddSingleton(new DynamoDBPartitionedStorageOptions() { TableNamePrefix = "PartitionedStorageTests" })
		.AddSingleton(CreateDynamoDBClient());

	internal static AmazonDynamoDBClient CreateDynamoDBClient()
		=> new(GetAccessKeyId(), GetSecretAccessKey(), GetRegion());

	static string GetAccessKeyId()
		=> Environment.GetEnvironmentVariable("PartitionedStorageAccessKeyId");

	static string GetSecretAccessKey()
		=> Environment.GetEnvironmentVariable("PartitionedStorageSecretAccessKey");

	static RegionEndpoint GetRegion()
		=> RegionEndpoint.GetBySystemName(Environment.GetEnvironmentVariable("PartitionedStorageRegion"));
}
