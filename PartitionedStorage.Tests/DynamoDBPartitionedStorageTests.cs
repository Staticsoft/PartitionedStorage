using Amazon;
using Amazon.DynamoDBv2;
using Microsoft.Extensions.DependencyInjection;
using Staticsoft.PartitionedStorage.Abstractions;
using Staticsoft.PartitionedStorage.AWS;
using Staticsoft.Testing;
using System;

namespace Staticsoft.PartitionedStorage.Tests
{
    public class DynamoDBPartitionedStorageTests : PartitionedStorageTests<DynamoDBPartitionedStorageServices> { }

    public class DynamoDBPartitionedStorageServices : UnitServicesBase
    {
        protected override IServiceCollection Services => base.Services
            .AddSingleton<Partitions, DynamoDBPartitions>()
            .AddSingleton<ItemSerializer, JsonItemSerializer>()
            .AddSingleton(new DynamoDBPartitionedStorageOptions() { TableNamePrefix = "PartitionedStorageTests" })
            .AddSingleton(CreateDynamoDBClient());

        static AmazonDynamoDBClient CreateDynamoDBClient()
            => new(GetAccessKeyId(), GetSecretAccessKey(), GetRegion());

        static string GetAccessKeyId()
            => Environment.GetEnvironmentVariable("PartitionedStorageAccessKeyId");

        static string GetSecretAccessKey()
            => Environment.GetEnvironmentVariable("PartitionedStorageSecretAccessKey");

        static RegionEndpoint GetRegion()
            => RegionEndpoint.GetBySystemName(Environment.GetEnvironmentVariable("PartitionedStorageRegion"));
    }
}
