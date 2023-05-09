using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Staticsoft.PartitionedStorage.Abstractions;

namespace Staticsoft.PartitionedStorage.AWS;

public class DynamoDBPartitions : Partitions
{
    readonly ItemSerializer Serializer;
    readonly DynamoDBContext Client;

    public DynamoDBPartitions(
        ItemSerializer serializer,
        AmazonDynamoDBClient client,
        DynamoDBPartitionedStorageOptions options
    )
    {
        Serializer = serializer;
        var config = new DynamoDBContextConfig { TableNamePrefix = options.TableNamePrefix };
        Client = new DynamoDBContext(client, config);
    }

    public Partition<TData> Get<TData>(string partitionName)
        where TData : new()
        => new DynamoDBPartition<TData>(Serializer, Client, partitionName);
}
